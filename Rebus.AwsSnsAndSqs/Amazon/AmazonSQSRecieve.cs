using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AwsSnsAndSqs.Amazon.Extensions;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;
using Message = Amazon.SQS.Model.Message;

namespace Rebus.AwsSnsAndSqs.Amazon.SQS
{
    internal class AmazonSQSRecieve
    {
        private AmazonSQSQueueContext m_amazonSqsQueueContext;
        private ILog _log;
        private AmazonSQSTransportOptions _options;
        private IAsyncTaskFactory _asyncTaskFactory;
        private AmazonPeekLockDuration m_amazonPeekLockDuration;
        private AmazonTransportMessageSerializer _serializer;

        public AmazonSQSRecieve(
            AmazonSQSQueueContext amazonSqsQueueContext,
            ILog log,
            AmazonSQSTransportOptions options,
            IAsyncTaskFactory asyncTaskFactory,
            AmazonPeekLockDuration amazonPeekLockDuration,
            AmazonTransportMessageSerializer serializer)
        {
            this.m_amazonSqsQueueContext = amazonSqsQueueContext;
            _log = log;
            _options = options;
            _asyncTaskFactory = asyncTaskFactory;
            m_amazonPeekLockDuration = amazonPeekLockDuration;
            _serializer = serializer;
        }

        /// <inheritdoc />
        public async Task<TransportMessage> Receive(ITransactionContext context, string address, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (address == null)
            {
                throw new InvalidOperationException("This Amazon SQS transport does not have an input queue, hence it is not possible to reveive anything");
            }

            var queueUrl = m_amazonSqsQueueContext.GetDestinationQueueUrlByName(address, context);

            if (string.IsNullOrWhiteSpace(queueUrl))
            {
                throw new InvalidOperationException("The queue URL is empty - has the transport not been initialized?");
            }

            var client = m_amazonSqsQueueContext.GetClientFromTransactionContext(context);

            var request = new ReceiveMessageRequest(queueUrl)
            {
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = _options.ReceiveWaitTimeSeconds,
                AttributeNames = new List<string>(new[] { "All" }),
                MessageAttributeNames = new List<string>(new[] { "All" })
            };

            var response = await client.ReceiveMessageAsync(request, cancellationToken);

            if (!response.Messages.Any()) return null;

            var sqsMessage = response.Messages.First();

            var renewalTask = CreateRenewalTaskForMessage(sqsMessage, queueUrl, client);

            context.OnCompleted(async () =>
            {
                renewalTask.Dispose();

                // if we get this far, we don't want to pass on the cancellation token
                // ReSharper disable once MethodSupportsCancellation
                await client.DeleteMessageAsync(new DeleteMessageRequest(queueUrl, sqsMessage.ReceiptHandle));
            });

            context.OnAborted(() =>
            {
                renewalTask.Dispose();
                Task.Run(() => client.ChangeMessageVisibilityAsync(queueUrl, sqsMessage.ReceiptHandle, 0, cancellationToken), cancellationToken).Wait(cancellationToken);
            });

            var transportMessage = ExtractTransportMessageFrom(sqsMessage);
            if (transportMessage.MessageIsExpired(sqsMessage))
            {
                // if the message is expired , we don't want to pass on the cancellation token
                // ReSharper disable once MethodSupportsCancellation
                await client.DeleteMessageAsync(new DeleteMessageRequest(queueUrl, sqsMessage.ReceiptHandle));
                return null;
            }
            renewalTask.Start();
            return transportMessage;
        }

        private IAsyncTask CreateRenewalTaskForMessage(Message message, string queueUrl, IAmazonSQS client)
        {
            return _asyncTaskFactory.Create($"RenewPeekLock-{message.MessageId}",
                async () =>
                {
                    _log.Info("Renewing peek lock for message with ID {messageId}", message.MessageId);

                    var request = new ChangeMessageVisibilityRequest(queueUrl, message.ReceiptHandle, (int)m_amazonPeekLockDuration.PeekLockDuration.TotalSeconds);

                    await client.ChangeMessageVisibilityAsync(request);
                },
                intervalSeconds: (int)m_amazonPeekLockDuration.PeekLockRenewalInterval.TotalSeconds,
                prettyInsignificant: true);
        }

        private TransportMessage ExtractTransportMessageFrom(Message message)
        {
            var sqsMessage = _serializer.Deserialize(message.Body);
            return new TransportMessage(sqsMessage.Headers, GetBodyBytes(sqsMessage.Body));
        }

        private byte[] GetBodyBytes(string bodyText) => Convert.FromBase64String(bodyText);
    }
}