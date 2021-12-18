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

namespace TopDownGrpcGameServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logic.Initialize();
            SendToMainServerThisServer();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any,
                            Convert.ToInt32(5000));
                    });
                    webBuilder.UseStartup<Startup>();
                });

        private static void SendToMainServerThisServer()
        {
            var factory = new ConnectionFactory()
            {
                HostName = ConfigurationManager.AppSettings.Get("RabbitMQHostName"),
                UserName = ConfigurationManager.AppSettings.Get("RabbitMQUserName"),
                Password = ConfigurationManager.AppSettings.Get("RabbitMQPassword"),
                // Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("RabbitMQPort")) //tls port
            };

            //factory.Ssl.Enabled = true;
            //factory.Ssl.ServerName = "NEWSPEED";

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: ConfigurationManager.AppSettings.Get("RabbitMQServerQueue"), durable: false, exclusive: false, autoDelete: false, arguments: null);

                Server thisServer = new Server()
                {
                    Address = ConfigurationManager.AppSettings.Get("GameServerIp"),
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("GameServerPingPort")),
                    Status = 0,
                };

                string str = JsonConvert.SerializeObject(thisServer, Formatting.Indented);

                var body = Encoding.UTF8.GetBytes(str);
                channel.BasicPublish(exchange: "", routingKey: ConfigurationManager.AppSettings.Get("RabbitMQServerQueue"), basicProperties: null, body: body);
            }
        }
    }
}
