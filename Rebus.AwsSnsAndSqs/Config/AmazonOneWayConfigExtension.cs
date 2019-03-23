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

    public static class AmazonOneWayConfigExtension
    {
        /// <summary>
        ///     Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
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
            standardConfigurer.OtherService<IAmazonCredentialsFactory>().Register(c => amazonCredentialsFactory);

            standardConfigurer.OtherService<IAmazonSQSTransportFactory>().Register(c => new AmazonSQSTransportFactory(c.Get<IAmazonInternalSettings>()));
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

                    return new PipelineStepInjector(pipeline).OnSend(new SnsAttributeMapperOutBoundStep(p), PipelineRelativePosition.Before, typeof(AssignDefaultHeadersStep));
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

    public class SnsAttributeMapperBuilder
    {
        private List<ISnsAttributeMapper> _snsAttributeMappers = new List<ISnsAttributeMapper>();

        public void AddMap<T>(Func<T, IDictionary<string, MessageAttributeValue>> func)
        {
            func = func ?? throw new ArgumentNullException(nameof(func));
            _snsAttributeMappers.Add(new SnsAttributeMapper<T>(func));
        }

        internal List<ISnsAttributeMapper> GetSnsAttributeMaping() => _snsAttributeMappers;
    }

    public class SnsAttributeMapperOutBoundStep : IOutgoingStep
    {
        public const string SnsAttributeKey = nameof(SnsAttributeKey);

        private readonly ISnsAttributeMapperFactory _snsAttributeMapperFactory;

        public SnsAttributeMapperOutBoundStep(IResolutionContext context)
        {
            _snsAttributeMapperFactory = context.Get<ISnsAttributeMapperFactory>();
        }

        /// <summary>
        /// Carries out whichever logic it takes to do something good for the outgoing message :)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var messageContext = MessageContext.Current;

            var body = messageContext.Message.Body;

            var processor = _snsAttributeMapperFactory.Create(body.GetType());

            if (processor != null)
            {
                var attributes = processor.GetAttributes(body);

                if (attributes.Count > 10)
                {
                    throw new InvalidOperationException($"You can only map up to 10 attributes with an sns message. The number of attributes mapped is {attributes.Count} and the keys are {string.Join(", ", attributes.Keys)}");
                }

                context.Save(SnsAttributeKey, attributes);
            }

            await next();
        }
    }

    internal interface ISnsAttributeMapperFactory
    {
        /// <summary>
        /// Creates the specified message value.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        ISnsAttributeMapper Create(Type messageType);
    }

    internal class SnsAttributeMapperFactory : ISnsAttributeMapperFactory
    {
        private readonly ReadOnlyDictionary<Type, ISnsAttributeMapper> _mapperLookup;

        public SnsAttributeMapperFactory(List<ISnsAttributeMapper> snsAndSqsAttributeMappers)
        {
            _mapperLookup = new ReadOnlyDictionary<Type, ISnsAttributeMapper>(snsAndSqsAttributeMappers.ToDictionary(k => k.MapperForType, v => v));
        }

        public ISnsAttributeMapper Create(Type messageType)
        {
            return _mapperLookup.ContainsKey(messageType) ? _mapperLookup[messageType] : null;
        }
    }

    internal interface ISnsAttributeMapper
    {
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        IDictionary<string, MessageAttributeValue> GetAttributes(object value);

        /// <summary>
        ///   Gets the value of the mapper for.
        /// </summary>
        /// <value>
        ///   The value of the mapper for.
        /// </value>
        Type MapperForType { get; }
    }

    public class SnsAttributeMapper<T> : ISnsAttributeMapper
    {
        private readonly Func<T, IDictionary<string, MessageAttributeValue>> _map;

        public SnsAttributeMapper(Func<T, IDictionary<string, MessageAttributeValue>> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public Type MapperForType => typeof(T);

        public IDictionary<string, MessageAttributeValue> GetAttributes(object value)
        {
            if (value is T valueOfT)
            {
                return _map(valueOfT);
            }

            throw new ArgumentOutOfRangeException(nameof(value), $"Expected type of {typeof(T).FullName} and was passed in {value?.GetType()?.FullName ?? "a null value"}");
        }
    }
}
