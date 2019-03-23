namespace Rebus.AwsSnsAndSqs.RebusAmazon.Receive
{
    using Messages;

    public interface IAmazonMessageProcessor
    {
        TransportMessage ProcessMessage();
    }
}