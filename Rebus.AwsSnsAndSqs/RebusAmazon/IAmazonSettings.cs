namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSettings
    {
        IAmazonCredentialsFactory AmazonCredentialsFactory { get; }
    }
}