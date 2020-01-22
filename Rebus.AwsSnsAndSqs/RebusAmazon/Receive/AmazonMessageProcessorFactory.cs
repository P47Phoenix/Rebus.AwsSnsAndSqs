namespace Rebus.AwsSnsAndSqs.RebusAmazon.Receive
{
    using System.Globalization;
    using Newtonsoft.Json.Linq;
    using Message = Amazon.SQS.Model.Message;

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
            if (message.Body.StartsWith("{", false, CultureInfo.InvariantCulture))
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
}