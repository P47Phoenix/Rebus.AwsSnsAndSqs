using Rebus.Bus;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSQSTransport : ITransport, IInitializable, ISubscriptionStorage
    {
    }
}
