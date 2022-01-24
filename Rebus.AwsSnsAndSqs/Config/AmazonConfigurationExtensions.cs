using System;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.RebusAmazon;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Subscriptions;
using Rebus.Timeouts;
using Rebus.Transport;

// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.AwsSnsAndSqs.Config
{
    using Pipeline.Send;
    using Rebus.Time;
    using RebusAmazon.Send;

    /// <summary>
    ///     Configuration extensions for the Amazon Simple Queue Service transport
    /// </summary>
    public static class AmazonConfigurationExtensions
    {
        /// <summary>
        ///     Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSnsAndSqs(this StandardConfigurer<ITransport> configurer, IAmazonCredentialsFactory amazonCredentialsFactory = null, AmazonSQSConfig amazonSqsConfig = null, AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig = null, string workerQueueAddress = "input_queue_address", AmazonSnsAndSqsTransportOptions amazonSnsAndSqsTransportOptions = null, ITopicFormatter topicFormatter = null, SnsAttributeMapperBuilder snsAttributeMapperBuilder = null)
        {
            configurer = configurer ?? throw new ArgumentNullException(nameof(configurer));

            topicFormatter = topicFormatter ?? new ConventionBasedTopicFormatter();
            amazonCredentialsFactory = amazonCredentialsFactory ?? new FailbackAmazonCredentialsFactory();
            amazonSqsConfig = amazonSqsConfig ?? new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USWest2 };
            amazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? new AmazonSnsAndSqsTransportOptions();
            snsAttributeMapperBuilder = snsAttributeMapperBuilder ?? new SnsAttributeMapperBuilder();
            amazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? new AmazonSimpleNotificationServiceConfig { RegionEndpoint = RegionEndpoint.USWest2 };
            Configure(configurer, amazonCredentialsFactory, amazonSqsConfig, amazonSimpleNotificationServiceConfig, workerQueueAddress, amazonSnsAndSqsTransportOptions, topicFormatter, snsAttributeMapperBuilder);
        }

        private static void Configure(StandardConfigurer<ITransport> configurer, IAmazonCredentialsFactory amazonCredentialsFactory, AmazonSQSConfig amazonSqsConfig, AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig, string inputQueueAddress, AmazonSnsAndSqsTransportOptions amazonSnsAndSqsTransportOptions, ITopicFormatter topicFormatter, SnsAttributeMapperBuilder snsAttributeMapperBuilder)
        {
            amazonCredentialsFactory = amazonCredentialsFactory ?? throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            configurer.OtherService<IAmazonCredentialsFactory>().Register(c => amazonCredentialsFactory);

            configurer.OtherService<IAmazonSQSTransportFactory>().Register(c => new AmazonSQSTransportFactory(c.Get<IAmazonInternalSettings>(), c.Get<IRebusTime>()));
            configurer.OtherService<IAmazonInternalSettings>().Register(c => new AmazonInternalSettings { ResolutionContext = c, AmazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? throw new ArgumentNullException(nameof(amazonSimpleNotificationServiceConfig)), InputQueueAddress = inputQueueAddress ?? throw new ArgumentNullException(nameof(inputQueueAddress)), AmazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig)), AmazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? throw new ArgumentNullException(nameof(amazonSnsAndSqsTransportOptions)), MessageSerializer = new AmazonTransportMessageSerializer(), TopicFormatter = topicFormatter ?? throw new ArgumentNullException(nameof(topicFormatter)) });

            configurer.Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            configurer.OtherService<ISubscriptionStorage>().Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            var snsAttributeMappers = snsAttributeMapperBuilder.GetSnsAttributeMaping();

            if (snsAttributeMappers.Count > 0)
            {
                configurer.OtherService<ISnsAttributeMapperFactory>().Register(context => new SnsAttributeMapperFactory(snsAttributeMappers));

                configurer.OtherService<IPipeline>().Decorate(p =>
                {
                    var pipeline = p.Get<IPipeline>();

                    return new PipelineStepInjector(pipeline).OnSend(new SnsAttributeMapperOutBoundStep(p), PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
                });
            }
            if (amazonSnsAndSqsTransportOptions.UseNativeDeferredMessages)
            {
                configurer.OtherService<IPipeline>().Decorate(p =>
                {
                    var pipeline = p.Get<IPipeline>();
                    return new PipelineStepRemover(pipeline).RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
                });

                configurer.OtherService<ITimeoutManager>().Register(c => new AmazonDisabledTimeoutManager(), description: AmazonConstaints.SqsTimeoutManagerText);
            }
        }
    }
}
