namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    internal interface IAmazonSendMessageCommandFactory
    {
        IAmazonSendMessageProcessor Create(string destinationAddress);
    }
}