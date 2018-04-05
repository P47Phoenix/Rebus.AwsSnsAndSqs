using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;
using Message = Amazon.SQS.Model.Message;
#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.Amazon.SQS
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Amazon Simple Queue Service to move messages around
    /// </summary>
    public class AmazonSQSTransport : ITransport, IInitializable
    {
        readonly AmazonTransportMessageSerializer _serializer = new AmazonTransportMessageSerializer();
        readonly AWSCredentials _credentials;
        readonly AmazonSQSConfig _amazonSqsConfig;
        readonly AmazonSQSTransportOptions _options;
        private readonly AmazonSQSQueueContext m_amazonSQSQueueContext;
        readonly ILog _log;


        private readonly AmazonSendMessage m_amazonSendMessage;
        private readonly AmazonCreateSQSQueue m_amazonCreateSqsQueue;
        private readonly AmazonSQSQueuePurgeUtility m_amazonSqsQueuePurgeUtility;

        AmazonPeekLockDuration m_amazonPeekLockDuration = new AmazonPeekLockDuration();
        private readonly AmazonSQSRecieve m_amazonSqsRecieve;

        /// <summary>
        /// Constructs the transport with the specified settings
        /// </summary>
        public AmazonSQSTransport(string inputQueueAddress, AWSCredentials credentials, AmazonSQSConfig amazonSqsConfig, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, AmazonSQSTransportOptions options = null)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _log = rebusLoggerFactory.GetLogger<AmazonSQSTransport>();

            if (inputQueueAddress != null)
            {
                if (inputQueueAddress.Contains("/") && !Uri.IsWellFormedUriString(inputQueueAddress, UriKind.Absolute))
                {
                    var message = $"The input queue address '{inputQueueAddress}' is not valid - please either use a simple queue name (eg. 'my-queue') or a full URL for the queue endpoint (e.g. 'https://sqs.eu-central-1.amazonaws.com/234234234234234/somqueue').";

                    throw new ArgumentException(message, nameof(inputQueueAddress));
                }
            }

            Address = inputQueueAddress;

            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _amazonSqsConfig = amazonSqsConfig ?? throw new ArgumentNullException(nameof(amazonSqsConfig));
            _options = options ?? new AmazonSQSTransportOptions();

            m_amazonSQSQueueContext = new AmazonSQSQueueContext(_options, _credentials, _amazonSqsConfig);
            m_amazonSendMessage = new AmazonSendMessage(_options, _serializer, m_amazonSQSQueueContext);
            m_amazonCreateSqsQueue = new AmazonCreateSQSQueue(_options, _credentials, m_amazonPeekLockDuration, _log, _amazonSqsConfig);
            m_amazonSqsQueuePurgeUtility = new AmazonSQSQueuePurgeUtility(_credentials, _amazonSqsConfig, _log);
            m_amazonSqsRecieve = new AmazonSQSRecieve(m_amazonSQSQueueContext, _log, _options, asyncTaskFactory, m_amazonPeekLockDuration, _serializer);
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
            m_amazonPeekLockDuration.PeekLockDuration = peeklockDuration;

            Initialize();
        }

        /// <summary>
        /// Initializes the transport by creating the input queue
        /// </summary>
        public void Initialize()
        {
            if (Address == null) return;
            if (_options.CreateQueues) CreateQueue(Address);
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
        public string Address { get; }

        /// <summary>
        /// Deletes the transport's input queue
        /// </summary>
        public void DeleteQueue()
        {
            using (var client = new AmazonSQSClient(_credentials, _amazonSqsConfig))
            {
                var queueUri = m_amazonSQSQueueContext.GetInputQueueUrl(Address);
                AmazonAsyncHelpers.RunSync(() => client.DeleteQueueAsync(queueUri));
            }
        }
    }
}
