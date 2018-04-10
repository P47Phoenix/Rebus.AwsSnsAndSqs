using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    public interface IAmazonSqsSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonSQSConfig AmazonSqsConfig { get; }
        AmazonSnsAndSqsTransportOptions AmazonSnsAndSqsTransportOptions { get; }
    }
}