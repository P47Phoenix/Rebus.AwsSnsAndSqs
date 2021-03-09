using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.RebusAmazon.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Amazon.SimpleNotificationService.Model;
    using Rebus.Time;
    using Receive;
    using Send;

    /// <summary>
    ///     Implementation of <see cref="ITransport" /> that uses Amazon Simple Queue Service to move messages around
    /// </summary>
    internal class AmazonSqsTransport : IAmazonSQSTransport
    {
        private const string c_removingSqsSubscriptionMessage = "Removing sqs subscriber {0} to sns topic {1}";
        private readonly AmazonCreateSQSQueue m_amazonCreateSqsQueue;
        private readonly IAmazonInternalSettings m_AmazonInternalSettings;
        private readonly AmazonSQSQueueContext m_amazonSQSQueueContext;
        private readonly AmazonSendMessageCommandFactory m_AmazonSendMessageCommandFactory;
        private readonly AmazonSQSQueuePurgeUtility m_amazonSqsQueuePurgeUtility;
        private readonly IAmazonMessageProcessorFactory m_AmazonMessageProcessorFactory;
        private readonly AmazonRecieveMessage m_AmazonRecieveMessage;
        private readonly ILog m_log;

        /// <summary>
        ///     Constructs the transport with the specified settings
        /// </summary>
        public AmazonSqsTransport(IAmazonInternalSettings amazonInternalSettings, IRebusTime rebusTime)
        {
            m_AmazonInternalSettings = amazonInternalSettings ?? throw new ArgumentNullException(nameof(amazonInternalSettings));

            m_log = amazonInternalSettings.RebusLoggerFactory.GetLogger<AmazonSqsTransport>();

            if (amazonInternalSettings.InputQueueAddress != null)
            {
                if (amazonInternalSettings.InputQueueAddress.Contains("/") && !Uri.IsWellFormedUriString(amazonInternalSettings.InputQueueAddress, UriKind.Absolute))
                {
                    var message = $"The input queue address '{amazonInternalSettings.InputQueueAddress}' is not valid - please either use a simple queue name (eg. 'my-queue') or a full URL for the queue endpoint (e.g. 'https://sqs.eu-central-1.amazonaws.com/234234234234234/somqueue').";

                    throw new ArgumentException(message, nameof(amazonInternalSettings.InputQueueAddress));
                }
            }

            //TODO!!!!
            m_amazonSQSQueueContext = new AmazonSQSQueueContext(m_AmazonInternalSettings);
            m_AmazonSendMessageCommandFactory = new AmazonSendMessageCommandFactory(m_AmazonInternalSettings, m_amazonSQSQueueContext, rebusTime);
            m_amazonCreateSqsQueue = new AmazonCreateSQSQueue(m_AmazonInternalSettings);
            m_amazonSqsQueuePurgeUtility = new AmazonSQSQueuePurgeUtility(m_AmazonInternalSettings);
            m_AmazonMessageProcessorFactory = new AmazonMessageProcessorFactory(m_AmazonInternalSettings);
            m_AmazonRecieveMessage = new AmazonRecieveMessage(m_AmazonInternalSettings, m_amazonSQSQueueContext, m_AmazonMessageProcessorFactory, rebusTime);
        }

        /// <summary>
        ///     Initializes the transport by creating the input queue
        /// </summary>
        public void Initialize()
        {
            if (Address == null)
            {
                return;
            }

            CreateQueue(Address);
        }

        /// <summary>
        ///     Creates the queue with the given name
        /// </summary>
        public void CreateQueue(string address)
        {
            m_amazonCreateSqsQueue.CreateQueue(address);
        }

        /// <inheritdoc />
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null)
            {
                throw new ArgumentNullException(nameof(destinationAddress));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var amazonSendMessageProcessor = m_AmazonSendMessageCommandFactory.Create(destinationAddress);

            await amazonSendMessageProcessor.SendAsync(message, context);
        }

        /// <inheritdoc />
        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return await m_AmazonRecieveMessage.Receive(context, Address, cancellationToken);
        }

        /// <summary>
        ///     Gets the input queue name
        /// </summary>
        public string Address => m_AmazonInternalSettings.InputQueueAddress;

        public bool IsCentralized => true;

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            var topicArn = await m_AmazonInternalSettings.GetTopicArn(topic);
            m_log.Debug("using sns topic {0} for topic contract {1}", topicArn, topic);
            return new[] { topicArn };
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            m_log.Debug("Adding sqs subscriber {0} to sns topic {1}", subscriberAddress, topic);
            using (var rebusTransactionScope = new RebusTransactionScope())
            {
                var topicArn = await m_AmazonInternalSettings.GetTopicArn(topic, rebusTransactionScope);

                var destinationQueueUrlByName = m_amazonSQSQueueContext.GetDestinationQueueUrlByName(subscriberAddress, rebusTransactionScope.TransactionContext);

                var sqsInformation = m_amazonSQSQueueContext.GetSqsInformationFromUri(destinationQueueUrlByName);

                var snsClient = m_AmazonInternalSettings.CreateSnsClient(rebusTransactionScope.TransactionContext);

                var listSubscriptionsByTopicResponse = await snsClient.ListSubscriptionsByTopicAsync(topicArn);

                var subscriptions = listSubscriptionsByTopicResponse?.Subscriptions;

                var subscription = subscriptions?.FirstOrDefault(s => s.SubscriptionArn == sqsInformation.Arn);

                if (subscription == null)
                {
                    var subscribeResponse = await snsClient.SubscribeAsync(topicArn, "sqs", sqsInformation.Arn);

                    if (subscribeResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new SnsRebusException($"Error creating subscription {subscriberAddress} on topic {topic}.", subscribeResponse.CreateAmazonExceptionFromResponse());
                    }

                    await m_AmazonInternalSettings.CheckSqsPolicy(rebusTransactionScope.TransactionContext, destinationQueueUrlByName, sqsInformation, topicArn);

                    await snsClient.SetSubscriptionAttributesAsync(subscribeResponse.SubscriptionArn, "RawMessageDelivery", bool.TrueString);
                }
                else
                {
                    await snsClient.SetSubscriptionAttributesAsync(subscription.SubscriptionArn, "RawMessageDelivery", bool.TrueString);
                }
            }
            m_log.Debug("Added sqs subscriber {0} to sns topic {1}", subscriberAddress, topic);
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            m_log.Debug(c_removingSqsSubscriptionMessage, subscriberAddress, topic);
            using (var rebusTransactionScope = new RebusTransactionScope())
            {
                var topicArn = await m_AmazonInternalSettings.GetTopicArn(topic, rebusTransactionScope);

                using (var scope = new RebusTransactionScope())
                {
                    var destinationQueueUrlByName = m_amazonSQSQueueContext.GetDestinationQueueUrlByName(subscriberAddress, scope.TransactionContext);

                    var sqsInfo = m_amazonSQSQueueContext.GetSqsInformationFromUri(destinationQueueUrlByName);

                    var snsClient = m_AmazonInternalSettings.CreateSnsClient(rebusTransactionScope.TransactionContext);

                    var listSubscriptionsByTopicResponse = await snsClient.ListSubscriptionsByTopicAsync(topicArn);

                    var subscriptions = listSubscriptionsByTopicResponse?.Subscriptions;

                    var subscription = subscriptions.FirstOrDefault(s => s.SubscriptionArn == sqsInfo.Arn);

                    if (subscription != null)
                    {
                        var unsubscribeResponse = await snsClient.UnsubscribeAsync(subscription.SubscriptionArn);

                        if (unsubscribeResponse.HttpStatusCode != HttpStatusCode.OK)
                        {
                            throw new SnsRebusException($"Error deleting subscription {subscriberAddress} on topic {topic}.", unsubscribeResponse.CreateAmazonExceptionFromResponse());
                        }
                    }
                }
            }
            m_log.Debug("Removed sqs subscriber {0} to sns topic {1}", subscriberAddress, topic);
        }

        public void Purge()
        {
            if (Address == null)
            {
                return;
            }

            var queueUri = m_amazonSQSQueueContext.GetInputQueueUrl(Address);
            m_amazonSqsQueuePurgeUtility.Purge(queueUri);
        }

        /// <summary>
        ///     Public initialization method that allows for configuring the peek lock duration. Mostly useful for tests.
        /// </summary>
        public void Initialize(TimeSpan peeklockDuration)
        {
            m_AmazonInternalSettings.AmazonPeekLockDuration.PeekLockDuration = peeklockDuration;

            Initialize();
        }

        /// <summary>
        ///     Deletes the transport's input queue
        /// </summary>
        public void DeleteQueue()
        {
            using (var client = new AmazonSQSClient(m_AmazonInternalSettings.AmazonCredentialsFactory.Create(), m_AmazonInternalSettings.AmazonSqsConfig))
            {
                var queueUri = m_amazonSQSQueueContext.GetInputQueueUrl(Address);
                AsyncHelpers.RunSync(() => client.DeleteQueueAsync(queueUri));
            }
        }
    }
}
