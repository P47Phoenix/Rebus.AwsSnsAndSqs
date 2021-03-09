using Rebus.Time;
using System.Collections.Concurrent;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonSQSTransportFactory : IAmazonSQSTransportFactory
    {
        private readonly IAmazonInternalSettings m_amazonInternalSettings;
        private readonly IRebusTime _rebusTime;

        public AmazonSQSTransportFactory(IAmazonInternalSettings amazonInternalSettings, IRebusTime rebusTime)
        {
            m_amazonInternalSettings = amazonInternalSettings;
            _rebusTime = rebusTime;
        }

        public IAmazonSQSTransport Create()
        {
            var transport = new AmazonSqsTransport(m_amazonInternalSettings, _rebusTime);
            TransportWrapperSingleton.Register(m_amazonInternalSettings.InputQueueAddress, transport);
            return transport;
        }
    }
}
