using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace IOCPServer
{
    /// <summary>
    /// HTTP请求方法
    /// </summary>
    public enum HTTPMethod
    {
        GET = 0,
        POST = 1,
        HEAD = 2,
        PUT = 3,
        DELETE = 4,
        TRACE = 5,
        CONNECTION = 6,
        OPTIONS = 7
    };

    /// <summary>
    /// 连接类型
    /// </summary>
    public enum ConnectionType
    {
        Keep_Alive = 0,
        Close = 1
    }

    /// <summary>
    /// HttpRequest对象
    /// </summary>
    public class HttpRequest
    {
        #region Properties
        public RequestHeader header {get; set; }
        public string content { get; set; }
        public string requestStr { get; set; }
        public byte[] rawData { get; set; }

        #endregion

        public HttpRequest()
        {
            this.header = new RequestHeader();
            this.content = "";
            this.requestStr = "";
            this.rawData = null;
        }

        public HttpRequest(string reqStr) : this()
        {
            this.requestStr = reqStr;
            requestParse(reqStr);
        }

        public HttpRequest(byte[] rawData, Encoding encoding) : this()
        {
            this.rawData = rawData;
            requestParse(rawData, encoding);
        }

        /// <summary>
        /// 二进制格式请求包解析
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        private void requestParse(byte[] rawData, Encoding encoding)
        {
            requestParse(encoding.GetString(rawData));
        }

        /// <summary>
        /// 字符串格式请求包解析
        /// </summary>
        /// <param name="reqStr"></param>
        /// <returns></returns>
        private void requestParse(string reqStr)
        {
            int splitIndex = reqStr.IndexOf("\r\n\r\n");
            string headerStr = reqStr.Substring(0, splitIndex);
            this.content = reqStr.Substring(splitIndex + 4);

            headerStr = headerStr.Replace("\r\n", "\n");
            string[] headerLines = headerStr.Split('\n');

            string[] reqLineFields = headerLines[0].Split(' ');
            this.header.httpMethod = (HTTPMethod) Enum.Parse(typeof(HTTPMethod), reqLineFields[0]);
            reqLineFields[1] = HttpUtility.UrlDecode(reqLineFields[1]);  //url解码
            switch(this.header.httpMethod)
            {
                case HTTPMethod.GET:
                    {
                        if (reqLineFields[1].Contains('?'))
                        {
                            string[] urls = reqLineFields[1].Split('?');
                            this.header.url = urls[0];
                            string[] paras = urls[1].Split('&');
                            foreach (string item in paras)
                            {
                                string[] keymap = item.Split('=');
                                this.header.parameters.Add(keymap[0], keymap[1]);
                            }
                        }
                        else
                        {
                            this.header.url = reqLineFields[1];
                        }

                        break;
                    }
                case HTTPMethod.POST:
                    {
                        this.header.url = reqLineFields[1];
                        string paramStr = HttpUtility.UrlDecode(this.content).Trim();
                        string[] parames = paramStr.Split('&');
                        foreach (string item in parames)
                        {

                            int index = item.IndexOf('=');
                            this.header.parameters.Add(item.Substring(0, index), item.Substring(index + 1).Trim());
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            this.header.httpVersion = reqLineFields[2];
            
            for (int i = 1; i < headerLines.Length; i++)
            {
                if (headerLines[i] != "")
                {
                    int index = headerLines[i].IndexOf(':');
                    string headerField = headerLines[i].Substring(0, index);
                    string fieldContent = headerLines[i].Substring(index+1);
                    this.header.headerFields.Add(headerField, fieldContent.Trim());
                }
            }
        }
    }

    /// <summary>
    /// HTTP请求包头
    /// </summary>
    public class RequestHeader
    {
        public HTTPMethod httpMethod { get; set; }
        public string url { get; set; }
        public string httpVersion { get; set; }
        public Dictionary<string, string> headerFields { get; set; }
        public Dictionary<string, string> parameters { get; set; }

        public RequestHeader()
        {
            url = "";
            httpVersion = "";
            headerFields = new Dictionary<string,string>();
            parameters = new Dictionary<string, string>();
        }
    }
}
