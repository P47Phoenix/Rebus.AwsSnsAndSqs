namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using Rebus.Time;
    using System;
    using System.Globalization;

    internal class AmazonSendMessageCommandFactory : IAmazonSendMessageCommandFactory
    {
        private const string c_SnsArn = "arn:aws:sns:";
        private readonly IAmazonInternalSettings _amazonInternalSettings;
        private readonly AmazonSQSQueueContext _amazonSqsQueueContext;
        private readonly IRebusTime _rebusTime;

        public AmazonSendMessageCommandFactory(IAmazonInternalSettings amazonInternalSettings, AmazonSQSQueueContext amazonSqsQueueContext, IRebusTime rebusTime)
        {
            this._amazonInternalSettings = amazonInternalSettings ?? throw new ArgumentNullException(nameof(amazonInternalSettings));
            this._amazonSqsQueueContext = amazonSqsQueueContext ?? throw new ArgumentNullException(nameof(amazonSqsQueueContext));
            this._rebusTime = rebusTime;
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

            return new SqsAmazonSendMessageProcessor(destinationAddress, _amazonInternalSettings, _amazonSqsQueueContext, _rebusTime);
        }
    }
}
