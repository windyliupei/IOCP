using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IOCPVCustom
{
    public class AsyncUserToken
    {
        private string uid = "-1";
        internal string UID {
            get
            {
                return uid;
            }
            set
            {
                uid = value;
                ReceiveSaea.UID = value;
                SendSaea.UID = value;
            }
        }
        internal bool Available { set; get; }
        public Socket Socket { get; set; }
        public TCCSocketAsyncEventArgs ReceiveSaea;
        public TCCSocketAsyncEventArgs SendSaea;
        public DateTime ConnecteDateTime { set; get; }
    }
}
