namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using Transport;

    internal class TransportWrapper : ITransportWrapper
    {
        private readonly WeakReference<AmazonSQSTransport> _amazonSqsTransport;

        public TransportWrapper(ITransport transport)
        {
            if (transport is AmazonSQSTransport amazonSqsTransport)
            {
                _amazonSqsTransport = new WeakReference<AmazonSQSTransport>(amazonSqsTransport);
                return;
            }

            throw new ArgumentException(nameof(transport), $"expected {typeof(AmazonSQSTransport).Name} and got {transport.GetType().Name}");
        }

        public AmazonSQSTransport GetAmazonSqsTransport()
        {
            if (_amazonSqsTransport.TryGetTarget(out AmazonSQSTransport amazonSqsTransport))
            {
                return amazonSqsTransport;
            }

            return null;
        }
    }
}
