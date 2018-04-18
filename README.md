# Rebus.AwsSnsAndSqs
Implement aws sns and sqs provider for [Rebus](https://github.com/rebus-org/Rebus)

## Target Framworks

Framwork       | What works
-------------- | ----------
netstandard1.3 | Everything exept attribute based topics
netstandard2.0 | Everything
net45          | Everything


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

## Don't like the convention based topic approach?
Make your own topic formater based using the ITopicFormatter
```csharp
public interface ITopicFormatter
{
    string FormatTopic(string topic);
}
```

Topic formaters are set when configuring the transport
Below you can see that we are going to us the attirbute based formated.
```csharp
Configure
    .With(activator)
    .Transport(t => 
    { 
        t.UseAmazonSnsAndSqs(workerQueueAddress: queueName, topicFormatter: new YourCustomTopicFormatter()); 
    })
    .Routing(r => r.TypeBased().Map<string>(queueName))
    .Start();
```

Attribute based topic formater allows you to set an attributes for the topic name
```csharp
using Rebus.AwsSnsAndSqs;

namespace Rebus.AwsSnsAndSqsTests
{
    [TopicName(nameof(SomeMessageTopic))]
    public class SomeMessageTopic
    {
        public string Message { get; set; }
    }
}
``` 

Setup the transport to use attribute based topic
```csharp
Configure
    .With(activator)
    .Transport(t => 
    { 
        t.UseAmazonSnsAndSqs(workerQueueAddress: queueName, topicFormatter: new AttributeBasedTopicFormatter()); 
    })
    .Routing(r => r.TypeBased().Map<string>(queueName))
    .Start();
```

## Load and perfomance tests
Load test results are located [here](LoadResults.md)
Results may vary depending on network latency, region, cpu, etc.

## Permissions
The permissions needed and example polcy documents are [here](PERMISSIONS.md)

## Contribute

Rebus sns and sqs is an [inner source](https://en.wikipedia.org/wiki/Inner_source) project.+
Pull requests are welcomed from anyone in Cox Automotive.
[Here's how to contribute](CONTRIBUTE.md).