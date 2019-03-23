namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using System.Text;
    using Messages;
    using Newtonsoft.Json.Linq;

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