using Rebus.AwsSnsAndSqs.RebusAmazon;

namespace Rebus.AwsSnsAndSqs.Config
{
    using System;
    using Amazon;
    using Amazon.Runtime;
    using Amazon.SimpleNotificationService;
    using Amazon.SQS;
    using Logging;
    using Pipeline;
    using Pipeline.Receive;
    using Rebus.Config;
    using Subscriptions;
    using Threading;
    using Timeouts;
    using Transport;

    public static class AmazonOneWayConfigExtension
    {
        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSnsAndSqsAsOneWayClient(
            this StandardConfigurer<ITransport> configurer,
            IAmazonCredentialsFactory amazonCredentialsFactory = null,
            AmazonSQSConfig amazonSqsConfig = null,
            AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig = null,
            AmazonSnsAndSqsTransportOptions options = null,
            ITopicFormatter topicFormatter = null)
        {
            topicFormatter = topicFormatter ?? new ConventionBasedTopicFormatter();
            amazonCredentialsFactory = amazonCredentialsFactory ?? new FailbackAmazonCredentialsFactory();
            amazonSqsConfig = amazonSqsConfig ?? new AmazonSQSConfig()
            {
                RegionEndpoint = RegionEndpoint.USWest2
            };
            options = options ?? new AmazonSnsAndSqsTransportOptions();
            amazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? new AmazonSimpleNotificationServiceConfig()
            {
                RegionEndpoint = RegionEndpoint.USWest2
            };
            ConfigureOneWayClient(configurer, amazonCredentialsFactory, amazonSqsConfig, amazonSimpleNotificationServiceConfig, options, topicFormatter);
        }

        private static void ConfigureOneWayClient(
            StandardConfigurer<ITransport> standardConfigurer,
            IAmazonCredentialsFactory amazonCredentialsFactory,
            AmazonSQSConfig amazonSqsConfig,
            AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig,
            AmazonSnsAndSqsTransportOptions amazonSnsAndSqsTransportOptions,
            ITopicFormatter topicFormatter)
        {
            amazonCredentialsFactory = amazonCredentialsFactory ?? throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            standardConfigurer
                .OtherService<IAmazonCredentialsFactory>()
                .Register(c => amazonCredentialsFactory);

            standardConfigurer
                .OtherService<IAmazonSQSTransportFactory>()
                .Register(c => new AmazonSQSTransportFactory(c.Get<IAmazonInternalSettings>()));
            standardConfigurer
                .OtherService<IAmazonInternalSettings>()
                .Register(c => new AmazonInternalSettings
                {
                    ResolutionContext = c,
                    AmazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? throw new ArgumentNullException(nameof(amazonSimpleNotificationServiceConfig)),
                    InputQueueAddress = null,
                    AmazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig)),
                    AmazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? throw new ArgumentNullException(nameof(amazonSnsAndSqsTransportOptions)),
                    MessageSerializer = new AmazonTransportMessageSerializer(),
                    TopicFormatter = topicFormatter ?? throw new ArgumentNullException(nameof(topicFormatter))
                });

            standardConfigurer.Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            standardConfigurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            OneWayClientBackdoor.ConfigureOneWayClient(standardConfigurer);

            if (amazonSnsAndSqsTransportOptions.UseNativeDeferredMessages)
            {
                standardConfigurer
                    .OtherService<IPipeline>()
                    .Decorate(p =>
                    {
                        var pipeline = p.Get<IPipeline>();

                        return new PipelineStepRemover(pipeline)
                            .RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
                    });

                standardConfigurer.OtherService<ITimeoutManager>()
                    .Register(c => new AmazonDisabledTimeoutManager(), AmazonConstaints.SqsTimeoutManagerText);
            }
        }
    }
}