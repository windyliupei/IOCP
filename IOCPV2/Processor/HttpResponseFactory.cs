using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace IOCPServer
{
    ///summary
    /// 这是HttpFactory的工厂类
    ///summary
    class HttpResponseFactory
    {
        #region Properties
        /// <summary>
        /// 支持的扩展名
        /// </summary>
        private Dictionary<string, string> extensions = new Dictionary<string, string>()
        { 
            //{ "extension", "content type" }
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "ico", "image/x-icon"},
            { "pdf", "application/pdf"},
            { "zip", "application/zip"}
        };


        #endregion

        #region construction       
        public HttpResponseFactory()
        {
        }
        #endregion


        #region generateResponse
        public HttpResponse genResponse(HttpRequest request)
        {
            if (request.header.url.Equals(""))
            {
                return badRequest();
            }

            string requestedFile = request.header.url.Trim();
            requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");
            int extIndex = requestedFile.LastIndexOf('.') + 1;

            switch(request.header.httpMethod)
            {
                case HTTPMethod.GET:
                    {
                        if (extIndex > 0)
                        {
                            string extension = requestedFile.Substring(extIndex);
                            if (extensions.ContainsKey(extension)) // Do we support this extension?
                            {
                                if (requestedFile == "\\config.html")
                                {
                                    return getConfigData();
                                }

                                if (File.Exists(Config.WEB_ROOT + requestedFile)) //If yes check existence of the file
                                {
                                    // Everything is OK, send requested file with correct content type:
                                    return OKResponse(readFile(Config.WEB_ROOT + requestedFile), extensions[extension]);
                                }
                                else
                                {
                                    return notFound();
                                }
                            }
                            else
                            {
                                // We don't support this extension.
                                return notImplement();
                            }
                        }
                        else
                        {
                            // If file is not specified try to send index.htm or index.html
                            // You can add more (default.htm, default.html)
                            //if (requestedFile.Substring(length - 1, 1) != @"\")
                            //if (requestedFile == @"\")
                            //{
                            //    requestedFile += @"\";
                            //}
                            if (File.Exists(Config.INDEX_PATH))
                            {
                                return OKResponse(readFile(Config.INDEX_PATH), extensions[Config.INDEX_PATH.Trim().Substring(Config.INDEX_PATH.Trim().LastIndexOf('.')+1)]);
                            }
                            else
                            {
                                return notFound();
                            }
                        }
                        
                        break;
                    }
                case HTTPMethod.POST:
                    {
                        if (requestedFile == "\\config.html")
                        {
                            Dictionary<string, string> parameters = request.header.parameters;
                            //这要加一个写锁,而且config的位置和初始化方式要改正下
                            ConfigHandler configHandler = new ConfigHandler();
                            configHandler.setProperty(parameters);

                            return getConfigData();
                        }
                        break;
                    }
                default:
                    {
                        return notImplement();
                        break;
                    }
            }
            return notImplement();
        }
        /// <summary>
        /// 根据传入的byte[]类型的content来构造正常的response
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public HttpResponse OKResponse(byte[] content, string contentType)
        {
            HttpResponse response = new HttpResponse();
            response.header = genResponseHeader(StatusCode.OK, contentType, content.Length);
            response.content = content;
            return response;
        }
        public HttpResponse badRequest()
        {
            HttpResponse response = new HttpResponse();
            if (File.Exists(Config.WEB_ROOT + @"\400.html"))
            {
                response.content = readFile(Config.WEB_ROOT + @"\400.html");
            }
            else
            {
                response.content = Config.ENCODING.GetBytes("<html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\"></head><body><h2>CGLZ Simple Web Server</h2><div>400 - Bad Request</div></body></html>");
            }
            response.header = genResponseHeader(StatusCode.Bad_Request, "text/html", response.content.Length);
            return response;
        }
        /// <summary>
        /// 构造没有发现资源的response
        /// </summary>
        /// <returns></returns>
        public HttpResponse notFound()
        {
            HttpResponse response = new HttpResponse();
            if (File.Exists(Config.WEB_ROOT + @"\404.html"))
            {
                response.content = readFile(Config.WEB_ROOT + @"\404.html");
            }
            else
            {
                response.content = Config.ENCODING.GetBytes("<html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\"></head><body><h2>CGLZ Simple Web Server</h2><div>404 - Not Found</div></body></html>");
            }
            response.header = genResponseHeader(StatusCode.Not_Found, "text/html", response.content.Length);
            return response;
        }
        /// <summary>
        /// 构造没有实现的response
        /// </summary>
        /// <returns></returns>
        public HttpResponse notImplement()
        {
            HttpResponse response = new HttpResponse();
            if (File.Exists(Config.WEB_ROOT + @"\501.html"))
            {
                response.content = readFile(Config.WEB_ROOT + @"\501.html");
            }
            else
            {
                response.content = Config.ENCODING.GetBytes("<html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\"></head><body><h2>CGLZ Simple Web Server</h2><div>501 - Not Implemented</div></body></html>");
            }
            response.header = genResponseHeader(StatusCode.Not_Implement, "text/html", response.content.Length);
            return response;
        }
        /// <summary>
        /// 构造带有配置参数的response
        /// </summary>
        /// <returns></returns>
        public HttpResponse getConfigData()
        {
            HttpResponse httpResponse = new HttpResponse();
            //获取配置文件对象
            ConfigHandler configHandler = new ConfigHandler();
            Dictionary<string, string> config = configHandler.getProperties();
            
            //将返回值硬编码进response中
            string titleStr = "CGLZ Simple Web Server Configuration";
            string container = "<div class=\"config\">";
            string containerend = "</div>";
            //构造response内容
            string responseBody = container+ "<form action=\"/config.html\" method=\"post\">";
            
            foreach (string item in config.Keys)
            {
                responseBody += "<lable class=\"con_name\">" + translate(item.ToString()) + "</lable>" +
                            "<input class=\"noneinput\" type=\"text\" name=\"" + item.ToString() + "\" value=\"" + config[item] + "\">" + "</input><br/>";
            }
            responseBody += "<input class=\"change\"type=\"submit\" value=\"提交\"></input>";
            responseBody += "<br/><br/><br/><a class=\"back\" href=\"index.html\">返回主页</a>";
            responseBody += "</form>" + containerend;

            
            //获取二进制数据
            httpResponse.content = Config.ENCODING.GetBytes(
                "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\">"
                +"<link href=\"css\\style.css\" rel=\"stylesheet\" type=\"text/css\" media=\"all\">"
                + "</head><body><div align=\"center\"><h2>" + titleStr + "</h2></div>"
                + responseBody + "</body></html>");
            httpResponse.header = genResponseHeader(StatusCode.OK, "text/html", httpResponse.content.Length);
            return httpResponse;
        }
        /// <summary>
        /// 这个函数的作用是将服务器中的一些条目的名字翻译成中文以供显示需要
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string translate(string input)
        {
            string output = "";
            switch (input)
            {
                case "index_path":
                    output = "首页路径";
                    break;
                case "web_root":
                    output = "服务器根目录";
                    break;
                case "server_port":
                    output = "服务器端口号";
                    break;
                case "timeout":
                    output = "等待时间(毫秒)";
                    break;
                case "max_client":
                    output = "最大并发连接数";
                    break;
                case "buffer_size":
                    output = "缓冲区大小（字节）";
                    break;
                case "encoding":
                    output = "服务器编码";
                    break;
            }
            return output;
        }
        
        /// <summary>
        /// 获取response的头部
        /// </summary>
        /// <param name="responseCode"></param>
        /// <param name="contentType"></param>
        /// <param name="contentLength"></param>
        /// <returns></returns>
        private ResponseHeader genResponseHeader(StatusCode responseCode, string contentType, long contentLength)
        {
            ResponseHeader header = new ResponseHeader();
            header.httpVersion = "HTTP/1.1";
            header.statCode = responseCode;
            header.headerFields.Add("Server", "CGLZ Simple Web Server");
            if(Config.TIMEOUT > 0)
            {
                //长连接
                header.headerFields.Add("Connection", ConnectionType.Keep_Alive.ToString());
            }
            else
            {
                //短连接
                header.headerFields.Add("Connection", ConnectionType.Close.ToString());
            }
            header.headerFields.Add("Content-Length", contentLength.ToString());
            header.headerFields.Add("Content-Type", contentType);
            return header;
        }

        private byte[] readFile(string filePath)
        {
            FileStream fs = File.OpenRead(filePath);
            long fileLength = fs.Length;
            byte[] context = new byte[fileLength];
            fs.Read(context, 0, (int)fileLength);
            fs.Close();
            return context;
        }

        #endregion
    }
}
