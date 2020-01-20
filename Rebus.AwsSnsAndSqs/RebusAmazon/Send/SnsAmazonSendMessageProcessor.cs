namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Amazon.SimpleNotificationService.Model;
    using Extensions;
    using Messages;
    using Transport;

    internal class SnsAmazonSendMessageProcessor : IAmazonSendMessageProcessor
    {
        private string _destinationAddress;
        private IAmazonInternalSettings _amazonInternalSettings;

        public SnsAmazonSendMessageProcessor(string destinationAddress, IAmazonInternalSettings amazonInternalSettings)
        {
            this._destinationAddress = destinationAddress;
            this._amazonInternalSettings = amazonInternalSettings;
        }

        public async Task SendAsync(TransportMessage message, ITransactionContext context)
        {
            var snsClient = _amazonInternalSettings.CreateSnsClient(context);

            var sqsMessage = new AmazonTransportMessage(message.Headers, StringHelper.GetBody(message.Body));

            var msg = _amazonInternalSettings.MessageSerializer.Serialize(sqsMessage);

            var pubRequest = new PublishRequest(_destinationAddress, msg);

            var messageAttributeValues = context.GetOrNull<IDictionary<string, MessageAttributeValue>>(SnsAttributeMapperOutBoundStep.SnsAttributeKey) ?? new Dictionary<string, MessageAttributeValue>();
            
            foreach (var messageAttributeValue in messageAttributeValues)
            {
                pubRequest.MessageAttributes.Add(messageAttributeValue.Key, messageAttributeValue.Value);
            }
            var publishResponse = await snsClient.PublishAsync(pubRequest);

            if (publishResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new SnsRebusException($"Error publishing message to topic {_destinationAddress}.", publishResponse.CreateAmazonExceptionFromResponse());
            }
        }
    }
}