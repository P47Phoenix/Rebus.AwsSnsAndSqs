using System;

namespace Rebus.AwsSnsAndSqs.Config
{
    using System.Collections.Generic;
    using Amazon.SimpleNotificationService.Model;

    internal interface ISnsAttributeMapper
    {
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        IDictionary<string, MessageAttributeValue> GetAttributes(object value);

        /// <summary>
        ///   Gets the value of the mapper for.
        /// </summary>
        /// <value>
        ///   The value of the mapper for.
        /// </value>
        Type MapperForType { get; }
    }
}
