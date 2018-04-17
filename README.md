# Rebus.AwsSnsAndSqs
Implement aws sns and sqs provider for [Rebus](https://github.com/rebus-org/Rebus)

## Contract based pubsub

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

Full example located here [here](FullExample.md)

## Load and perfomance tests
Results may vary depending on network latency, region, cpu, etc.

[Load test summary](LoadResults.md)

## Permissions
The permissions needed and example polcy documents are [here](PERMISSIONS.md)

## Contribute

Rebus sns and sqs is an [inner source](https://en.wikipedia.org/wiki/Inner_source) project.+
Pull requests are welcomed from anyone in Cox Automotive.
[Here's how to contribute](CONTRIBUTE.md).