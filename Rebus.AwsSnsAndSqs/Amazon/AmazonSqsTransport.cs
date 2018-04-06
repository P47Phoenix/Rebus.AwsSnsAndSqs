using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.Amazon
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Amazon Simple Queue Service to move messages around
    /// </summary>
    public class AmazonSQSTransport : ITransport, IInitializable
    {
        private readonly ILog m_log;        
        private readonly AmazonSQSQueueContext m_amazonSQSQueueContext;
        private readonly AmazonSendMessage m_amazonSendMessage;
        private readonly AmazonCreateSQSQueue m_amazonCreateSqsQueue;
        private readonly AmazonSQSQueuePurgeUtility m_amazonSqsQueuePurgeUtility;        
        private readonly AmazonSQSRecieve m_amazonSqsRecieve;
        private readonly AmazonInternalSettings m_AmazonInternalSettings;

        /// <summary>
        /// Constructs the transport with the specified settings
        /// </summary>
        public AmazonSQSTransport(string inputQueueAddress, AWSCredentials credentials, AmazonSQSConfig amazonSqsConfig, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, AmazonSQSTransportOptions options = null)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            m_log = rebusLoggerFactory.GetLogger<AmazonSQSTransport>();

            if (inputQueueAddress != null)
            {
                if (inputQueueAddress.Contains("/") && !Uri.IsWellFormedUriString(inputQueueAddress, UriKind.Absolute))
                {
                    var message = $"The input queue address '{inputQueueAddress}' is not valid - please either use a simple queue name (eg. 'my-queue') or a full URL for the queue endpoint (e.g. 'https://sqs.eu-central-1.amazonaws.com/234234234234234/somqueue').";

                    throw new ArgumentException(message, nameof(inputQueueAddress));
                }
            }

            m_AmazonInternalSettings = new AmazonInternalSettings
            {
                Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials)),
                InputQueueAddress = inputQueueAddress,
                AmazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig)),
                AmazonSQSTransportOptions = options ?? new AmazonSQSTransportOptions(),
                RebusLoggerFactory = rebusLoggerFactory,
                AmazonPeekLockDuration = new AmazonPeekLockDuration(),
                MessageSerializer = new AmazonTransportMessageSerializer(),
                AsyncTaskFactory = asyncTaskFactory
            };
            m_amazonSQSQueueContext = new AmazonSQSQueueContext(m_AmazonInternalSettings);
            m_amazonSendMessage = new AmazonSendMessage(m_AmazonInternalSettings, m_amazonSQSQueueContext);
            m_amazonCreateSqsQueue = new AmazonCreateSQSQueue(m_AmazonInternalSettings);
            m_amazonSqsQueuePurgeUtility = new AmazonSQSQueuePurgeUtility(m_AmazonInternalSettings);
            m_amazonSqsRecieve = new AmazonSQSRecieve(m_AmazonInternalSettings, m_amazonSQSQueueContext);
        }

        public void Purge()
        {
            if (Address == null) return;
            var queueUri = m_amazonSQSQueueContext.GetInputQueueUrl(Address);
            m_amazonSqsQueuePurgeUtility.Purge(queueUri);
        }

        /// <summary>
        /// Public initialization method that allows for configuring the peek lock duration. Mostly useful for tests.
        /// </summary>
        public void Initialize(TimeSpan peeklockDuration)
        {
            m_AmazonInternalSettings.AmazonPeekLockDuration.PeekLockDuration = peeklockDuration;

            Initialize();
        }

        /// <summary>
        /// Initializes the transport by creating the input queue
        /// </summary>
        public void Initialize()
        {
            if (Address == null) return;

            if (m_AmazonInternalSettings.AmazonSQSTransportOptions.CreateQueues)
            {
                CreateQueue(Address);
            }
        }
        
        /// <summary>
        /// Creates the queue with the given name
        /// </summary>
        public void CreateQueue(string address)
        {
            m_amazonCreateSqsQueue.CreateQueue(address);
        }

        /// <inheritdoc />
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            await m_amazonSendMessage.Send(destinationAddress, message, context);
        }

        /// <inheritdoc />
        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return await m_amazonSqsRecieve.Receive(context, Address, cancellationToken);
        }

        /// <summary>
        /// Gets the input queue name
        /// </summary>
        public string Address => m_AmazonInternalSettings.InputQueueAddress;

        /// <summary>
        /// Deletes the transport's input queue
        /// </summary>
        public void DeleteQueue()
        {
            using (var client = new AmazonSQSClient(m_AmazonInternalSettings.Credentials, m_AmazonInternalSettings.AmazonSqsConfig))
            {
                var queueUri = m_amazonSQSQueueContext.GetInputQueueUrl(Address);
                AmazonAsyncHelpers.RunSync(() => client.DeleteQueueAsync(queueUri));
            }
        }
    }
}
