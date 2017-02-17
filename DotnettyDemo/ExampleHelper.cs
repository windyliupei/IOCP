using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;

namespace DotnettyDemo
{
    public static class ExampleHelper
    {
        static ExampleHelper()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(ProcessDirectory)//"Microsoft.Extensions.Configuration.FileExtensions": "1.0.0-*"
                .AddJsonFile("appsettings.json")//"Microsoft.Extensions.Configuration.Json": "1.0.0"
                .Build();
        }

        public static string ProcessDirectory
        {
            get
            {
                return AppContext.BaseDirectory;
            }
        }

        public static IConfigurationRoot Configuration { get; }

        public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
    }
}


