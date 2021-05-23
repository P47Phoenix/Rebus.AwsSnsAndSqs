namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Bus;
    using Subscriptions;
    using Transport;

    public interface IAmazonSQSTransport : ITransport, IInitializable, ISubscriptionStorage
    {
    }
}
