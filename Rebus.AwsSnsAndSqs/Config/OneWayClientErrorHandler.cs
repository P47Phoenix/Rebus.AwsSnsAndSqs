namespace Rebus.AwsSnsAndSqs.Config
{
    using System;
    using System.Threading.Tasks;
    using Messages;
    using Retry;
    using Transport;

    public class OneWayClientErrorHandler : IErrorHandler
    {
        public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
        {
            throw new NotImplementedException("One way client should never get here");
        }
    }
}
