namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal interface ITransportWrapper
    {
        AmazonSQSTransport GetAmazonSqsTransport();
    }
}
