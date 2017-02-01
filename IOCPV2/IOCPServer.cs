using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using IocpServer;
using IOCPServer;

namespace IOCPServer
{
    /// <summary>
    /// IOCP SOCKET服务器
    /// </summary>
    public class IOCPServer : IDisposable,IServer
    {
        #region Fields
        /// <summary>
        /// 服务器程序允许的最大客户端连接数
        /// </summary>
        private int _maxClient;

        /// <summary>
        /// 当前的连接的客户端数
        /// </summary>
        private int _clientCount;

        /// <summary>
        /// 用于每个I/O Socket操作的缓冲区大小
        /// </summary>
        private int _bufferSize;

        /// <summary>
        /// 监听Socket，用于接受客户端的连接请求
        /// </summary>
        private Socket _serverSock;

        /// <summary>
        /// 信号量
        /// </summary>
        Semaphore _maxAcceptedClients;

        /// <summary>
        /// 缓冲区管理
        /// </summary>
        BufferManager _bufferManager;

        /// <summary>
        /// 对象池
        /// </summary>
        SocketAsyncEventArgsPool _objectPool;

        /// <summary>
        /// 超时线程
        /// </summary>
        private DaemonThread m_daemonThread;

        /// <summary>
        /// 连接客户端列表
        /// </summary>
        private SocketAsyncEventArgsList _objectList;
        public SocketAsyncEventArgsList ObjectList { get { return _objectList; } }

        /// <summary>
        /// 请求处理函数
        /// </summary>
        Util _util;

        private ILogWriter logWriter = ConsoleLogWriter.Instance;
        private string sourceName = "IOCPServer";

        private bool disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 监听的IP地址
        /// </summary>
        public IPAddress Address { get; private set; }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public int Port { get; private set; }

        #endregion

        #region Ctors

        public IOCPServer():this(443,200)
        {
        }

        /// <summary>
        /// 异步IOCP SOCKET服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大的客户端数量</param>
        public IOCPServer(int listenPort, int maxClient)
            : this(IPAddress.Any, listenPort, maxClient)
        {
        }

        /// <summary>
        /// 异步Socket TCP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        /// <param name="maxClient">最大客户端数量</param>
        public IOCPServer(IPEndPoint localEP, int maxClient)
            : this(localEP.Address, localEP.Port, maxClient)
        {
        }

        /// <summary>
        /// 异步Socket TCP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public IOCPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            this.Address = localIPAddress;
            this.Port = listenPort;

            _maxClient = maxClient;
            _bufferSize = Config.BUFFER_SIZE;

            _serverSock = new Socket(localIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _bufferManager = new BufferManager(_bufferSize * _maxClient, _bufferSize);
            _objectPool = new SocketAsyncEventArgsPool(_maxClient);
            _maxAcceptedClients = new Semaphore(_maxClient, _maxClient);

            //初始行已链接socket列表
            _objectList = new SocketAsyncEventArgsList();

            _util = new Util();
            _util.ResponseReady += new ResponseEventHandler(OnResponseReady);
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化函数
        /// </summary>
        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
            // against memory fragmentation
            _bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < _maxClient; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                readWriteEventArg.UserToken = null;

                // add SocketAsyncEventArg to the pool
                _objectPool.Push(readWriteEventArg);
            }
            //打开超时检查线程
            m_daemonThread = new DaemonThread(this);
        }

        #endregion

