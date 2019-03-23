using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal interface IAmazonSendMessageProcessor
    {
        Task SendAsync(TransportMessage message, ITransactionContext context);
    }
}