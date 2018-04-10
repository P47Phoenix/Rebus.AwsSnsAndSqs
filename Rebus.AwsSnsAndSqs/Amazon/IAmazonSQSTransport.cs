using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    using Bus;
    using Subscriptions;

    public interface IAmazonSQSTransport : ITransport, IInitializable, ISubscriptionStorage
    {
    }
}