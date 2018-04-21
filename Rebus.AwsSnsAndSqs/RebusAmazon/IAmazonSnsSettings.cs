using Amazon.SimpleNotificationService;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSnsSettings : IAmazonSettings
    {
        AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; }
    }
}
