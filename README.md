# Rebus.AwsSnsAndSqs
Implement aws sns and sqs provider for [Rebus](https://github.com/rebus-org/Rebus)

Aditional information documentation can be found (here)[]

## Contract based pubsub

The following example is in the repo and is is located [here](https://ghe.coxautoinc.com/Mike-Connelly/Rebus.AwsSnsAndSqs/tree/master/RebusSnsSqsExample)

For example this following contract
```csharp
using System;

namespace Topic.Contracts
{
    public class MessengerMessage
    {
        public string Message { get; set; }

        public DateTime CreateDateTime { get; set; }
        public string Sender { get; set; }
    }
}
```

Will create a topic like
```
Topic_Contracts_MessengerMessage--Topic_Contracts
```

In this example of pub sub the client and the worker is the same.
Each instance will create queue using the machine name and pid of the process.
```csharp
var queueName = $"{Environment.MachineName}_{Process.GetCurrentProcess().Id}".ToLowerInvariant();
```
Next we should subscribe to the topic.
```csharp
await bus.Subscribe<MessengerMessage>();
```
Finally we just publish new messages we get onto the topic
```csharp
var line = String.Empty;
do
{
    Console.Write("message:");
    line = Console.ReadLine();

    await bus.Publish(new MessengerMessage
    {
        CreateDateTime = DateTime.Now,
        Message = line,
        Sender = queueName
    });
}
while (string.IsNullOrWhiteSpace(line) == false);
```

Full example
[Source](https://ghe.coxautoinc.com/Mike-Connelly/Rebus.AwsSnsAndSqs/blob/master/RebusSnsSqsExample/MessangerConsole/Program.cs)
```csharp
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
                .WriteTo.RollingFile("log-.txt")
                .CreateLogger();
            var queueName = $"{Environment.MachineName}_{Process.GetCurrentProcess().Id}".ToLowerInvariant();

            var workHandlerActivator = new BuiltinHandlerActivator();

            var clientActivator = new BuiltinHandlerActivator();

            using (var builtinHandlerActivator = new DisposeChain(clientActivator, workHandlerActivator))
            {

                // Setup worker bus
                workHandlerActivator.Handle<MessengerMessage>(message =>
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
                    .Transport(t =>
                    {
                        // set the worker queue name
                        t.UseAmazonSnsAndSqsAsOneWayClient();
                    })
                    .Start();

                var line = String.Empty;
                do
                {
                    Console.Write("message:");
                    line = Console.ReadLine();

                    // publish a message to the MessengerMessage topic
                    await client.Publish(new MessengerMessage
                    {
                        CreateDateTime = DateTime.Now,
                        Message = line,
                        Sender = queueName
                    });
                }
                while (string.IsNullOrWhiteSpace(line) == false);

                // remove the worker queue from the topic
                await worker.Unsubscribe<MessengerMessage>();
            }
        }
    }
}
```
## Contribute

Rebus sns and sqs is an [inner source](https://en.wikipedia.org/wiki/Inner_source) project.+
Pull requests are welcomed from anyone in Cox Automotive.
[Here's how to contribute](CONTRIBUTE.md).