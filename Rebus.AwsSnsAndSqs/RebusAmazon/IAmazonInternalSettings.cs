using Amazon.SimpleNotificationService;
using Amazon.SQS;
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
    }
}
