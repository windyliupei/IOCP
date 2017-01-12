using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections;

namespace IOCPServer
{
    class Util
    {
        #region Properties
        /// <summary>
        /// 响应生成工厂
        /// </summary>
        HttpResponseFactory httpResponseFactory;

        public event ResponseEventHandler ResponseReady;
        #endregion

        public Util()
        {
            httpResponseFactory = new HttpResponseFactory();
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="e"></param>
        /// <param name="strReceived"></param>
        public void processRequest(SocketAsyncEventArgs e, string strReceived)
        {
            RequestInfo requestInfo = new RequestInfo();
            requestInfo.SocketAsyncArg = e;
            requestInfo.Request = new HttpRequest(strReceived);
            ThreadPool.QueueUserWorkItem(new WaitCallback(requestProcessor), requestInfo);
        }

        /// <summary>
        /// 生成响应信息
        /// </summary>
        /// <param name="args">请求信息</param>
        private void requestProcessor(Object args)
        {
            //解析请求，获取请求对象
            RequestInfo requestInfo = args as RequestInfo;

            //根据请求生成相应对象
            ResponseEventArgs responseEventArgs = new ResponseEventArgs();
            responseEventArgs.ResponseAsyncEventArg = requestInfo.SocketAsyncArg;

            responseEventArgs.ResponseData = this.httpResponseFactory.genResponse(requestInfo.Request).ToBytes(Config.ENCODING);
            ResponseReady(this, responseEventArgs);
        }
    }

    class RequestInfo
    {
        public SocketAsyncEventArgs SocketAsyncArg { get; set; }
        public HttpRequest Request { get; set; }
    }


}
