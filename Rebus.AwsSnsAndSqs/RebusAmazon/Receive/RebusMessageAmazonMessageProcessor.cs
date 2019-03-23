namespace Rebus.AwsSnsAndSqs.RebusAmazon.Receive
{
    using AwsSnsAndSqs.Extensions;
    using Messages;
    using Message = Amazon.SQS.Model.Message;

    /// <summary></summary>
    internal class RebusMessageAmazonMessageProcessor : IAmazonMessageProcessor
    {
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly Message _message;

        public RebusMessageAmazonMessageProcessor(Message message, IAmazonInternalSettings amazonInternalSettings)
        {
            _amazonInternalSettings = amazonInternalSettings;
            _message = message;
        }
        /// <summary>Processes the message.</summary>
        /// <returns></returns>
        public TransportMessage ProcessMessage()
        {
            var sqsMessage = _amazonInternalSettings.MessageSerializer.Deserialize(_message.Body);
            return new TransportMessage(sqsMessage.Headers, body: sqsMessage.Body.GetBodyBytes());
        }
    }
}