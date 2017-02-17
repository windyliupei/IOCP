using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnettyDemo
{
    public static class ServerSettings
    {
        public static bool IsSsl
        {
            get
            {
                string ssl = ExampleHelper.Configuration["ssl"];
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }

        public static int Port => int.Parse(ExampleHelper.Configuration["port"]);
    }
}
