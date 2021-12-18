using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PostgresEntities.Entities;
using RabbitMQ.Client;
using TopDownGameServer;
using TopDownGrpcGameServer.Services;

namespace TopDownGrpcGameServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logic.Initialize();
            
            PingService ps = new PingService();
            ps.SendToMainServerThisServer();
            ps.StartListen();

            CreateHostBuilder(args).Build().Run();
            ps.EndListen();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any,
                            Convert.ToInt32(ConfigurationManager.AppSettings.Get("GrpcPort")));
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
