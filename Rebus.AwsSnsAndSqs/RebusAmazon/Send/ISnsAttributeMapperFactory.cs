namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;

    internal interface ISnsAttributeMapperFactory
    {
        /// <summary>
        /// Creates the specified message value.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        ISnsAttributeMapper Create(Type messageType);
    }
}
