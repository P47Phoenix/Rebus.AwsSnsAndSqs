using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Amazon;
using Rebus.AwsSnsAndSqs.Amazon.SQS;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Threading;
using Rebus.Timeouts;
using Rebus.Transport;

// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.AwsSnsAndSqs.Config
{
    using global::Amazon.SimpleNotificationService;
    using Subscriptions;

    /// <summary>
    /// Configuration extensions for the Amazon Simple Queue Service transport
    /// </summary>
    public static class AmazonConfigurationExtensions
    {

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSnsAndSqs(
            this StandardConfigurer<ITransport> configurer,
            IAmazonCredentialsFactory amazonCredentialsFactory = null,
            AmazonSQSConfig amazonSqsConfig = null,
            AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig = null,
            string workerQueueAddress = "input_queue_address",
            string topicAddress = "topic_address",
            AmazonSnsAndSqsTransportOptions amazonSnsAndSqsTransportOptions = null)
        {
            amazonCredentialsFactory = amazonCredentialsFactory ?? new FailbackAmazonCredentialsFactory();
            amazonSqsConfig = amazonSqsConfig ?? new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.USWest2
            };
            amazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? new AmazonSnsAndSqsTransportOptions();
            amazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? new AmazonSimpleNotificationServiceConfig
            {
                RegionEndpoint = RegionEndpoint.USWest2
            };
            Configure(configurer, amazonCredentialsFactory, amazonSqsConfig, amazonSimpleNotificationServiceConfig, workerQueueAddress, topicAddress, amazonSnsAndSqsTransportOptions);
        }

        static void Configure(
            StandardConfigurer<ITransport> configurer,
            IAmazonCredentialsFactory amazonCredentialsFactory,
            AmazonSQSConfig amazonSqsConfig,
            AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig,
            string inputQueueAddress,
            string topicAddress,
            AmazonSnsAndSqsTransportOptions amazonSnsAndSqsTransportOptions)
        {
            amazonCredentialsFactory = amazonCredentialsFactory ?? throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            configurer
                .OtherService<IAmazonCredentialsFactory>()
                .Register(c => amazonCredentialsFactory);

            configurer
                .OtherService<IAmazonSQSTransportFactory>()
                .Register(c => new AmazonSQSTransportFactory(c.Get<IAmazonInternalSettings>()));
            configurer
                .OtherService<IAmazonInternalSettings>()
                .Register(c => new AmazonInternalSettings
                {
                    ResolutionContext = c,
                    AmazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? throw new ArgumentNullException(nameof(amazonSimpleNotificationServiceConfig)),
                    InputQueueAddress = inputQueueAddress ?? throw new ArgumentNullException(nameof(inputQueueAddress)),
                    TopicAddress = topicAddress ?? throw new ArgumentNullException(nameof(topicAddress)),
                    AmazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig)),
                    AmazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? throw new ArgumentNullException(nameof(amazonSnsAndSqsTransportOptions)),
                    MessageSerializer = new AmazonTransportMessageSerializer()
                });

            configurer.Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            if (amazonSnsAndSqsTransportOptions.UseNativeDeferredMessages)
            {
                configurer
                    .OtherService<IPipeline>()
                    .Decorate(p =>
                    {
                        var pipeline = p.Get<IPipeline>();
                        return new PipelineStepRemover(pipeline)
                            .RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
                    });

                configurer.OtherService<ITimeoutManager>()
                    .Register(c => new AmazonDisabledTimeoutManager(), description: AmazonConstaints.SqsTimeoutManagerText);
            }
        }
    }
}
