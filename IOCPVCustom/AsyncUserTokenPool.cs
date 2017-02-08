using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCPVCustom
{
    public class AsyncUserTokenPool
    {
        internal Stack<AsyncUserToken> Pool;
        internal IDictionary<string, AsyncUserToken> Busypool;
        private NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();
        private string[] keys;
        internal Int32 Count
        {
            get
            {
                lock (this.Pool)
                {
                    return this.Pool.Count;
                }
            }
        }
        internal string[] OnlineUID
        {
            get
            {
                lock (this.Busypool)
                {
                    Busypool.Keys.CopyTo(keys, 0);
                }
                return keys;
            }
        }
        internal AsyncUserTokenPool(Int32 capacity)
        {
            keys = new string[capacity];
            this.Pool = new Stack<AsyncUserToken>(capacity);
            this.Busypool = new Dictionary<string, AsyncUserToken>(capacity);
        }
        internal AsyncUserToken Pop(string uid)
        {
            if (uid == string.Empty || uid == "")
                return null;
            AsyncUserToken asyncUserToken = null;
            lock (this.Pool)
            {
                asyncUserToken = this.Pool.Pop();
            }
            asyncUserToken.UID = uid;
            asyncUserToken.Available = true;    //mark the state of pool is not the initial step
            Busypool.Add(uid, asyncUserToken);
            return asyncUserToken;
        }
        internal void Push(AsyncUserToken item)
        {
            if (item == null)
                throw new ArgumentNullException("AsyncUserToken is Null");
            if (item.Available)
            {
                if (Busypool.Keys.Count != 0)
                {
                    if (Busypool.Keys.Contains(item.UID))
                        Busypool.Remove(item.UID);
                    else
                        _logger.Error("AsyncUserToken:{0} Not in Busy queue.",item.UID);
                }
                else
                     _logger.Error("Busy Pool was empty."); 
            }
            item.UID = "-1";
            item.Available = false;
            lock (this.Pool)
            {
                this.Pool.Push(item);
            }
        }
        internal AsyncUserToken FindByUID(string uid)
        {
            if (uid == string.Empty || uid == "")
                return null;
            AsyncUserToken asyncUserToken = null;
            if (this.OnlineUID.Any(key => key == uid))
            {
                asyncUserToken = Busypool[uid];
            }
            return asyncUserToken;
        }
        internal bool BusyPoolContains(string uid)
        {
            lock (this.Busypool)
            {
                return Busypool.Keys.Contains(uid);
            }
        }
    }
}
