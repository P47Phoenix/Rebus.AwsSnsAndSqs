namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;
    using System.Globalization;

    internal class AmazonSendMessageCommandFactory : IAmazonSendMessageCommandFactory
    {
        private const string c_SnsArn = "arn:aws:sns:";
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly AmazonSQSQueueContext _amazonSqsQueueContext;

        public AmazonSendMessageCommandFactory(IAmazonInternalSettings amazonInternalSettings, AmazonSQSQueueContext amazonSqsQueueContext)
        {
            this._amazonInternalSettings = amazonInternalSettings ?? throw new ArgumentNullException(nameof(amazonInternalSettings));
            this._amazonSqsQueueContext = amazonSqsQueueContext ?? throw new ArgumentNullException(nameof(amazonSqsQueueContext));
        }

        public IAmazonSendMessageProcessor Create(string destinationAddress)
        {
            if (destinationAddress == null)
            {
                throw new ArgumentNullException(nameof(destinationAddress));
            }

            if (destinationAddress.StartsWith(c_SnsArn, true, CultureInfo.InvariantCulture))
            {
                return new SnsAmazonSendMessageProcessor(destinationAddress, _amazonInternalSettings);
            }

            return new SqsAmazonSendMessageProcessor(destinationAddress, _amazonInternalSettings, _amazonSqsQueueContext);
        }
    }
}
