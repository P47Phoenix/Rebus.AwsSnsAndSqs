using System.Collections.Concurrent;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonSQSTransportFactory : IAmazonSQSTransportFactory
    {
        private static readonly ConcurrentDictionary<IAmazonInternalSettings, AmazonSQSTransport> m_AmazonSqsTransports = new ConcurrentDictionary<IAmazonInternalSettings, AmazonSQSTransport>();
        private readonly IAmazonInternalSettings m_amazonInternalSettings;

        public AmazonSQSTransportFactory(IAmazonInternalSettings amazonInternalSettings)
        {
            m_amazonInternalSettings = amazonInternalSettings;
        }

        public IAmazonSQSTransport Create()
        {
            return m_AmazonSqsTransports.GetOrAdd(m_amazonInternalSettings, settings => new AmazonSQSTransport(settings));
        }
    }
}
