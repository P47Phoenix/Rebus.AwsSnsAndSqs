using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal interface ISendMessage
    {
        /// <inheritdoc />
        Task Send(string destinationAddress, TransportMessage message, ITransactionContext context);
    }
}
