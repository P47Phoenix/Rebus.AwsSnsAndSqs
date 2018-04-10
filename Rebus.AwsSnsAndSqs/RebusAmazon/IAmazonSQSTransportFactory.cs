#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public interface IAmazonSQSTransportFactory
    {
        IAmazonSQSTransport Create();
    }
}
