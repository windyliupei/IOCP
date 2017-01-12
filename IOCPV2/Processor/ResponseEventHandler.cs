using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace IOCPServer
{
    public delegate void ResponseEventHandler(object sender, ResponseEventArgs e);

    public class ResponseEventArgs : EventArgs
    {
        /// <summary>
        /// SocketAsyncEventArgs,包含要连接的Socket对象
        /// </summary>
        public SocketAsyncEventArgs ResponseAsyncEventArg { get; set; }

        /// <summary>
        /// 待发送数据
        /// </summary>
        public byte[] ResponseData { get; set; }
    }
}
