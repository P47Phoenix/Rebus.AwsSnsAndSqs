using Rebus.Messages;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonOutgoingMessage
    {
        public AmazonOutgoingMessage(string destinationAddress, TransportMessage transportMessage)
        {
            DestinationAddress = destinationAddress;
            TransportMessage = transportMessage;
        }

        public string DestinationAddress { get; }
        public TransportMessage TransportMessage { get; }
    }
}
