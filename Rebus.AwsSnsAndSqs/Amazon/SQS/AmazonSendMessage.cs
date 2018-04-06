using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.AwsSnsAndSqs.Internals;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.Amazon.SQS
{
    internal class AmazonSendMessage
    {
        private readonly AmazonInternalSettings m_AmazonInternalSettings;
        private readonly AmazonSQSQueueContext m_amazonSQSQueueContext;
       
        public AmazonSendMessage(AmazonInternalSettings m_AmazonInternalSettings, AmazonSQSQueueContext m_amazonSQSQueueContext)
        {
            this.m_AmazonInternalSettings = m_AmazonInternalSettings;
            this.m_amazonSQSQueueContext = m_amazonSQSQueueContext;
        }


        /// <inheritdoc />
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var outgoingMessages = context.GetOrAdd(AmazonConstaints.OutgoingMessagesItemsKey, () =>
            {
                var sendMessageBatchRequestEntries = new ConcurrentQueue<AmazonOutgoingMessage>();

                context.OnCommitted(() => SendOutgoingMessages(sendMessageBatchRequestEntries, context));

                return sendMessageBatchRequestEntries;
            });

            outgoingMessages.Enqueue(new AmazonOutgoingMessage(destinationAddress, message));
        }

        private async Task SendOutgoingMessages(ConcurrentQueue<AmazonOutgoingMessage> outgoingMessages, ITransactionContext context)
        {
            if (!outgoingMessages.Any()) return;

            var client = m_amazonSQSQueueContext.GetClientFromTransactionContext(context);

            var messagesByDestination = outgoingMessages
                .GroupBy(m => m.DestinationAddress)
                .ToList();

            await Task.WhenAll(
                messagesByDestination
                    .Select(async batch =>
                    {
                        var entries = batch
                            .Select(message =>
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
                            })
                            .ToList();

                        var destinationUrl = m_amazonSQSQueueContext.GetDestinationQueueUrlByName(batch.Key, context);

                        foreach (var batchToSend in entries.Batch(10))
                        {
                            var request = new SendMessageBatchRequest(destinationUrl, batchToSend);
                            var response = await client.SendMessageBatchAsync(request);

                            if (response.Failed.Any())
                            {
                                var failed = response.Failed.Select(f => new AmazonSQSException($"Failed {f.Message} with Id={f.Id}, Code={f.Code}, SenderFault={f.SenderFault}"));

                                throw new AggregateException(failed);
                            }
                        }
                    })
            );
        }

        private int? GetDelaySeconds(IReadOnlyDictionary<string, string> headers)
        {
            if (!m_AmazonInternalSettings.AmazonSQSTransportOptions.UseNativeDeferredMessages) return null;

            if (!headers.TryGetValue(Headers.DeferredUntil, out var deferUntilTime)) return null;

            var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();

            var delay = (int)Math.Ceiling((deferUntilDateTimeOffset - RebusTime.Now).TotalSeconds);

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