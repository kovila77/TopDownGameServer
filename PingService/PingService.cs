using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
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

        private static Timer Timer { get; set; }

        private static object TimerLocker = new();

        public static void SendToMainServerThisServer(int status)
        {
            try
            {
                Console.WriteLine($"Sending status: {status}");
                lock (TimerLocker)
				{
                    if (Timer == null)
                    {
                        Timer = new Timer(20 * 1000) { AutoReset = false };
                        Timer.Elapsed += (sender, e) => { SendToMainServerThisServer(Status); };
                    }

                    Timer.Stop();
                    Timer.Start();
                }
                

                var factory = new ConnectionFactory()
                {
                    HostName = Environment.GetEnvironmentVariable("TOPDOWN_RABBITMQ_HOSTNAME"),
                    UserName = Environment.GetEnvironmentVariable("TOPDOWN_RABBITMQ_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("TOPDOWN_RABBITMQ_PASSWORD"),
                    // Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("RabbitMQPort")) //tls port
                };

                //factory.Ssl.Enabled = true;
                //factory.Ssl.ServerName = "NEWSPEED";

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: Environment.GetEnvironmentVariable("TOPDOWN_RABBITMQ_SERVERQUEUE"), durable: true, exclusive: false, autoDelete: false, arguments: null);

                    Server thisServer = new Server()
                    {
                        Address = Dns.GetHostName(),
                        Port = Convert.ToInt32(Environment.GetEnvironmentVariable("TOPDOWN_GAMESERVER_GRPC_PORT")),
                        PingPort = Convert.ToInt32(Environment.GetEnvironmentVariable("TOPDOWN_GAMESERVER_PING_PORT")),
                        Status = status,
                    };

                    string str = JsonConvert.SerializeObject(thisServer, Formatting.Indented);

                    var body = Encoding.UTF8.GetBytes(str);
                    channel.BasicPublish(exchange: "", routingKey: Environment.GetEnvironmentVariable("TOPDOWN_RABBITMQ_SERVERQUEUE"), basicProperties: null, body: body);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task StartListen()
        {
            await Task.Run(() =>
            {
                try
                {
                    var ipAddresses = Array.FindAll(
                            Dns.GetHostEntry(Dns.GetHostName()).AddressList,
                            a => a.AddressFamily == AddressFamily.InterNetwork);
                    _tcpListener = new TcpListener(
                        ipAddresses.First(),
                        Convert.ToInt32(Environment.GetEnvironmentVariable("TOPDOWN_GAMESERVER_PING_PORT"))
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
                    var c = _tcpListener.AcceptTcpClient();
                    Task.Run(() => SendPong(c));
                } while (true);
            });
        }

        private static async Task SendPong(TcpClient tcpClient)
        {
            try
            {
                lock(TimerLocker)
				{
                    Timer.Stop();
                    Timer.Start();
                }
                
                tcpClient.SendTimeout = 1000 * 15;
                tcpClient.ReceiveTimeout = 1000 * 15;

                await using BinaryWriter bw = new BinaryWriter(tcpClient.GetStream());
                bw.Write(Status);
                Console.WriteLine("Pong");
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                tcpClient.Close();
            }
            tcpClient.Dispose();
        }

        public static void EndListen()
        {
            _tcpListener.Stop();
        }
    }
}
