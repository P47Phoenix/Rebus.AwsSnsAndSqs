namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Logging;
    using Receive;
    using Threading;

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
