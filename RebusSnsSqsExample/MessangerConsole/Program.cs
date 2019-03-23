using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.AwsSnsAndSqs.RebusAmazon;
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
                .WriteTo.RollingFile("log-.txt")
                .CreateLogger();
            var queueName = $"{Environment.MachineName}_{Process.GetCurrentProcess().Id}".ToLowerInvariant();

            var workHandlerActivator = new BuiltinHandlerActivator();

            var clientActivator = new BuiltinHandlerActivator();

            using (var builtinHandlerActivator = new DisposeChain(clientActivator, workHandlerActivator))
            {

                // Setup worker bus
                workHandlerActivator.Handle<Rebus.AwsSnsAndSqs.RebusAmazon.VinEventMessage>(message =>
                {
                    // igonore message we sent
                    if (message.Source == queueName)
                    {
                        return Task.CompletedTask;
                    }
                    Console.WriteLine();
                    Console.WriteLine($"{message.TimeStamp:g}: {message.Message}");
                    Console.Write("message:");
                    return Task.CompletedTask;
                });

                var worker = Configure
                    .With(workHandlerActivator)
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
                await worker.Subscribe<MessengerMessage>();

                // setup a client
                var client = Configure
                    .With(clientActivator)
                    .Logging(configurer => configurer.Serilog(Log.Logger))
                    .Transport(t =>  t.UseAmazonSnsAndSqsAsOneWayClient())                   
                    .Start();

                var line = String.Empty;
                do
                {
                    Console.Write("message:");
                    line = Console.ReadLine();

                    // publish a message to the MessengerMessage topic
                    await client.PublishEvent(new VinEventMessage
                        {
                            Message = line,
                            EventId = Guid.NewGuid(),
                            MessageVersion = new Version(1, 0),
                            Source = "RebusSnsSqsExample.Application",
                            MessageType = "Test.Test",
                        });                  
                }
                while (string.IsNullOrWhiteSpace(line) == false);

                // remove the worker queue from the topic
                await worker.Unsubscribe<MessengerMessage>();
            }
        }
    }

}


