using Amazon.SimpleNotificationService;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    public interface IAmazonSnsSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
        AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; }
    }
}