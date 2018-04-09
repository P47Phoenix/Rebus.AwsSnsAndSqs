using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Logging;
using Rebus.Threading;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    internal class AmazonInternalSettings : IAmazonSnsSettings, IAmazonSqsSettings
    {
        public IAmazonCredentialsFactory AmazonCredentialsFactory { get; internal set; }
        public string InputQueueAddress { get; internal set; }
        public AmazonSQSConfig AmazonSqsConfig { get; internal set; }
        public AmazonSQSTransportOptions AmazonSQSTransportOptions { get; internal set; }
        public IRebusLoggerFactory RebusLoggerFactory { get; internal set; }
        public AmazonPeekLockDuration AmazonPeekLockDuration { get; internal set; }
        public AmazonTransportMessageSerializer MessageSerializer { get; internal set; }
        public IAsyncTaskFactory AsyncTaskFactory { get; internal set; }
        public AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; internal set; }
    }

    internal interface IAmazonSqsSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonSQSConfig AmazonSqsConfig { get; }
        AmazonSQSTransportOptions AmazonSQSTransportOptions { get; }
    }

    internal interface IAmazonSnsSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; }
    }
}