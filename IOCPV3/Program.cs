using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Net;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using NLog;


namespace AsyncSocketServer
{    
    public class Program
    {
        public static ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static AsyncSocketServer AsyncSocketSvr;
        public static string FileDirectory;
        
        
    }
}