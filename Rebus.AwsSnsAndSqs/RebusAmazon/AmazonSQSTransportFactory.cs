using System.Collections.Concurrent;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonSQSTransportFactory : IAmazonSQSTransportFactory
    {
        private readonly IAmazonInternalSettings m_amazonInternalSettings;

        public AmazonSQSTransportFactory(IAmazonInternalSettings amazonInternalSettings)
        {
            m_amazonInternalSettings = amazonInternalSettings;
        }

        public IAmazonSQSTransport Create()
        {
            var transport = new AmazonSqsTransport(m_amazonInternalSettings);
            TransportWrapperSingleton.Register(m_amazonInternalSettings.InputQueueAddress, transport);
            return transport;
        }
    }
}
