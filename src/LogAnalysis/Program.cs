using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogAnalysis
{
    class Program
    {
        static IDictionary<string, int> LogCount = new Dictionary<string, int>()
        {
            {LogLevel.Info, 0},
            {LogLevel.Warning, 0},
            {LogLevel.Error, 0},
            {LogLevel.Critical, 0},
        };

        static async Task Main(string[] args)
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            var task = Task.Run(async delegate
            {   
                // Were we already canceled?
                ct.ThrowIfCancellationRequested();

                while (!ct.IsCancellationRequested)
                {
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine($"Info: {LogCount[LogLevel.Info]} logs");
                    Console.WriteLine($"Warning: {LogCount[LogLevel.Warning]} logs");
                    Console.WriteLine($"Error: {LogCount[LogLevel.Error]} logs");
                    Console.WriteLine($"Critical: {LogCount[LogLevel.Critical]} logs");
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);    
                }
            }, ct);



            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Direct);

                {
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Info);
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        LogCount[LogLevel.Info]++;
                    };
                    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                }

                {
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Warning);
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        LogCount[LogLevel.Warning]++;
                    };
                    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                }

                {
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Error);
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        LogCount[LogLevel.Error]++;
                    };
                    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                }

                {
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName,  exchange: "logs", routingKey: LogLevel.Critical);
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        LogCount[LogLevel.Critical]++;
                    };
                    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                }

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }

            

            tokenSource.Cancel();

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                tokenSource.Dispose();
            }
        }
    }
}
