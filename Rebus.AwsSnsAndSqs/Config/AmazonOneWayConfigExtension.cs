namespace Rebus.AwsSnsAndSqs.Config
{
    using System;
    using Amazon;
    using global::Amazon;
    using global::Amazon.Runtime;
    using global::Amazon.SQS;
    using Logging;
    using Pipeline;
    using Pipeline.Receive;
    using Rebus.Config;
    using Threading;
    using Timeouts;
    using Transport;

    public static class AmazonOneWayConfigExtension
    {
        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQSAsOneWayClient(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint, AmazonSQSTransportOptions options = null)
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            var config = new AmazonSQSConfig { RegionEndpoint = regionEndpoint };

            ConfigureOneWayClient(configurer, new StaticAmazonCredentialsFactory(credentials), config, options ?? new AmazonSQSTransportOptions());
        }

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQSAsOneWayClient(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey, AmazonSQSConfig amazonSqsConfig, AmazonSQSTransportOptions options = null)
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);

            ConfigureOneWayClient(configurer, new StaticAmazonCredentialsFactory(credentials), amazonSqsConfig, options ?? new AmazonSQSTransportOptions());
        }

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQSAsOneWayClient(this StandardConfigurer<ITransport> configurer, AWSCredentials credentials, AmazonSQSConfig config, AmazonSQSTransportOptions options = null)
        {
            ConfigureOneWayClient(configurer, new StaticAmazonCredentialsFactory(credentials), config, options ?? new AmazonSQSTransportOptions());
        }

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSQSAsOneWayClient(this StandardConfigurer<ITransport> configurer, AWSCredentials credentials, RegionEndpoint regionEndpoint, AmazonSQSTransportOptions options = null)
        {
            var config = new AmazonSQSConfig { RegionEndpoint = regionEndpoint };

            ConfigureOneWayClient(configurer, new StaticAmazonCredentialsFactory(credentials), config, options ?? new AmazonSQSTransportOptions());
        }

        private static void ConfigureOneWayClient(StandardConfigurer<ITransport> configurer, IAmazonCredentialsFactory amazonCredentialsFactory, AmazonSQSConfig amazonSqsConfig, AmazonSQSTransportOptions options)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (amazonCredentialsFactory == null) throw new ArgumentNullException(nameof(amazonCredentialsFactory));
            if (amazonSqsConfig == null) throw new ArgumentNullException(nameof(amazonSqsConfig));
            if (options == null) throw new ArgumentNullException(nameof(options));

            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();

                return new AmazonSQSTransport(null, amazonCredentialsFactory, amazonSqsConfig, rebusLoggerFactory, asyncTaskFactory, options);
            });

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);

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