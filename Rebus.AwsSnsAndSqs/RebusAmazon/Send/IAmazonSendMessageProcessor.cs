namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System.Threading.Tasks;
    using Messages;
    using Transport;

    internal interface IAmazonSendMessageProcessor
    {
        Task SendAsync(TransportMessage message, ITransactionContext context);
    }
}