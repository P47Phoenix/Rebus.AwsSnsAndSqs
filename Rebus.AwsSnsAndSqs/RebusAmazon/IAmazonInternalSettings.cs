using System;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Logging;
using Rebus.Threading;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Receive;

    public interface IAmazonInternalSettings : IAmazonSnsSettings, IAmazonSqsSettings
    {
        IAsyncTaskFactory AsyncTaskFactory { get; }
        IRebusLoggerFactory RebusLoggerFactory { get; }
        AmazonPeekLockDuration AmazonPeekLockDuration { get; }
        string InputQueueAddress { get; }
        AmazonTransportMessageSerializer MessageSerializer { get; }
        ITopicFormatter TopicFormatter { get; }
        Action<CreateQueueRequest> PrepareCreateQueueRequest { get; }
        Action<CreateTopicRequest> PrepareCreateTopicRequest { get; }
    }
}
