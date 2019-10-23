using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FhirTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    IHostingEnvironment environment = context.HostingEnvironment;
                    config
                        .SetBasePath(environment.ContentRootPath)
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true)
                        .AddJsonFile("appsettings.localdev.json", optional: true)
                        .AddEnvironmentVariables();
                })
                .UseStartup<Startup>();
    }
}
