using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocketServer.AsyncSocketProtocol
{
    public class TCCTestProtocol : BaseSocketProtocol
    {
        public TCCTestProtocol(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken) : base(asyncSocketServer, asyncSocketUserToken)
        {
        }

        public override bool ProcessCommand(byte[] buffer, int offset, int count) //处理分完包的数据，子类从这个方法继承
        {
            //return base.ProcessCommand(buffer, offset, count);

            return DoSendBuffer(buffer, offset, count);
        }

    }
}
