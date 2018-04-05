using Rebus.Messages;
#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.Amazon
{
    internal class AmazonOutgoingMessage
    {
        public string DestinationAddress { get; }
        public TransportMessage TransportMessage { get; }

        public AmazonOutgoingMessage(string destinationAddress, TransportMessage transportMessage)
        {
            DestinationAddress = destinationAddress;
            TransportMessage = transportMessage;
        }
    }
}
