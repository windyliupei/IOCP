using DotnettyDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCoreStart
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DotnettyServer server = new DotnettyServer();
            DotnettyServer.RunServerAsync().Wait();
        }
    }
}
