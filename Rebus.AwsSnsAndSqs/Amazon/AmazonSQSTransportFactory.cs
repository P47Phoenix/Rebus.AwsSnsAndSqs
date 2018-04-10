﻿using System.Collections.Concurrent;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.Amazon
{
    internal class AmazonSQSTransportFactory : IAmazonSQSTransportFactory
    {
        private static ConcurrentDictionary<IAmazonInternalSettings, AmazonSQSTransport> m_AmazonSqsTransports = new ConcurrentDictionary<IAmazonInternalSettings, AmazonSQSTransport>();
        private IAmazonInternalSettings m_amazonInternalSettings;

        public AmazonSQSTransportFactory(IAmazonInternalSettings amazonInternalSettings)
        {
            this.m_amazonInternalSettings = amazonInternalSettings;
        }

        public IAmazonSQSTransport Create()
        {
            return m_AmazonSqsTransports.GetOrAdd(m_amazonInternalSettings, settings => new AmazonSQSTransport(settings));
        }
    }
}
