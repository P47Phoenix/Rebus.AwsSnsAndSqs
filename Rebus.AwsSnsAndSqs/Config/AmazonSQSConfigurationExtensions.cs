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
    /// <summary>
    /// Configuration extensions for the Amazon Simple Queue Service transport
    /// </summary>
    public static class AmazonSQSConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQS(this StandardConfigurer<ITransport> configurer, AWSCredentials credentials, AmazonSQSConfig config, string inputQueueAddress, AmazonSQSTransportOptions options = null)
        {
            Configure(configurer, new StaticAmazonCredentialsFactory(credentials), config, inputQueueAddress, options ?? new AmazonSQSTransportOptions());
        }

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQS(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint, string inputQueueAddress, AmazonSQSTransportOptions options = null)
        {
            var config = new AmazonSQSConfig { RegionEndpoint = regionEndpoint };
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);

            Configure(configurer, new StaticAmazonCredentialsFactory(credentials), config, inputQueueAddress, options ?? new AmazonSQSTransportOptions());
        }

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQS(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey, AmazonSQSConfig config, string inputQueueAddress, AmazonSQSTransportOptions options = null)
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);

            Configure(configurer, new StaticAmazonCredentialsFactory(credentials),  config, inputQueueAddress, options ?? new AmazonSQSTransportOptions());
        }

        static void Configure(StandardConfigurer<ITransport> configurer, IAmazonCredentialsFactory amazonCredentialsFactory, AmazonSQSConfig config, string inputQueueAddress, AmazonSQSTransportOptions options)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (amazonCredentialsFactory == null) throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (inputQueueAddress == null) throw new ArgumentNullException(nameof(inputQueueAddress));
            if (options == null) throw new ArgumentNullException(nameof(options));

            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();

                return new AmazonSQSTransport(inputQueueAddress, amazonCredentialsFactory, config, rebusLoggerFactory, asyncTaskFactory, options);
            });

            if (options.UseNativeDeferredMessages)
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
