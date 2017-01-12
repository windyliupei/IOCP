using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace IOCPServer
{
    class DaemonThread
    {
        private Thread m_thread;
        private IOCPServer m_asyncSocketServer;
        private ILogWriter logWriter = ConsoleLogWriter.Instance;
        private string sourceName = "DaemonThread";

        public DaemonThread(IOCPServer asyncSocketServer)
        {
            m_asyncSocketServer = asyncSocketServer;
            m_thread = new Thread(DaemonThreadStart);
            m_thread.Start();
        }
        private void TimeOutTest()
        {
            SocketAsyncEventArgs[] userTokenArray = null;
            m_asyncSocketServer.ObjectList.CopyList(ref userTokenArray);

            for (int i = 0; i < userTokenArray.Length; i++)
            {
                if (!m_thread.IsAlive)
                    break;
                try
                {
                    if (((Socket)userTokenArray[i].UserToken).SendTimeout-- == 0) //超时Socket断开
                    {                                                 
                        lock (userTokenArray[i])
                        {
                            logWriter.Write(sourceName, LogPrio.Info, String.Format("与客户端 {0} 的长连接超时", ((Socket)userTokenArray[i].UserToken).RemoteEndPoint));
                            m_asyncSocketServer.CloseClientSocket(userTokenArray[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logWriter.Write(sourceName, LogPrio.Error, ex.Message);
                }
            }
        }
        public void DaemonThreadStart()
        {
            while (m_thread.IsAlive)
            {
                TimeOutTest();

                for (int i = 0; i <1 * 1000 / 10; i++) //每2秒检测一次
                {
                    if (!m_thread.IsAlive)
                        break;
                    Thread.Sleep(10);
                }
            }
        }

        public void Close()
        {
            m_thread.Abort();
            m_thread.Join();
        }
    }
}
