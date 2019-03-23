namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal interface IAmazonSendMessageCommandFactory
    {
        IAmazonSendMessageProcessor Create(string destinationAddress);
    }
}