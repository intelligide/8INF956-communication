using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLipsum.Core;
using RabbitMQ.Client;

namespace LogEmitter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IModel channel = null;
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            var task = new Task(async delegate
            {   
                ct.ThrowIfCancellationRequested();

                Random random = new Random();
                LipsumGenerator lipsum = new LipsumGenerator();

                while (!ct.IsCancellationRequested)
                {
                    if(channel != null)
                    {
                        string[] generatedSentences = lipsum.GenerateSentences(1, Sentence.Medium);
                        var body = Encoding.UTF8.GetBytes(generatedSentences[0]);

                        string level = "";
                        switch(random.Next(0, 4))
                        {
                        case 0:
                            level = LogLevel.Info;
                            break;
                        case 1:
                            level = LogLevel.Warning;
                            break;
                        case 2:
                            level = LogLevel.Error;
                            break;
                        case 3:
                            level = LogLevel.Critical;
                            break;
                        default:
                            level = "";
                            break;
                        }

                        channel.BasicPublish(exchange: "logs", routingKey: level, basicProperties: null, body: body);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(random.Next(500, 3000)), ct);
                }
            }, ct);


            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (channel = connection.CreateModel())
            {
                channel.ExchangeDeclare("logs", ExchangeType.Direct);

                task.Start();

                Console.WriteLine("Press [enter] to exit.");
                Console.ReadLine();

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
            channel = null;
        }
    }
}
