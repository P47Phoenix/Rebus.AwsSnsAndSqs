using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Serilog;
using Topic.Contracts;

namespace MessangerConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile("log-{date}.txt")
                .CreateLogger();
            var queueName = $"{Environment.MachineName}_{Process.GetCurrentProcess().Id}".ToLowerInvariant();
            using (var builtinHandlerActivator = new BuiltinHandlerActivator())
            {
                builtinHandlerActivator.Handle<MessengerMessage>(message =>
                {
                    // igonore message we sent
                    if (message.Sender == queueName)
                    {
                        return Task.CompletedTask;
                    }
                    Console.WriteLine();
                    Console.WriteLine($"{message.CreateDateTime:g}: {message.Message}");
                    Console.Write("message:");
                    return Task.CompletedTask;
                });

                var bus = Configure
                    .With(builtinHandlerActivator)
                    .Logging(configurer => configurer.Serilog(Log.Logger))
                    .Transport(t =>
                    {
                        // set the worker queue name
                        t.UseAmazonSnsAndSqs(workerQueueAddress: queueName);
                    })
                    .Routing(r =>
                    {
                        // Map the message type to the queue
                        r.TypeBased().Map<MessengerMessage>(queueName);
                    })
                    .Start();

                // add the current queue to the MessengerMessage topic
                await bus.Subscribe<MessengerMessage>();

                var line = String.Empty;
                do
                {
                    Console.Write("message:");
                    line = Console.ReadLine();

                    // publish a message to the MessengerMessage topic
                    await bus.Publish(new MessengerMessage
                    {
                        CreateDateTime = DateTime.Now,
                        Message = line,
                        Sender = queueName
                    });
                }
                while (string.IsNullOrWhiteSpace(line) == false);

                // remove the queue from the topic
                await bus.Unsubscribe<MessengerMessage>();
            }
        }
    }
}
