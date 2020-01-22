namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal interface ITransportWrapper
    {
        AmazonSqsTransport GetAmazonSqsTransport();
    }
}
