using System;
using System.IO;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FileDump
{
    class Program
    {
        static void Main(string[] args)
        {
            using (StreamWriter w = File.AppendText("app.log"))
            {
                w.AutoFlush = true;							 
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using(var connection = factory.CreateConnection())
                using(var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Direct);
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Warning);
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Error);
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Critical);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        Log(w, ea.RoutingKey, message);
                    };
                    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }

            
        }

        public static void Log(TextWriter w, string level, string message)
        {
            w.WriteLine($"[{DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss")}] [{level.ToUpper()}]: {message}");
        }
    }
}
