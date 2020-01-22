namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using System.Collections.Concurrent;
    using Transport;

    internal static class TransportWrapperSingleton
    {
        private static ConcurrentDictionary<string, TransportWrapper> _concurrentDictionary = new ConcurrentDictionary<string, TransportWrapper>();

        public static AmazonSqsTransport GetAmazonSqsTransport(string inputQueueAddress)
        {
            if (_concurrentDictionary.TryGetValue(inputQueueAddress, out TransportWrapper transportWrapper))
            {
                return transportWrapper.GetAmazonSqsTransport();
            }
            return null;
        }

        internal static void Register(string inputQueueAddress, AmazonSqsTransport transport)
        {
            _concurrentDictionary.AddOrUpdate(inputQueueAddress ?? string.Empty, s => new TransportWrapper(transport), (s, wrapper) => new TransportWrapper(transport));
        }
    }
}
