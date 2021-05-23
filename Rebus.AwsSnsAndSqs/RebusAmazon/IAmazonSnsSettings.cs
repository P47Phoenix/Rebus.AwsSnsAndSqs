namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Amazon.SimpleNotificationService;

    public interface IAmazonSnsSettings : IAmazonSettings
    {
        AmazonSimpleNotificationServiceConfig AmazonSimpleNotificationServiceConfig { get; }
    }
}
