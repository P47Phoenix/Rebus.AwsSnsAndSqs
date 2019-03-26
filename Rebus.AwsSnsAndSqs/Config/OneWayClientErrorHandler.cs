using System;
using System.Threading.Tasks;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.Config
{
    using Rebus.Messages;
    using Retry;

    public class OneWayClientErrorHandler : IErrorHandler
    {
        public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
        {
            throw new NotImplementedException("One way client should never get here");
        }
    }
}
