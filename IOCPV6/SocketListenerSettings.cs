using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SocketAsyncServer
{
    class SocketListenerSettings
    {
        // the maximum number of connections the sample is designed to handle simultaneously 
        private Int32 _maxConnections;

        // this variable allows us to create some extra SAEA objects for the pool,
        // if we wish.
        private Int32 _numberOfSaeaForRecSend;

        // max # of pending connections the listener can hold in queue
        private Int32 _backlog;

        // tells us how many objects to put in pool for accept operations
        private Int32 _maxSimultaneousAcceptOps;

        // buffer size to use for each socket receive operation
        private Int32 _receiveBufferSize;

        // length of message prefix for receive ops
        private Int32 _receivePrefixLength;

        // length of message prefix for send ops
        private Int32 _sendPrefixLength;

        // See comments in buffer manager.
        private Int32 _opsToPreAllocate;

        // Endpoint for the listener.
        private IPEndPoint _localEndPoint;

        public SocketListenerSettings(Int32 maxConnections, Int32 excessSaeaObjectsInPool, Int32 backlog, Int32 maxSimultaneousAcceptOps, Int32 receivePrefixLength, Int32 receiveBufferSize, Int32 sendPrefixLength, Int32 opsToPreAlloc, IPEndPoint theLocalEndPoint)
        {
            this._maxConnections = maxConnections;
            this._numberOfSaeaForRecSend = maxConnections + excessSaeaObjectsInPool;
            this._backlog = backlog;
            this._maxSimultaneousAcceptOps = maxSimultaneousAcceptOps;
            this._receivePrefixLength = receivePrefixLength;
            this._receiveBufferSize = receiveBufferSize;
            this._sendPrefixLength = sendPrefixLength;
            this._opsToPreAllocate = opsToPreAlloc;
            this._localEndPoint = theLocalEndPoint;
        }

        public SocketListenerSettings(Int32 maxConnections,IPEndPoint theLocalEndPoint,int bufferSize)
        {
            //this.maxConnections = maxConnections;
            //this.numberOfSaeaForRecSend = maxConnections + excessSaeaObjectsInPool;
            //this.backlog = 100;
            //this.maxSimultaneousAcceptOps = maxSimultaneousAcceptOps;

            //this.receiveBufferSize = receiveBufferSize;

            //this.opsToPreAllocate = 2;
            //this.localEndPoint = theLocalEndPoint;

            this._maxConnections = maxConnections;
            this._numberOfSaeaForRecSend = maxConnections + 10;//10个额外的saea对象
            this._backlog = 100;
            this._maxSimultaneousAcceptOps = 10;
            this._receiveBufferSize = bufferSize;
            this._opsToPreAllocate = 2;
            this._localEndPoint = theLocalEndPoint;
        }

        public Int32 MaxConnections
        {
            get
            {
                return this._maxConnections;
            }
        }
        public Int32 NumberOfSaeaForRecSend
        {
            get
            {
                return this._numberOfSaeaForRecSend;
            }
        }
        public Int32 Backlog
        {
            get
            {
                return this._backlog;
            }
        }
        public Int32 MaxAcceptOps
        {
            get
            {
                return this._maxSimultaneousAcceptOps;
            }
        }
        public Int32 ReceivePrefixLength
        {
            get
            {
                return this._receivePrefixLength;
            }
        }
        public Int32 BufferSize
        {
            get
            {
                return this._receiveBufferSize;
            }
        }
        public Int32 SendPrefixLength
        {
            get
            {
                return this._sendPrefixLength;
            }
        }
        public Int32 OpsToPreAllocate
        {
            get
            {
                return this._opsToPreAllocate;
            }
        }
        public IPEndPoint LocalEndPoint
        {
            get
            {
                return this._localEndPoint;
            }
        }

        public static SocketListenerSettings CreateSetting(string ipAddress, int port, int maxConnections, int receiveBufferSize)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            return new SocketListenerSettings(maxConnections, localEndPoint, receiveBufferSize);
        }
    }    
}
