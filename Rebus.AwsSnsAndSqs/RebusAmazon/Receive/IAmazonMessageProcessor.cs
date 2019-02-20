using Rebus.Messages;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonMessageProcessor
    {
        TransportMessage ProcessMessage();
    }
}