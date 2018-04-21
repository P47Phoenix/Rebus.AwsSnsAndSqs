using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSqsSettings : IAmazonSettings
    {
        AmazonSQSConfig AmazonSqsConfig { get; }
        AmazonSnsAndSqsTransportOptions AmazonSnsAndSqsTransportOptions { get; }
    }
}
