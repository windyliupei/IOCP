using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace AsyncSocketServer
{
    public class BaseSocketProtocol : AsyncSocketInvokeElement
    {
        protected string m_userName;
        public string UserName { get { return m_userName; } }
        protected bool m_logined;
        public bool Logined { get { return m_logined; } }
        protected string m_socketFlag;
        public string SocketFlag { get { return m_socketFlag; } }

        public BaseSocketProtocol(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {
            m_userName = "";
            m_logined = false;
            m_socketFlag = "";
        }

        public bool DoLogin()
        {
            string userName = "";
            string password = "";
            if (m_incomingDataParser.GetValue(ProtocolKey.UserName, ref userName) & m_incomingDataParser.GetValue(ProtocolKey.Password, ref password))
            {
                if (password.Equals(BasicFunc.MD5String("admin"), StringComparison.CurrentCultureIgnoreCase))
                {
                    m_outgoingDataAssembler.AddSuccess();
                    m_userName = userName;
                    m_logined = true;
                    Program.Logger.Info("{0} login success", userName);
                }
                else
                {
                    m_outgoingDataAssembler.AddFailure(ProtocolCode.UserOrPasswordError, "");
                    Program.Logger.Error("{0} login failure,password error", userName);
                }
            }
            else
                m_outgoingDataAssembler.AddFailure(ProtocolCode.ParameterError, "");
            return DoSendResult();
        }

        public bool DoActive()
        {
            m_outgoingDataAssembler.AddSuccess();
            return DoSendResult();
        }
    }
}
