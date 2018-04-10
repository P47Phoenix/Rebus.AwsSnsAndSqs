#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.Amazon
{
    public interface IAmazonSQSTransportFactory
    {
        IAmazonSQSTransport Create();
    }
}
