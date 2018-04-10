using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSqsSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonSQSConfig AmazonSqsConfig { get; }
        AmazonSnsAndSqsTransportOptions AmazonSnsAndSqsTransportOptions { get; }
    }
}