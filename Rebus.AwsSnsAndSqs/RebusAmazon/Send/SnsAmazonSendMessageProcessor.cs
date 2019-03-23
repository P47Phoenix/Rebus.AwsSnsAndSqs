using Amazon.SimpleNotificationService.Model;
using Rebus.AwsSnsAndSqs.RebusAmazon.Extensions;
using Rebus.Messages;
using Rebus.Transport;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{

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

            var msgBytes = Encoding.UTF8.GetBytes(msg);

            // promote certain headers to a messageAttribute, we can then filter on those with policy
            var pubRequest = new PublishRequest(_destinationAddress, StringHelper.GetBody(msgBytes));
            foreach (var kvp in sqsMessage.Headers.Where(h => !string.IsNullOrEmpty(h.Value)))
                pubRequest.MessageAttributes[kvp.Key] =
                    new Amazon.SimpleNotificationService.Model.MessageAttributeValue { StringValue = kvp.Value, DataType = "String" };

            var publishResponse = await snsClient.PublishAsync(pubRequest);

            if (publishResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new SnsRebusExption($"Error publishing message to topic {_destinationAddress}.", publishResponse.CreateAmazonExceptionFromResponse());
            }
        }
    }
}