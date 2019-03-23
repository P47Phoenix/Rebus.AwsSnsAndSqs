namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using System.Text;
    using Amazon.SQS.Model;
    using Messages;
    using Newtonsoft.Json.Linq;
    using Message = Amazon.SQS.Model.Message;

    public interface IAmazonMessageProcessorFactory
    {
        IAmazonMessageProcessor Create(Message message);
    }

    internal class AmazonMessageProcessorFactory : IAmazonMessageProcessorFactory
    {
        private readonly IAmazonInternalSettings _amazonInternalSettings;

        public AmazonMessageProcessorFactory(IAmazonInternalSettings amazonInternalSettings)
        {
            _amazonInternalSettings = amazonInternalSettings;
        }

        public IAmazonMessageProcessor Create(Message message)
        {
            if (message.Body.StartsWith("{"))
            {
                var messageJObject = JObject.Parse(message.Body);

                var isFromSnsTopic = messageJObject["Type"]?.Value<string>() == "Notification";

                if (isFromSnsTopic)
                {
                    return new SnsTopicAmazonMessageProcessor(messageJObject, _amazonInternalSettings);
                }
                else
                {
                    return new RebusMessageAmazonMessageProcessor(message, _amazonInternalSettings);
                }
            }


        }
    }

    internal class RebusMessageAmazonMessageProcessor : IAmazonMessageProcessor
    {
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly Message _message;

        public RebusMessageAmazonMessageProcessor(Message message, IAmazonInternalSettings amazonInternalSettings)
        {
            _amazonInternalSettings = amazonInternalSettings;
            _message = message;
        }
        public TransportMessage ProcessMessage()
        {
            var sqsMessage = _amazonInternalSettings.MessageSerializer.Deserialize(_message.Body);
            return new TransportMessage(sqsMessage.Headers, body: sqsMessage.Body.GetBodyBytes());
        }
    }

    internal class SnsTopicAmazonMessageProcessor : IAmazonMessageProcessor
    {
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly JObject _message;

        public SnsTopicAmazonMessageProcessor(JObject message, IAmazonInternalSettings amazonInternalSettings)
        {
            _amazonInternalSettings = amazonInternalSettings;
            _message = message;
        }
        public TransportMessage ProcessMessage()
        {
            var snsMessage = _message["Message"].Value<string>();

            var msgBytes = Convert.FromBase64String(snsMessage);

            var msg = Encoding.UTF8.GetString(msgBytes);

            var sqsMessage = _amazonInternalSettings.MessageSerializer.Deserialize(msg);

            return new TransportMessage(sqsMessage.Headers, body: sqsMessage.Body.GetBodyBytes());
        }
    }
}