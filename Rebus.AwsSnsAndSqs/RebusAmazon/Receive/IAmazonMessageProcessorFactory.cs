namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Amazon.SQS.Model;
    using Messages;
    using Newtonsoft.Json.Linq;
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