namespace Rebus.AwsSnsAndSqs.RebusAmazon.Receive
{
    using Message = Amazon.SQS.Model.Message;

    /// <summary></summary>
    public interface IAmazonMessageProcessorFactory
    {
        /// <summary>Creates the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        IAmazonMessageProcessor Create(Message message);
    }
}