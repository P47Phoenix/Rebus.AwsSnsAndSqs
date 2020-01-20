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

            throw new ArgumentException($"expected {typeof(AmazonSQSTransport).Name} and got {transport.GetType().Name}", nameof(transport));
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
