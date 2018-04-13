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
using Rebus.AwsSnsAndSqs.RebusAmazon.SQS;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    /// <summary>
    ///     Implementation of <see cref="ITransport" /> that uses Amazon Simple Queue Service to move messages around
    /// </summary>
    internal class AmazonSQSTransport : IAmazonSQSTransport
    {
        private readonly AmazonCreateSQSQueue m_amazonCreateSqsQueue;
        private readonly IAmazonInternalSettings m_AmazonInternalSettings;
        private readonly AmazonSQSQueueContext m_amazonSQSQueueContext;
        private readonly AmazonSQSQueuePurgeUtility m_amazonSqsQueuePurgeUtility;
        private readonly AmazonSQSRecieve m_amazonSqsRecieve;
        private readonly ILog m_log;
        private readonly ISendMessage m_sendMessage;

        /// <summary>
        ///     Constructs the transport with the specified settings
        /// </summary>
        public AmazonSQSTransport(IAmazonInternalSettings amazonInternalSettings)
        {
            m_AmazonInternalSettings = amazonInternalSettings ?? throw new ArgumentNullException(nameof(amazonInternalSettings));

            m_log = amazonInternalSettings.RebusLoggerFactory.GetLogger<AmazonSQSTransport>();

            if (amazonInternalSettings.InputQueueAddress != null)
            {
                if (amazonInternalSettings.InputQueueAddress.Contains("/") && !Uri.IsWellFormedUriString(amazonInternalSettings.InputQueueAddress, UriKind.Absolute))
                {
                    var message = $"The input queue address '{amazonInternalSettings.InputQueueAddress}' is not valid - please either use a simple queue name (eg. 'my-queue') or a full URL for the queue endpoint (e.g. 'https://sqs.eu-central-1.amazonaws.com/234234234234234/somqueue').";

                    throw new ArgumentException(message, nameof(amazonInternalSettings.InputQueueAddress));
                }
            }

            m_amazonSQSQueueContext = new AmazonSQSQueueContext(m_AmazonInternalSettings);
            m_sendMessage = new AmazonSendMessage(m_AmazonInternalSettings, m_amazonSQSQueueContext);
            m_amazonCreateSqsQueue = new AmazonCreateSQSQueue(m_AmazonInternalSettings);
            m_amazonSqsQueuePurgeUtility = new AmazonSQSQueuePurgeUtility(m_AmazonInternalSettings);
            m_amazonSqsRecieve = new AmazonSQSRecieve(m_AmazonInternalSettings, m_amazonSQSQueueContext);
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
            await m_sendMessage.Send(destinationAddress, message, context);
        }

        /// <inheritdoc />
        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return await m_amazonSqsRecieve.Receive(context, Address, cancellationToken);
        }

        /// <summary>
        ///     Gets the input queue name
        /// </summary>
        public string Address => m_AmazonInternalSettings.InputQueueAddress;

        public bool IsCentralized => true;

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            var snsClient = m_AmazonInternalSettings.CreateSnsClient();

            var formatedTopicName = m_AmazonInternalSettings.TopicFormatter.FormatTopic(topic);

            var findTopicResult = await snsClient.FindTopicAsync(formatedTopicName);

            if (findTopicResult == null)
            {
                throw new ArgumentOutOfRangeException($"The topic {formatedTopicName} does not exist");
            }

            return new[] {findTopicResult.TopicArn};
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            var snsClient = m_AmazonInternalSettings.CreateSnsClient();

            var topicArn = await GetTopicArn(snsClient, topic);

            using (var scope = new RebusTransactionScope())
            {
                var destinationQueueUrlByName = m_amazonSQSQueueContext.GetDestinationQueueUrlByName(subscriberAddress, scope.TransactionContext);

                var sqsInformation = m_amazonSQSQueueContext.GetSqsInformationFromUri(destinationQueueUrlByName);

                var listSubscriptionsByTopicResponse = await snsClient.ListSubscriptionsByTopicAsync(topicArn);

                var subscriptions = listSubscriptionsByTopicResponse?.Subscriptions;

                if (subscriptions?.Count <= 0 || subscriptions?.Any(s => s.SubscriptionArn == sqsInformation.Arn) == false)
                {
                    var subscribeResponse = await snsClient.SubscribeAsync(topicArn, "sqs", sqsInformation.Arn);

                    if (subscribeResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new SnsRebusExption($"Error creating subscription {subscriberAddress} on topic {topic}.", subscribeResponse.CreateAmazonExceptionFromResponse());
                    }

                    var sqsClient = m_AmazonInternalSettings.CreateSqsClient(scope.TransactionContext);

                    var sqsPolicy = new Policy().WithStatements(new Statement(Statement.StatementEffect.Allow).WithPrincipals(Principal.AllUsers).WithResources(new Resource(sqsInformation.Arn)).WithConditions(ConditionFactory.NewSourceArnCondition(topicArn)).WithActionIdentifiers(SQSActionIdentifiers.SendMessage));

                    var attributes = new Dictionary<string, string> {{"Policy", sqsPolicy.ToJson()}};

                    await sqsClient.SetAttributesAsync(sqsInformation.Url, attributes);
                }
            }
        }


        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            var snsClient = m_AmazonInternalSettings.CreateSnsClient();

            var formatedTopicName = m_AmazonInternalSettings.TopicFormatter.FormatTopic(topic);

            var topicArn = await GetTopicArn(snsClient, formatedTopicName);

            using (var scope = new RebusTransactionScope())
            {
                var destinationQueueUrlByName = m_amazonSQSQueueContext.GetDestinationQueueUrlByName(subscriberAddress, scope.TransactionContext);

                var sqsInfo = m_amazonSQSQueueContext.GetSqsInformationFromUri(destinationQueueUrlByName);

                var listSubscriptionsByTopicResponse = await snsClient.ListSubscriptionsByTopicAsync(topicArn);

                var subscriptions = listSubscriptionsByTopicResponse?.Subscriptions;

                var subscription = subscriptions.FirstOrDefault(s => s.SubscriptionArn == sqsInfo.Arn);

                if (subscription != null)
                {
                    var unsubscribeResponse = await snsClient.UnsubscribeAsync(subscription.SubscriptionArn);

                    if (unsubscribeResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new SnsRebusExption($"Error deleting subscription {subscriberAddress} on topic {topic}.", unsubscribeResponse.CreateAmazonExceptionFromResponse());
                    }
                }
            }
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
                AmazonAsyncHelpers.RunSync(() => client.DeleteQueueAsync(queueUri));
            }
        }

        private async Task<string> GetTopicArn(IAmazonSimpleNotificationService snsClient, string topic)
        {
            var formatedTopicName = m_AmazonInternalSettings.TopicFormatter.FormatTopic(topic);

            var findTopicResult = await snsClient.FindTopicAsync(formatedTopicName);

            var topicArn = findTopicResult?.TopicArn;

            if (topicArn == null)
            {
                var createTopicResponse = await snsClient.CreateTopicAsync(formatedTopicName);

                if (createTopicResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new SnsRebusExption($"Error creating topic {formatedTopicName}.", createTopicResponse.CreateAmazonExceptionFromResponse());
                }

                topicArn = createTopicResponse.TopicArn;
            }

            return topicArn;
        }
    }
}
