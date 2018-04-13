namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonConstaints
    {
        public const string OutgoingMessagesItemsKey = "SQS_OutgoingMessages";

        public const string SqsClientContextKey = "SQS_Client";

        public const string SnsClientContextKey = "SNS_Client";

        public const string SqsTimeoutManagerText = "A disabled timeout manager was installed as part of the SQS configuration, becuase the transport has native support for deferred messages";
    }
}
