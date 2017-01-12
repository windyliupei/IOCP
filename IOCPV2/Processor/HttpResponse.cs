using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOCPServer
{   
    /// <summary>
    /// HTTP响应包状态码
    /// </summary>
    public enum StatusCode
    {
        OK = 200,  //客户端请求成功
        Bad_Request = 400, //客户端请求有语法错误，不能被服务器所理解
        Forbidden = 403,  //服务器收到请求，但是拒绝提供服务
        Not_Found = 404,  //请求资源不存在，eg：输入了错误的URL
        URL_TOO_LONG = 414, //请求URL太长
        Internal_Server_Error = 500,  //服务器发生不可预期的错误
        Not_Implement = 501,    //未实现
        Server_Unavailable = 503  //服务器当前不能处理客户端的请求，一段时间后可能恢复正常
    };

    /// <summary>
    /// 封装httpResponse的组建和项目的设置,
    /// 现在项目中基本不用去生成request,生成response比较有用
    /// </summary>
    public class HttpResponse
    {
        #region Properties
        /// <summary>
        ///获取或设置response的header 
        /// </summary>
        public ResponseHeader header { get; set; }
        /// <summary>
        /// 获取或设置报文内容
        /// </summary>
        public byte[] content { get; set; }

        //属性待扩展        

        #endregion

        #region construction
        
        public HttpResponse()
        {
        }

        public string ToString(Encoding encoding)
        {
            return this.header.ToString() + encoding.GetString(this.content);
        }

        public byte[] ToBytes(Encoding encoding)
        {
            return this.header.ToBytes(encoding).Concat(this.content).ToArray();
        }

        #endregion
    }

    /// <summary>
    /// HTTP响应头
    /// </summary>
    public class ResponseHeader
    {
        public string httpVersion { get; set; }
        public StatusCode statCode { get; set; }
        public Dictionary<string, string> headerFields { get; set; }

        public ResponseHeader()
        {
            httpVersion = "HTTP/1.1";
            headerFields = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            StringBuilder responseHeader = new StringBuilder();
            responseHeader.Append(string.Format("{0} {1} {2}\r\n", httpVersion, (int)statCode, statCode.ToString().Replace('_', ' ')));
            foreach(var item in headerFields)
            {
                responseHeader.Append(string.Format("{0}: {1}\r\n", item.Key, item.Value));
            }
            responseHeader.Append("\r\n");
            return responseHeader.ToString();
        }

        public byte[] ToBytes(Encoding encoding)
        {
            return encoding.GetBytes(this.ToString());
        }
    }
}
