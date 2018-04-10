using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Logging;
using Rebus.Threading;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonInternalSettings : IAmazonSnsSettings, IAmazonSqsSettings
    {
        IAsyncTaskFactory AsyncTaskFactory { get; }
        IRebusLoggerFactory RebusLoggerFactory { get;}
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonPeekLockDuration AmazonPeekLockDuration { get; }
        AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; }
        AmazonSnsAndSqsTransportOptions AmazonSnsAndSqsTransportOptions { get; }
        AmazonSQSConfig AmazonSqsConfig { get; }
        string InputQueueAddress { get; }
        AmazonTransportMessageSerializer MessageSerializer { get; }
    }
}