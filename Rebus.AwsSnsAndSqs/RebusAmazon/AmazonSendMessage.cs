using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AwsSnsAndSqs.Internals;
using Rebus.AwsSnsAndSqs.RebusAmazon.Extensions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonSendMessage : ISendMessage
    {
        private const string c_SnsArn = "arn:aws:sns:";

        private readonly IAmazonInternalSettings m_AmazonInternalSettings;
        private readonly AmazonSQSQueueContext m_amazonSQSQueueContext;

        public AmazonSendMessage(IAmazonInternalSettings m_AmazonInternalSettings, AmazonSQSQueueContext m_amazonSQSQueueContext)
        {
            this.m_AmazonInternalSettings = m_AmazonInternalSettings ?? throw new ArgumentNullException(nameof(m_AmazonInternalSettings));
            this.m_amazonSQSQueueContext = m_amazonSQSQueueContext ?? throw new ArgumentNullException(nameof(m_amazonSQSQueueContext));
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

            if (destinationAddress.StartsWith(c_SnsArn))
            {
                var snsClient = m_AmazonInternalSettings.CreateSnsClient(context);

                var sqsMessage = new AmazonTransportMessage(message.Headers, GetBody(message.Body));

                var msg = m_AmazonInternalSettings.MessageSerializer.Serialize(sqsMessage);

                var msgBytes = Encoding.UTF8.GetBytes(msg);

                var publishResponse = await snsClient.PublishAsync(new PublishRequest(destinationAddress, GetBody(msgBytes)));

                if (publishResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new SnsRebusExption($"Error publishing message to topic {destinationAddress}.", publishResponse.CreateAmazonExceptionFromResponse());
                }
            }
            else
            {
                var outgoingMessages = context.GetOrAdd(AmazonConstaints.OutgoingMessagesItemsKey, () =>
                {
                    var sendMessageBatchRequestEntries = new ConcurrentQueue<AmazonOutgoingMessage>();

                    context.OnCommitted(() => SendOutgoingMessages(sendMessageBatchRequestEntries, context));

                    return sendMessageBatchRequestEntries;
                });

                outgoingMessages.Enqueue(new AmazonOutgoingMessage(destinationAddress, message));
            }
        }

        private async Task SendOutgoingMessages(ConcurrentQueue<AmazonOutgoingMessage> outgoingMessages, ITransactionContext context)
        {
            if (!outgoingMessages.Any())
            {
                return;
            }

            var client = m_AmazonInternalSettings.CreateSqsClient(context);

            var messagesByDestination = outgoingMessages.GroupBy(m => m.DestinationAddress).ToList();

            await Task.WhenAll(messagesByDestination.Select(async batch =>
            {
                var entries = batch.Select(message =>
                {
                    var transportMessage = message.TransportMessage;
                    var headers = transportMessage.Headers;
                    var messageId = headers[Headers.MessageId];

                    var sqsMessage = new AmazonTransportMessage(transportMessage.Headers, GetBody(transportMessage.Body));

                    var entry = new SendMessageBatchRequestEntry(messageId, m_AmazonInternalSettings.MessageSerializer.Serialize(sqsMessage));

                    var delaySeconds = GetDelaySeconds(headers);

                    if (delaySeconds != null)
                    {
                        entry.DelaySeconds = delaySeconds.Value;
                    }

                    return entry;
                }).ToList();

                var destinationUrl = m_amazonSQSQueueContext.GetDestinationQueueUrlByName(batch.Key, context);

                foreach (var batchToSend in entries.Batch(10))
                {
                    var request = new SendMessageBatchRequest(destinationUrl, batchToSend);
                    var response = await client.SendMessageBatchAsync(request);

                    if (response.Failed.Count == 0)
                    {
                        continue;
                    }

                    var failed = response.Failed.Select(f => new AmazonSQSException($"Failed {f.Message} with Id={f.Id}, Code={f.Code}, SenderFault={f.SenderFault}"));

                    throw new AggregateException(failed);
                }
            }));
        }

        private int? GetDelaySeconds(IReadOnlyDictionary<string, string> headers)
        {
            if (m_AmazonInternalSettings.AmazonSnsAndSqsTransportOptions.UseNativeDeferredMessages == false)
            {
                return null;
            }

            if (headers.TryGetValue(Headers.DeferredUntil, out var deferUntilTime) == false)
            {
                return null;
            }

            var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();

            var delay = (int) Math.Ceiling((deferUntilDateTimeOffset - RebusTime.Now).TotalSeconds);

            // SQS will only accept delays between 0 and 900 seconds.
            // In the event that the value for deferreduntil is before the current date, the message should be processed immediately. i.e. with a delay of 0 seconds.
            return Math.Max(delay, 0);
        }


        private static string GetBody(byte[] bodyBytes)
        {
            return Convert.ToBase64String(bodyBytes);
        }
    }
}