        #region Start
        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                Init();
                IsRunning = true;
                IPEndPoint localEndPoint = new IPEndPoint(Address, Port);
                // 创建监听socket
                _serverSock = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _serverSock.ReceiveBufferSize = _bufferSize;
                //_serverSock.SendBufferSize = _bufferSize;
                if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // 配置监听socket为 dual-mode (IPv4 & IPv6) 
                    // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,
                    _serverSock.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _serverSock.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
                }
                else
                {
                    _serverSock.Bind(localEndPoint);
                }
                // 开始监听
                _serverSock.Listen(this._maxClient);
                // 在监听Socket上投递一个接受请求。
                StartAccept(null);
                _maxAcceptedClients.WaitOne();
            }
        }
        #endregion

        #region Stop

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _serverSock.Close();

                //TODO 关闭对所有客户端的连接
            }
        }

        #endregion


        #region Accept

        /// <summary>
        /// 从客户端开始接受一个连接操作
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs asyniar)
        {
            if (asyniar == null)
            {
                asyniar = new SocketAsyncEventArgs();
                asyniar.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                //socket must be cleared since the context object is being reused
                asyniar.AcceptSocket = null;
            }
            _maxAcceptedClients.WaitOne();
            if (!_serverSock.AcceptAsync(asyniar))
            {
                ProcessAccept(asyniar);
                //如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件
                //此时I/O操作同步完成，不会触发Asyn_Completed事件，所以指定BeginAccept()方法
            }
        }

        /// <summary>
        /// accept 操作完成时回调函数
        /// </summary>
        /// <param name="sender">Object who raised the event.</param>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        

        /// <summary>
        /// 监听Socket接受处理
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket s = e.AcceptSocket;//和客户端关联的socket
                if (s.Connected)
                {
                    try
                    {
                        Interlocked.Increment(ref _clientCount);//原子操作加1
                        //将对象池中的一个空闲对象取出与当前的用户socket绑定
                        SocketAsyncEventArgs asyniar = _objectPool.Pop();

                        s.DontFragment = true;  //表示新的连接        
                        // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                        _bufferManager.SetBuffer(asyniar);
                        asyniar.UserToken = s;

                        logWriter.Write(sourceName, LogPrio.Info, String.Format("客户 {0} 连入, 共有 {1} 个连接。", s.RemoteEndPoint.ToString(), _clientCount));
                        //这里是投递接收请求，如果返回false则证明是I/O 操作同步完成，但是此时不会引发Completed事件
                        //如果返回的是true，则进程被挂起，引发Completed事件
                        if (!s.ReceiveAsync(asyniar))//投递接收请求
                        {
                            ProcessReceive(asyniar);
                        }
                    }
                    catch (SocketException ex)
                    {
                        logWriter.Write(sourceName, LogPrio.Error, String.Format("接受客户 {0} 连接出错, 异常信息： {1} .", s.RemoteEndPoint, ex.ToString()));
                        //TODO 异常处理
                    }
                    //投递下一个接受请求
                    StartAccept(e);
                }
            }
        }

        #endregion

        #region 回调函数

        /// <summary>
        /// 当Socket上的发送或接收请求被完成时，调用此函数
        /// </summary>
        /// <param name="sender">激发事件的对象</param>
        /// <param name="e">与发送或接收完成操作相关联的SocketAsyncEventArg对象</param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Determine which type of operation just completed and call the associated handler.
            switch (e.LastOperation)
            {
                //case SocketAsyncOperation.Accept:
                //    ProcessAccept(e);
                //    break;
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

        #endregion

        

        #region 接收数据


        /// <summary>
        ///接收完成时处理函数
        /// </summary>
        /// <param name="e">与接收完成操作相关联的SocketAsyncEventArg对象</param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket s = (Socket)e.UserToken;

                if (! s.DontFragment)
                {
                    s.DontFragment = true;
                    _objectList.Remove(e);
                }

                if(e.BytesTransferred > 0)
                {
                    string info = "";
                    // 检查远程主机是否关闭连接
                    
                    //判断所有需接收的数据是否已经完成
                    if (s.Available == 0)
                    {
                        //从侦听者获取接收到的消息。 
                        byte[] data = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, e.Offset, data, 0, data.Length);//从e.Buffer块中复制数据出来，保证它可重用
                        info = Encoding.UTF8.GetString(data);
                        logWriter.Write(sourceName, LogPrio.Info, String.Format("收到 {0} {1}字节数据.", s.RemoteEndPoint.ToString(), data.Length));

                        //处理HTTP请求
                        _util.processRequest(e, info);
                    }
                }
                else
                {
                    //TODO 传入数据长度为0情况下，处理分支(相当于没读到数据，一般不会发送IOCompleted请求)
                    logWriter.Write(sourceName, LogPrio.Info, String.Format("客户端 {0} 主动断开连接！", s.RemoteEndPoint.ToString()));
                    CloseClientSocket(e);
                }
            }
            else if (e.SocketError == SocketError.OperationAborted)
            {
                logWriter.Write(sourceName, LogPrio.Info, "客户端异步接收操作取消.");
                return;
            }
            else
            {
                //TODO 接收不成功，可能是因为缓冲区过小，因此应该判断这种情况，增大缓冲区，重新接受
                logWriter.Write(sourceName, LogPrio.Error, String.Format("客户端{0} 数据接收失败: {1}", ((Socket)e.UserToken).RemoteEndPoint.ToString(), e.SocketError.ToString()));
                CloseClientSocket(e);
            }
        }

       

        #endregion


        #region 发送数据

        private void OnResponseReady(object sender, ResponseEventArgs e)
        {
            //数据异步发送
            Send(e.ResponseAsyncEventArg, e.ResponseData);

            //数据同步发送
            //Send((Socket)e.UserToken, response, 0, response.Length, 1000);

            //数据分片发送
            //if (!s.SendAsync(e))//为发送下一段数据，投递发送请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
            //{
            //    //同步接收时处理接收完成事件
            //    ProcessSend(e);
            //}
        }

        /// <summary>
        /// 异步的发送数据
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        public void Send(SocketAsyncEventArgs e, byte[] data)
        {
            Socket s = (Socket)e.UserToken;
            if (s.Connected)
            {
                logWriter.Write(sourceName, LogPrio.Info, String.Format("发送数据到 {0} {1}字节", s.RemoteEndPoint.ToString(), data.Length));
                
                _bufferManager.returnBuffer(e); //将内存区归还到内存池
                e.SetBuffer(data, 0, data.Length); //设置发送数据
                if (!s.SendAsync(e))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                {
                    // 同步发送时处理发送完成事件
                    ProcessSend(e);
                }
            }
            else 
            {
                logWriter.Write(sourceName, LogPrio.Error, String.Format("客户端 {0} 已断开连接，数据发送失败!", s.RemoteEndPoint.ToString()));
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// 同步的使用socket发送数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="timeout"></param>
        public void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int sent = 0; // how many bytes is already sent
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                {
                    //throw new Exception("Timeout.");
                }
                try
                {
                    sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                    ex.SocketErrorCode == SocketError.IOPending ||
                    ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                    {
                        throw ex; // any serious error occurr
                    }
                }
            } while (sent < size);
        }


        /// <summary>
        /// 发送完成时处理函数
        /// </summary>
        /// <param name="e">与发送完成操作相关联的SocketAsyncEventArg对象</param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (Config.TIMEOUT > 0)
                {
                    Socket s = (Socket)e.UserToken;
                    //SendTimeout置零
                    s.SendTimeout = (int)Config.TIMEOUT;
                    s.DontFragment = false;  //表示旧的连接
                    _objectList.Add(e);
                    /* TODO
                     * 长连接(keep-alive)模式下，等待下一个接收数据，应该实现超时断开连接机制
                     */
                    if (!s.ReceiveAsync(e))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        //同步接收时处理接收完成事件
                        ProcessReceive(e);
                    }
                }
                else
                {

                    /*
                     * 短连接模式，关闭Socket连接
                     */
                    CloseClientSocket(e);
                }
            }
            else
            {
                logWriter.Write(sourceName, LogPrio.Error, String.Format("客户端{0} 数据发送失败: {1}", ((Socket)e.UserToken).RemoteEndPoint.ToString(), e.SocketError.ToString()));
                CloseClientSocket(e);
            }
        }

        #endregion

        

        #region Close
        /// <summary>
        /// 关闭socket连接
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed send/receive operation.</param>
        public void CloseClientSocket(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;

            try
            {
                s.Shutdown(SocketShutdown.Both);
                logWriter.Write(sourceName, LogPrio.Info, String.Format("服务器断开与 {0} 的连接", s.RemoteEndPoint.ToString()));
            }
            catch (Exception ex)
            {
                logWriter.Write(sourceName, LogPrio.Error, "断开连接发生错误: " + ex.Message);
                // Throw if client has closed, so it is not necessary to catch.
            }
            finally
            {
                try
                {
                    s.Close();
                    _objectList.Remove(e);
                    Thread.Sleep(10);
                    _bufferManager.FreeBuffer(e); //释放内存
                    e.UserToken = null;  //释放资源
                    _objectPool.Push(e);//SocketAsyncEventArg对象被释放，压入可重用队列。
                    
                    _maxAcceptedClients.Release();  //断开连接，可以接收的连接数+1
                    Interlocked.Decrement(ref _clientCount);
                    logWriter.Write(sourceName, LogPrio.Info, String.Format("现在的连接数量为{0}", _clientCount) );
                }
                catch (Exception ex)
                {
                    logWriter.Write(sourceName, LogPrio.Error, "关闭连接错误: " + ex.Message);
                }
            }           
        }

        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release 
        /// both managed and unmanaged resources; <c>false</c> 
        /// to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Stop();
                        if (_serverSock != null)
                        {
                            _serverSock = null;
                        }
                    }
                    catch (SocketException ex)
                    {
                        //TODO 事件
                        logWriter.Write(sourceName, LogPrio.Error, ex.Message);
                    }
                }
                disposed = true;
            }
        }

        public void PrintCurrentConnections()
        {
            Console.WriteLine("throw new NotImplementedException");
        }

        #endregion
    }
}
