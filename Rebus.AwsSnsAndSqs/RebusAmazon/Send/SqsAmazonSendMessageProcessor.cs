namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using Extensions;
    using Internals;
    using Messages;
    using Rebus.Extensions;
    using Time;
    using Transport;

    internal class SqsAmazonSendMessageProcessor : IAmazonSendMessageProcessor
    {
        private readonly string _destinationAddress;
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly AmazonSQSQueueContext _amazonSqsQueueContext;
        private readonly IRebusTime _rebusTime;

        public SqsAmazonSendMessageProcessor(string destinationAddress, IAmazonInternalSettings amazonInternalSettings, AmazonSQSQueueContext amazonSqsQueueContext, IRebusTime rebusTime)
        {
            this._destinationAddress = destinationAddress;
            this._amazonInternalSettings = amazonInternalSettings;
            this._amazonSqsQueueContext = amazonSqsQueueContext;
            this._rebusTime = rebusTime;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task SendAsync(TransportMessage message, ITransactionContext context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var outgoingMessages = context.GetOrAdd(AmazonConstaints.OutgoingMessagesItemsKey, () =>
            {
                var sendMessageBatchRequestEntries = new ConcurrentQueue<AmazonOutgoingMessage>();

                context.OnCommitted((ITransactionContext ctx) => SendOutgoingMessages(sendMessageBatchRequestEntries, context));

                return sendMessageBatchRequestEntries;
            });

            outgoingMessages.Enqueue(new AmazonOutgoingMessage(_destinationAddress, message));
        }

        private async Task SendOutgoingMessages(ConcurrentQueue<AmazonOutgoingMessage> outgoingMessages, ITransactionContext context)
        {
            if (!outgoingMessages.Any())
            {
                return;
            }

            var client = _amazonInternalSettings.CreateSqsClient(context);

            var messagesByDestination = outgoingMessages.GroupBy(m => m.DestinationAddress).ToList();

            await Task.WhenAll(messagesByDestination.Select(async batch =>
            {
                var entries = batch.Select(message =>
                {
                    var transportMessage = message.TransportMessage;
                    var headers = transportMessage.Headers;
                    var messageId = headers[Headers.MessageId];

                    var sqsMessage = new AmazonTransportMessage(transportMessage.Headers, StringHelper.GetBody(transportMessage.Body));

                    var entry = new SendMessageBatchRequestEntry(messageId, _amazonInternalSettings.MessageSerializer.Serialize(sqsMessage));

                    var delaySeconds = GetDelaySeconds(headers);

                    if (delaySeconds != null)
                    {
                        entry.DelaySeconds = delaySeconds.Value;
                    }

                    return entry;
                }).ToList();

                var destinationUrl = _amazonSqsQueueContext.GetDestinationQueueUrlByName(batch.Key, context);

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
            if (_amazonInternalSettings.AmazonSnsAndSqsTransportOptions.UseNativeDeferredMessages == false)
            {
                return null;
            }

            if (headers.TryGetValue(Headers.DeferredUntil, out var deferUntilTime) == false)
            {
                return null;
            }

            var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();

            var delay = (int)Math.Ceiling((deferUntilDateTimeOffset - _rebusTime.Now).TotalSeconds);

            // SQS will only accept delays between 0 and 900 seconds.
            // In the event that the value for deferreduntil is before the current date, the message should be processed immediately. i.e. with a delay of 0 seconds.
            return Math.Max(delay, 0);
        }
    }
}