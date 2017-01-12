using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ioc;
using IocpServer;
using Microsoft.Extensions.DependencyInjection;
using NetFrame.Net;

namespace IOCPService
{
    class Program
    {
        private static int Port;
        private static int MaxConnections;
        private static IPAddress ServerIPAddress;

        static void Main(string[] args)
        {
            Console.WriteLine("Program Starting...");
            LoadSettings();

            LoadModels();

            StartServer();

            Console.WriteLine("Server Started:{0}:{1}!", ServerIPAddress, Port);
            Console.ReadLine();
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
            DependencyResolver.Services.AddSingleton<IServer>(new AsyncIOCPServer(ServerIPAddress, Port, MaxConnections));

            DependencyResolver.Services.AddSingleton<IServer>(new IOCPServer.IOCPServer(ServerIPAddress, Port, MaxConnections));

            DependencyResolver.Services.AddSingleton<IServer>(new AsyncSocketServer.AsyncSocketServer(Port, MaxConnections));

        }
    }
}
