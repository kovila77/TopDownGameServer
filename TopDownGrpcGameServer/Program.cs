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
using PingService;

namespace TopDownGrpcGameServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logic.Initialize();
            PingService.PingService.StartListen();

            CreateHostBuilder(args).Build().Run();
            PingService.PingService.EndListen();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any,
                            Convert.ToInt32(Environment.GetEnvironmentVariable("TOPDOWN_GAMESERVER_GRPC_PORT")));
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
