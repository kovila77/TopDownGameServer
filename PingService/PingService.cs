using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PostgresEntities.Entities;
using RabbitMQ.Client;

namespace PingService
{
    public static class PingService
    {
        private static TcpListener _tcpListener;
        public static int Status { get; set; } = 1;

        public static void SendToMainServerThisServer(int status)
        {
            try
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
                    channel.QueueDeclare(queue: ConfigurationManager.AppSettings.Get("RabbitMQServerQueue"), durable: true, exclusive: false, autoDelete: false, arguments: null);

                    Server thisServer = new Server()
                    {
                        Address = ConfigurationManager.AppSettings.Get("GameServerIp"),
                        Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("GrpcPort")),
                        PingPort = Convert.ToInt32(ConfigurationManager.AppSettings.Get("GameServerPingPort")),
                        Status = status,
                    };

                    string str = JsonConvert.SerializeObject(thisServer, Formatting.Indented);

                    var body = Encoding.UTF8.GetBytes(str);
                    channel.BasicPublish(exchange: "", routingKey: ConfigurationManager.AppSettings.Get("RabbitMQServerQueue"), basicProperties: null, body: body);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void StartListen()
        {
            Task.Run(() =>
            {
                try
                {
                    _tcpListener = new TcpListener(
                        IPAddress.Any,
                        Convert.ToInt32(ConfigurationManager.AppSettings.Get("GameServerPingPort"))
                    );
                    _tcpListener.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                Console.WriteLine("Ping: waiting for a client to connect...");
                do
                {
                    TcpClient tcpClient = _tcpListener.AcceptTcpClient();

                    Task.Run(() =>
                    {
                        try
                        {
                            tcpClient.SendTimeout = 1000 * 15;
                            tcpClient.ReceiveTimeout = 1000 * 15;

                            //using BinaryReader br = new BinaryReader(tcpClient.GetStream());
                            using BinaryWriter bw = new BinaryWriter(tcpClient.GetStream());
                            bw.Write(Status);
                            Console.WriteLine("Pong");
                            tcpClient.Dispose();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            tcpClient.Dispose();
                        }
                    });
                } while (true);
            });
        }

        public static void EndListen()
        {
            _tcpListener.Stop();
        }
    }
}
