﻿using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Logging;
using Rebus.Threading;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    using global::Amazon;
    using Rebus.Injection;
    using Threading.TaskParallelLibrary;

    internal class AmazonInternalSettings : IAmazonInternalSettings
    {
        private IAmazonCredentialsFactory m_AmazonCredentialsFactory = null;
        private IRebusLoggerFactory m_RebusLoggerFactory = null;
        private IAsyncTaskFactory m_AsyncTaskFactory = null;

        public AmazonInternalSettings()
        {
            ResolutionContext = null;
            AmazonPeekLockDuration = new AmazonPeekLockDuration();
            AmazonSimpleNotificationServiceConfig = new AmazonSimpleNotificationServiceConfig()
            {
                RegionEndpoint = RegionEndpoint.USWest2
            };
            AmazonSnsAndSqsTransportOptions = new AmazonSnsAndSqsTransportOptions();
            AmazonSqsConfig = new AmazonSQSConfig()
            {
                RegionEndpoint = RegionEndpoint.USWest2
            };
            InputQueueAddress = null;
            MessageSerializer = new AmazonTransportMessageSerializer();
        }

        public AmazonInternalSettings(ConsoleLoggerFactory consoleLoggerFactory = null, TplAsyncTaskFactory tplAsyncTaskFactory = null, IAmazonCredentialsFactory amazonCredentialsFactory = null) : this()
        {
            m_RebusLoggerFactory = consoleLoggerFactory;
            m_AsyncTaskFactory = tplAsyncTaskFactory;
            m_AmazonCredentialsFactory = amazonCredentialsFactory;
        }

        public string InputQueueAddress { get; internal set; }
        public AmazonSQSConfig AmazonSqsConfig { get; internal set; }
        public AmazonSnsAndSqsTransportOptions AmazonSnsAndSqsTransportOptions { get; internal set; }
        public AmazonPeekLockDuration AmazonPeekLockDuration { get; }
        public AmazonTransportMessageSerializer MessageSerializer { get; internal set; }
        public AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; internal set; }
        public string TopicAddress { get; internal set; }

        public IResolutionContext ResolutionContext { get; set; }

        public IAmazonCredentialsFactory AmazonCredentialsFactory => m_AmazonCredentialsFactory = m_AmazonCredentialsFactory ?? (ResolutionContext?.Get<IAmazonCredentialsFactory>() ?? new FailbackAmazonCredentialsFactory());

        public IRebusLoggerFactory RebusLoggerFactory => m_RebusLoggerFactory = m_RebusLoggerFactory ?? (ResolutionContext?.Get<IRebusLoggerFactory>() ?? new ConsoleLoggerFactory(true));

        public IAsyncTaskFactory AsyncTaskFactory => m_AsyncTaskFactory = m_AsyncTaskFactory ?? (ResolutionContext.Get<IAsyncTaskFactory>() ?? new TplAsyncTaskFactory(RebusLoggerFactory));
    }
}