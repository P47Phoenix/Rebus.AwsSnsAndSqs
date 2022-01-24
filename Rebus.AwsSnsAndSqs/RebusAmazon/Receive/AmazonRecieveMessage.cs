namespace Rebus.AwsSnsAndSqs.RebusAmazon.Receive
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using Extensions;
    using Logging;
    using Messages;
    using Rebus.Time;
    using Threading;
    using Transport;
    using Message = Amazon.SQS.Model.Message;

    internal class AmazonRecieveMessage
    {
        private readonly IAmazonInternalSettings m_amazonInternalSettings;
        private readonly IAmazonMessageProcessorFactory _amazonMessageProcessorFactory;
        private readonly AmazonSQSQueueContext m_amazonSqsQueueContext;
        private readonly ILog m_log;
        private readonly IRebusTime _rebusTime;

        public AmazonRecieveMessage(IAmazonInternalSettings amazonInternalSettings, AmazonSQSQueueContext amazonSQSQueueContext, IAmazonMessageProcessorFactory amazonMessageProcessorFactory, IRebusTime rebusTime)
        {
            _amazonMessageProcessorFactory = amazonMessageProcessorFactory;
            m_amazonInternalSettings = amazonInternalSettings ?? throw new ArgumentNullException(nameof(amazonInternalSettings));
            m_amazonSqsQueueContext = amazonSQSQueueContext ?? throw new ArgumentNullException(nameof(amazonSQSQueueContext));
            m_log = m_amazonInternalSettings.RebusLoggerFactory.GetLogger<AmazonRecieveMessage>();
            _rebusTime = rebusTime;
        }

        /// <inheritdoc />
        public async Task<TransportMessage> Receive(ITransactionContext context, string address, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (address == null)
            {
                throw new InvalidOperationException("This Amazon SQS transport does not have an input queue, hence it is not possible to receive anything");
            }

            var queueUrl = m_amazonSqsQueueContext.GetDestinationQueueUrlByName(address, context);

            if (string.IsNullOrWhiteSpace(queueUrl))
            {
                throw new InvalidOperationException("The queue URL is empty - has the transport not been initialized?");
            }

            var client = m_amazonInternalSettings.CreateSqsClient(context);

            var request = new ReceiveMessageRequest(queueUrl)
            {
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = m_amazonInternalSettings.AmazonSnsAndSqsTransportOptions.ReceiveWaitTimeSeconds,
                AttributeNames = new List<string>(new[] { "All" }),
                MessageAttributeNames = new List<string>(new[] { "All" })
            };

            var response = await client.ReceiveMessageAsync(request, cancellationToken);

            if (response.Messages.Any() == false)
            {
                return null;
            }

            var sqsMessage = response.Messages.First();

            var renewalTask = CreateRenewalTaskForMessage(sqsMessage, queueUrl, client);

            context.OnCompleted((ITransactionContext ctx) =>
            {
                renewalTask.Dispose();
                // if we get this far, we don't want to pass on the cancellation token
                // ReSharper disable once MethodSupportsCancellation
                return client.DeleteMessageAsync(new DeleteMessageRequest(queueUrl, sqsMessage.ReceiptHandle));
            });

            context.OnAborted((ITransactionContext ctx) =>
            {
                renewalTask.Dispose();
                Task.Run(() => client.ChangeMessageVisibilityAsync(queueUrl, sqsMessage.ReceiptHandle, 0, cancellationToken), cancellationToken).Wait(cancellationToken);
            });

            IAmazonMessageProcessor amazonMessageProcessor = _amazonMessageProcessorFactory.Create(sqsMessage);

            var transportMessage = amazonMessageProcessor.ProcessMessage();

            if (transportMessage.MessageIsExpired(_rebusTime, sqsMessage))
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
            return m_amazonInternalSettings.AsyncTaskFactory.Create($"RenewPeekLock-{message.MessageId}", async () =>
            {
                m_log.Info("Renewing peek lock for message with ID {messageId}", message.MessageId);

                var request = new ChangeMessageVisibilityRequest(queueUrl, message.ReceiptHandle, (int)m_amazonInternalSettings.AmazonPeekLockDuration.PeekLockDuration.TotalSeconds);

                await client.ChangeMessageVisibilityAsync(request);
            }, intervalSeconds: (int)m_amazonInternalSettings.AmazonPeekLockDuration.PeekLockRenewalInterval.TotalSeconds, prettyInsignificant: true);
        }
    }
}
