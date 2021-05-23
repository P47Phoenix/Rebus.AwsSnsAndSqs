namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Amazon.SQS;
    using Config;

    public interface IAmazonSqsSettings : IAmazonSettings
    {
        AmazonSQSConfig AmazonSqsConfig { get; }
        AmazonSnsAndSqsTransportOptions AmazonSnsAndSqsTransportOptions { get; }
    }
}
