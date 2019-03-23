namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Amazon.SQS.Model;
    using Messages;
    using Newtonsoft.Json.Linq;
    using Message = Amazon.SQS.Model.Message;

    /// <summary></summary>
    public interface IAmazonMessageProcessorFactory
    {
        /// <summary>Creates the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        IAmazonMessageProcessor Create(Message message);
    }

    /// <summary></summary>
    internal class AmazonMessageProcessorFactory : IAmazonMessageProcessorFactory
    {
        private readonly IAmazonInternalSettings _amazonInternalSettings;

        /// <summary></summary>
        /// <param name="amazonInternalSettings"></param>
        public AmazonMessageProcessorFactory(IAmazonInternalSettings amazonInternalSettings)
        {
            _amazonInternalSettings = amazonInternalSettings;
        }

        /// <summary>Creates the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public IAmazonMessageProcessor Create(Message message)
        {
            if (message.Body.StartsWith("{"))
            {
                
                var messageJObject = JObject.Parse(message.Body);

                var isFromSnsTopic = messageJObject["Type"]?.Value<string>() == Amazon.SimpleNotificationService.Util.Message.MESSAGE_TYPE_NOTIFICATION;

                if (isFromSnsTopic)
                {
                    return new SnsTopicAmazonMessageProcessor(messageJObject, _amazonInternalSettings);
                }
                else
                {
                    return new RebusMessageAmazonMessageProcessor(message, _amazonInternalSettings);
                }
            }
            
            return new RebusMessageAmazonMessageProcessor(message, _amazonInternalSettings);
        }
    }

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

    internal class SnsTopicAmazonMessageProcessor : IAmazonMessageProcessor
    {
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly JObject _message;
        private string c_SnsArn;
        private IAmazonInternalSettings m_AmazonInternalSettings;
        private AmazonSQSQueueContext m_amazonSQSQueueContext;

        /// <summary>Initializes a new instance of the <see cref="SnsTopicAmazonMessageProcessor"/> class.</summary>
        /// <param name="message">The message.</param>
        /// <param name="amazonInternalSettings">The amazon internal settings.</param>
        public SnsTopicAmazonMessageProcessor(JObject message, IAmazonInternalSettings amazonInternalSettings)
        {
            _amazonInternalSettings = amazonInternalSettings;
            _message = message;
        }

        /// <summary>Processes the message.</summary>
        /// <returns></returns>
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