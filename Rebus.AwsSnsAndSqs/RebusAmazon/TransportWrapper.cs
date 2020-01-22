namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using Transport;

    internal class TransportWrapper : ITransportWrapper
    {
        private readonly WeakReference<AmazonSqsTransport> _amazonSqsTransport;

        public TransportWrapper(ITransport transport)
        {
            if (transport is AmazonSqsTransport amazonSqsTransport)
            {
                _amazonSqsTransport = new WeakReference<AmazonSqsTransport>(amazonSqsTransport);
                return;
            }

            throw new ArgumentException($"expected {typeof(AmazonSqsTransport).Name} and got {transport.GetType().Name}", nameof(transport));
        }

        public AmazonSqsTransport GetAmazonSqsTransport()
        {
            if (_amazonSqsTransport.TryGetTarget(out AmazonSqsTransport amazonSqsTransport))
            {
                return amazonSqsTransport;
            }

            return null;
        }
    }
}
