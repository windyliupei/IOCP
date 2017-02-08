using IocpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCPVCustom
{
    public class ServerCustom : IServer
    {
        private int m_maxConnections;   // the maximum number of connections the sample is designed to handle simultaneously 
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        
        AsyncUserTokenPool m_userTokenPool;// pool of reusable AsyncUserToken objects for write, read and accept socket operations
        int m_totalConnectedSockets;      // the total number of clients connected to the server 
        Semaphore m_maxNumberAcceptedClients;
        private int _previouseSemaphore;
        private string m_ipAddress;
        private int m_port;
        private NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();
        
        public ServerCustom(string ipAddress, int port, int maxConnections, int receiveBufferSize)
        {
            m_totalConnectedSockets = 0;
            m_maxConnections = maxConnections;
            m_ipAddress = ipAddress;
            m_port = port;
             
            // allocate buffers such that the maximum number of sockets can have one outstanding read and 
            //write posted to the socket simultaneously  
            m_bufferManager = new BufferManager(receiveBufferSize * maxConnections,
                receiveBufferSize);

            m_userTokenPool = new AsyncUserTokenPool(maxConnections);
            m_maxNumberAcceptedClients = new Semaphore(maxConnections, maxConnections);
        }

        // Initializes the server by preallocating reusable buffers and 
        // context objects.  These objects do not need to be preallocated 
        // or reused, but it is done this way to illustrate how the API can 
        // easily be used to create reusable objects to increase server performance.
        //
        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
            // against memory fragmentation
            m_bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            AsyncUserToken asyncUserToken;

            for (int i = 0; i < m_maxConnections; i++)
            {
                //Pre-allocate a set of reusable asyncUserToken
                asyncUserToken = new AsyncUserToken {ReceiveSaea = new TCCSocketAsyncEventArgs(),SendSaea = new TCCSocketAsyncEventArgs() };
                asyncUserToken.ReceiveSaea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                asyncUserToken.SendSaea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

                //只给接收的SocketAsyncEventArgs设置缓冲区
                m_bufferManager.SetBuffer(asyncUserToken.ReceiveSaea);

                // add asyncUserToken to the pool
                m_userTokenPool.Push(asyncUserToken);
            }

            //Print Serever status
            Thread prinThread = new Thread(() =>
            {
                while (true)
                {
                    PrintCurrentConnections();
                    Thread.Sleep(1000);
                }
            }) {IsBackground = true};
            prinThread.Start();
        }

        // Starts the server such that it is listening for 
        // incoming connection requests.    
        //
        // <param name="localEndPoint">The endpoint which the server will listening 
        // for connection requests on</param>
        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //Refer:https://msdn.microsoft.com/zh-cn/library/1011kecd
            //      https://msdn.microsoft.com/zh-cn/library/system.net.sockets.socketoptionname
            listenSocket.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.Linger, 0);//LINGER 关闭一个连接前等待未发送的数据发送完毕所经过的秒数。如果该值为0，则禁用了该属性
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);

            // post accepts on the listening socket
            StartAccept(null);
        }


        // Begins an operation to accept a connection request from the client 
        //
        // <param name="acceptEventArg">The context object to use when issuing 
        // the accept operation on the server's listening socket</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync 
        // operations and is invoked when an accept operation is complete
        //
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            string uid = e.AcceptSocket.RemoteEndPoint.ToString();   //根据IP获取UID
            //TODO:
            if (m_userTokenPool.BusyPoolContains(uid)) //判断现在的用户是否已经连接，避免同一用户开两个连接
            {
                _logger.Error("Client:{0}, already connected", uid);
                return;
            }

            AsyncUserToken token = m_userTokenPool.Pop(uid);
            token.Socket = e.AcceptSocket;
            token.ReceiveSaea.UserToken = token;
            token.SendSaea.UserToken = token;
            token.ConnecteDateTime = DateTime.Now;

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(token.ReceiveSaea);
            if (!willRaiseEvent)
            {
                ProcessReceive(token.ReceiveSaea);
            }

            Interlocked.Increment(ref m_totalConnectedSockets);
            // Accept the next connection request
            StartAccept(e);
        }

        // This method is called whenever a receive or send operation is completed on a socket 
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        // This method is invoked when an asynchronous receive operation completes. 
        // If the remote host closed the connection, then the socket is closed.  
        // If data was received then the data is echoed back to the client.
        //
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                ProcessReceivedContent(e);  
            }
            else
            {
                CloseClientSocket(token);
            }
        }

        private void ProcessReceivedContent(SocketAsyncEventArgs e)
        {
            //Get Received Bytes
            int bytesToProcess = e.BytesTransferred;
            byte[] receivedBytes = new byte[bytesToProcess];
            Buffer.BlockCopy(e.Buffer, e.Offset, receivedBytes, 0, bytesToProcess);

            Send((e.UserToken as AsyncUserToken), receivedBytes);

        }
        public void Send(AsyncUserToken token, byte[] data)
        {
            try
            {
                if (token.ReceiveSaea.BytesTransferred > 0 && token.ReceiveSaea.SocketError == SocketError.Success)
                {
                    if (token != null && token.Socket.Connected)
                    {
                        token.SendSaea.SetBuffer(data,0, data.Length);
                        //异步发送,如果 willRaiseEvent 为false则异步操作失败，进行同步。
                        bool willRaiseEvent = token.Socket.SendAsync(token.SendSaea);
                        if (!willRaiseEvent)
                        {
                            ProcessSend(token.SendSaea);
                        }
                    }
                    else
                    {
                        CloseClientSocket(token);
                    }
                }
                else
                {
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                CloseClientSocket(token);
                _logger.Error(ex);
            }
        }

        // This method is invoked when an asynchronous send operation completes.  
        // The method issues another receive on the socket to read any additional 
        // data sent from the client
        //
        // <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                // read the next block of data send from the client
                bool willRaiseEvent = token.Socket.ReceiveAsync(token.ReceiveSaea);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.ReceiveSaea);
                }
            }
            else
            {
                CloseClientSocket(e.UserToken as AsyncUserToken);
            }
        }

        private void CloseClientSocket(AsyncUserToken token)
        {
            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_totalConnectedSockets);
            _previouseSemaphore = m_maxNumberAcceptedClients.Release();
            
            m_userTokenPool.Push(token);
        }

        public void Start()
        {
            Init();
            this.Start(new IPEndPoint(IPAddress.Parse(m_ipAddress), m_port));
        }

        public void PrintCurrentConnections()
        {
            long currentConns = m_totalConnectedSockets;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connections:{0},Semaphore:{1}", currentConns, _previouseSemaphore);
            Console.ResetColor();
        }
    }
}
