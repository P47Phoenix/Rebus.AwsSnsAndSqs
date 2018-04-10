using Rebus.AwsSnsAndSqs.RebusAmazon;

namespace Rebus.AwsSnsAndSqs.Config
{
    using System;
    using global::Amazon;
    using global::Amazon.Runtime;
    using global::Amazon.SimpleNotificationService;
    using global::Amazon.SQS;
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
            topicFormatter = topicFormatter ?? new DefualtTopicFormatter();
            amazonCredentialsFactory = amazonCredentialsFactory ?? new FailbackAmazonCredentialsFactory();
            amazonSqsConfig = amazonSqsConfig ?? new AmazonSQSConfig();
            options = options ?? new AmazonSnsAndSqsTransportOptions();
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
            amazonCredentialsFactory = amazonCredentialsFactory ??
                                       throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            standardConfigurer
                .OtherService<IAmazonCredentialsFactory>()
                .Register(c => amazonCredentialsFactory);

            standardConfigurer.OtherService<IAmazonInternalSettings>()
                .Register(c => new AmazonInternalSettings
                {
                    ResolutionContext = c,
                    InputQueueAddress = null,
                    AmazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig)),
                    AmazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? throw new ArgumentNullException(nameof(amazonSnsAndSqsTransportOptions)),
                    MessageSerializer = new AmazonTransportMessageSerializer(),
                    AmazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? throw new ArgumentNullException(nameof(amazonSimpleNotificationServiceConfig)),
                    TopicFormatter = topicFormatter
                });
            
 
            standardConfigurer.Register(c => new AmazonSQSTransport(c.Get<IAmazonInternalSettings>()));

            standardConfigurer
                .OtherService<ISubscriptionStorage>();
            
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
                    .Register(c => new AmazonDisabledTimeoutManager(), description: AmazonConstaints.SqsTimeoutManagerText);
            }
        }
    }
}