using Amazon.SimpleNotificationService;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSnsSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; }
    }
}