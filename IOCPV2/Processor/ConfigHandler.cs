using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.Collections;
using System.IO;
using System.Threading;
using System.Xml;


/*
 * 需要设置的项目如下：
 * 主页
 * 主目录路径、 应用主文件夹
 * 超时时间 
 * 同时并发数
 * 缓冲区大小
 * 编码
 * 
 * 实现的功能：
 * 1、配置文件的路径一般都是在项目路径对应的根目录config.properties文件下，要有生成新文件的方法
 * 2、获取所有的配置文件项
 */
namespace IOCPServer
{
    /// <summary>
    /// 配置参数静态类，设置默认值
    /// </summary>
    public static class Config
    {
        private static string _WEB_ROOT = @"webapps";
        public static string WEB_ROOT
        {
            get
            {
                return _WEB_ROOT;
            }
            set
            {
                Interlocked.Exchange(ref _WEB_ROOT, value);
            }
        }
        
        private static string _INDEX_PATH = _WEB_ROOT + @"\index.html";
        public static string INDEX_PATH
        {
            get 
            {
                return _INDEX_PATH;
            }
            set
            {
                Interlocked.Exchange(ref _INDEX_PATH, value);
            }
        }

        private static Encoding _ENCODING = Encoding.UTF8;
        public static Encoding ENCODING
        {
            get
            {
                return _ENCODING;
            }
            set
            {
                Interlocked.Exchange(ref _ENCODING, value);
            }
        }

        private static long _TIMEOUT = -1;   //milliseconds, <0表示短连接
        public static long TIMEOUT
        {
            get
            {
                return _TIMEOUT;
            }
            set
            {
                Interlocked.Exchange(ref _TIMEOUT, value);
            }
        }

        private static int _SERVER_PORT = 8088;
        public static int SERVER_PORT
        {
            get
            {
                return _SERVER_PORT;
            }
            set
            {
                Interlocked.Exchange(ref _SERVER_PORT, value);
            }
        }

        private static int _MAX_CLIENT = 1024;
        public static int MAX_CLIENT
        {
            get
            {
                return _MAX_CLIENT;
            }
            set
            {
                Interlocked.Exchange(ref _MAX_CLIENT, value);
            }
        }

        private static int _BUFFER_SIZE = 1024;  //bytes
        public static int BUFFER_SIZE
        {
            get
            {
                return _BUFFER_SIZE;
            }
            set
            {
                Interlocked.Exchange(ref _BUFFER_SIZE, value);
            }
        }
    }

    /// <summary>
    /// 类名：ConfigHandler 
    /// </summary> 
    public class ConfigHandler
    {
        #region Properties
        /// <summary>
        /// 配置文件中存在的键和对应的初始值
        /// </summary>
        public Dictionary<string, object> configs = new Dictionary<string, object>(){
                {"web_root","webapps"},
                {"index_path","webapps/index.html"},
                {"max_client",1024},
                {"encoding","utf-8"},
                {"server_port",8088},
                {"buffer_size",1024},
                {"timeout",10}
            };
        
        /// <summary>
        /// 配置文件所在路径
        /// </summary>
        public static String FILENAME = string.Empty;              //要读写的Properties文件名

        
        #endregion
        /// <summary>
        /// 构造函数
        /// </summary>
        public ConfigHandler(){ }
        /// <summary>
        /// 服务器开始运行时调用的初始化方法，这个方法的
        /// 作用是赋予静态类Config各个属性的值
        /// </summary>
        /// <param name="fileName"></param>
        public void Init(string fileName)
        {
            FILENAME = fileName;
            
            if(!File.Exists(fileName))
            {
                //如果不存在配置文件，就按照默认值创建一个
                XMLHelper.CreateXmlDocument(fileName,"config","1.0","utf-8","yes");
                foreach(string item in configs.Keys)
                {
                    XMLHelper.CreateOrUpdateXmlNodeByXPath(fileName,"//config",item,configs[item].ToString());
                }
            }
            else
            {
                //如果存在就加载
            }

            loadConfig();
        }
        /// <summary>
        ///将文件中的配置信息加载到静态类中
        /// </summary>
        private void loadConfig()
        {
            XmlNode web_root = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//web_root");
            XmlNode index_path = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//index_path");
            XmlNode max_client = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//max_client");
            XmlNode encoding = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//encoding");
            XmlNode server_port = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//server_port");
            XmlNode buffer_size = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//buffer_size");
            XmlNode timeout = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//timeout");
            if (web_root != null)
            {
                Config.WEB_ROOT = web_root.InnerText;
                configs["web_root"] = web_root.InnerText;
            }
            if (index_path != null)
            {
                Config.INDEX_PATH = index_path.InnerText;
                configs["index_path"] = index_path.InnerText;
            }
            if (max_client != null)
            {
                Config.MAX_CLIENT = Int16.Parse(max_client.InnerText);
                configs["max_client"] = Int16.Parse(max_client.InnerText);
            }
            if (encoding != null)
            {
                configs["encoding"] = encoding.InnerText;
                switch (encoding.InnerText)
                { 
                    case "utf-8":
                        Config.ENCODING = Encoding.UTF8;
                        break;
                    case "unicode":
                        Config.ENCODING = Encoding.Unicode;
                        break;
                    default:
                        Config.ENCODING = Encoding.UTF8;
                        break;
                }
                
            }
            if (server_port != null)
            {
                Config.SERVER_PORT = Int16.Parse(server_port.InnerText);
                configs["server_port"] = Int16.Parse(server_port.InnerText);
            }
            if (buffer_size != null)
            {
                Config.BUFFER_SIZE = Int16.Parse(buffer_size.InnerText);
                configs["buffer_size"] = Int16.Parse(buffer_size.InnerText);
            }
            if (timeout != null)
            {
                Config.TIMEOUT = Int16.Parse(timeout.InnerText);
                configs["timeout"] = Int16.Parse(timeout.InnerText);
            }
        }


        public Dictionary<string, string> getProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            XmlNode parent = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config");
            foreach (XmlNode node in parent.ChildNodes)
            {
                properties.Add(node.Name, node.InnerText);
            }
            return properties;
        }
        /// <summary>
        /// 对外开放的设置属性接口
        /// </summary>
        /// <param name="key">要保存的属性key</param>
        /// <param name="value">要保存的属性value</param>
        /// <returns>
        /// <param name="success">返回是否设置成功</param>
        /// </returns>
        public bool setProperty(string key, object value)
        {
            bool success = false;
            XmlNode node = XMLHelper.GetXmlNodeByXpath(FILENAME, "//config//" + key);
            if (node != null)
            {
                XMLHelper.CreateOrUpdateXmlNodeByXPath(FILENAME, "//config", key, value.ToString());
                success = true;
            }
            return success;
        }

        ///<summary>
        /// 对外开放的设置属性接口
        /// </summary>
        /// 
        public bool setProperty(Dictionary<string,string> map)
        {
            bool success = false;
            try
            {
                foreach(string item in map.Keys)
                {
                    this.setProperty(item, map[item]);
                    
                }
                loadConfig();
                success = true;
            }
            catch
            {
                throw new Exception("设置配置文件失败");
            }
            return success;
        }

    }

}



