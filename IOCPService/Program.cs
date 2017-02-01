using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ioc;
using IocpServer;
using IOCP;
using Microsoft.Extensions.DependencyInjection;
using NetFrame.Net;
using NetFramework.AsyncSocketServer;

namespace IOCPService
{
    class Program
    {
        private static int Port;
        private static int MaxConnections;
        private static IPAddress ServerIPAddress;
        private static NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            _logger.Debug("Start...");
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //处理未捕获的异常  
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            //处理UI线程异常  
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //处理非UI线程异常  
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Console.WriteLine("Program Starting...");
            LoadSettings();

            LoadModels();

            StartServer();

            Console.WriteLine("Server Started:{0}:{1}!", ServerIPAddress, Port);
            Console.ReadLine();
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Console.WriteLine("Application ThreadException Error");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Application Error");
        }

        private static void StartServer()
        {
            IocpServer.IServer server = DependencyResolver.Instance.GetService<IocpServer.IServer>();
            server.Start();
            Console.WriteLine("{0} Server Started, Port:{1}!", server.GetType().Name, Port);
        }

        private static void LoadSettings()
        {
            Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["port"]);
            MaxConnections = int.Parse(System.Configuration.ConfigurationManager.AppSettings["maxConnection"]);
            ServerIPAddress = IPAddress.Parse("0.0.0.0");
        }

        private static void LoadModels()
        {
            //IOCPV1
            //DependencyResolver.Services.AddSingleton<IServer>(new AsyncIOCPServer(ServerIPAddress, Port, MaxConnections));
            //IOCPV2
            //DependencyResolver.Services.AddSingleton<IServer>(new IOCPServer.IOCPServer(ServerIPAddress, Port, MaxConnections));
            //IOCPV3
            //DependencyResolver.Services.AddSingleton<IServer>(new AsyncSocketServer.AsyncSocketServer(Port, MaxConnections));
            //IOCPV5
            //DependencyResolver.Services.AddSingleton<IServer>(new IServerSocket(MaxConnections,1024));
            //IOCP
            DependencyResolver.Services.AddSingleton<IServer>(new Server(ServerIPAddress.ToString(), Port, MaxConnections, 1024));

        }
    }
}
