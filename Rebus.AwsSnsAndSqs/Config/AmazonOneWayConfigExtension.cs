using System;
using System.Threading.Tasks;
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

namespace Rebus.AwsSnsAndSqs.Config
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Amazon.SimpleNotificationService.Model;
    using Injection;
    using Pipeline.Send;
    using Rebus.Messages;
    using Rebus.Time;
    using RebusAmazon.Send;
    using Retry;

    public static class AmazonOneWayConfigExtension
    {
        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        /// <param name="configurer">The configurer.</param>
        /// <param name="amazonCredentialsFactory">The amazon credentials factory.</param>
        /// <param name="amazonSqsConfig">The amazon SQS configuration.</param>
        /// <param name="amazonSimpleNotificationServiceConfig">The amazon simple notification service configuration.</param>
        /// <param name="options">The options.</param>
        /// <param name="topicFormatter">The topic formatter.</param>
        /// <param name="snsAttributeMapperBuilder">The SNS attribute mapper builder.</param>
        public static void UseAmazonSnsAndSqsAsOneWayClient(this StandardConfigurer<ITransport> configurer, IAmazonCredentialsFactory amazonCredentialsFactory = null, AmazonSQSConfig amazonSqsConfig = null, AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig = null, AmazonSnsAndSqsTransportOptions options = null, ITopicFormatter topicFormatter = null, SnsAttributeMapperBuilder snsAttributeMapperBuilder = null)
        {
            topicFormatter = topicFormatter ?? new ConventionBasedTopicFormatter();
            amazonCredentialsFactory = amazonCredentialsFactory ?? new FailbackAmazonCredentialsFactory();
            amazonSqsConfig = amazonSqsConfig ?? new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USWest2 };
            options = options ?? new AmazonSnsAndSqsTransportOptions();
            snsAttributeMapperBuilder = snsAttributeMapperBuilder ?? new SnsAttributeMapperBuilder();
            amazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? new AmazonSimpleNotificationServiceConfig { RegionEndpoint = RegionEndpoint.USWest2 };
            ConfigureOneWayClient(configurer, amazonCredentialsFactory, amazonSqsConfig, amazonSimpleNotificationServiceConfig, options, topicFormatter, snsAttributeMapperBuilder);
        }

        private static void ConfigureOneWayClient(StandardConfigurer<ITransport> standardConfigurer, IAmazonCredentialsFactory amazonCredentialsFactory, AmazonSQSConfig amazonSqsConfig, AmazonSimpleNotificationServiceConfig amazonSimpleNotificationServiceConfig, AmazonSnsAndSqsTransportOptions amazonSnsAndSqsTransportOptions, ITopicFormatter topicFormatter, SnsAttributeMapperBuilder snsAttributeMapperBuilder)
        {
            amazonCredentialsFactory = amazonCredentialsFactory ?? throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            standardConfigurer.OtherService<IErrorHandler>().Register(c => new OneWayClientErrorHandler());
            standardConfigurer.OtherService<IAmazonCredentialsFactory>().Register(c => amazonCredentialsFactory);

            standardConfigurer.OtherService<IAmazonSQSTransportFactory>().Register(c => new AmazonSQSTransportFactory(c.Get<IAmazonInternalSettings>(), c.Get<IRebusTime>()));
            standardConfigurer.OtherService<IAmazonInternalSettings>().Register(c => new AmazonInternalSettings { ResolutionContext = c, AmazonSimpleNotificationServiceConfig = amazonSimpleNotificationServiceConfig ?? throw new ArgumentNullException(nameof(amazonSimpleNotificationServiceConfig)), InputQueueAddress = null, AmazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig)), AmazonSnsAndSqsTransportOptions = amazonSnsAndSqsTransportOptions ?? throw new ArgumentNullException(nameof(amazonSnsAndSqsTransportOptions)), MessageSerializer = new AmazonTransportMessageSerializer(), TopicFormatter = topicFormatter ?? throw new ArgumentNullException(nameof(topicFormatter)) });

            standardConfigurer.Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            standardConfigurer.OtherService<ISubscriptionStorage>().Register(c => c.Get<IAmazonSQSTransportFactory>().Create());

            OneWayClientBackdoor.ConfigureOneWayClient(standardConfigurer);

            var snsAttributeMappers = snsAttributeMapperBuilder.GetSnsAttributeMaping();

            if (snsAttributeMappers.Count > 0)
            {
                standardConfigurer.OtherService<ISnsAttributeMapperFactory>().Register(context => new SnsAttributeMapperFactory(snsAttributeMappers));

                standardConfigurer.OtherService<IPipeline>().Decorate(p =>
                {
                    var pipeline = p.Get<IPipeline>();

                    return new PipelineStepInjector(pipeline).OnSend(new SnsAttributeMapperOutBoundStep(p), PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
                });
            }

            if (amazonSnsAndSqsTransportOptions.UseNativeDeferredMessages)
            {
                standardConfigurer.OtherService<IPipeline>().Decorate(p =>
                {
                    var pipeline = p.Get<IPipeline>();

                    return new PipelineStepRemover(pipeline).RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
                });

                standardConfigurer.OtherService<ITimeoutManager>().Register(c => new AmazonDisabledTimeoutManager(), AmazonConstaints.SqsTimeoutManagerText);
            }
        }
    }
}
