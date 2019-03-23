namespace Rebus.AwsSnsAndSqs
{
    using System;

    /// <summary>
    /// Explicit and "Vin-opinioned" and complaint message contract for uniform messaging.
    ///  
    ///  All of these fields, except message, will be "filterable" by policy in SNS, 
    ///  see https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html
    ///  
    /// For more information on Architectural Decisions on Event message contracts,
    /// see https://pages.ghe.coxautoinc.com/VinSolutions/Architecture/adr/0004-message-contract-standards-adr.html
    /// 
    /// </summary>
    public abstract class VinEventMessage
    {
        /// <summary>
        ///  REQUIRED: The payload you wish to send
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// REQUIRED: The event id attribute represents the identifier used for correlating business actions across one or more events in a 
        /// software system. These values are unique to the event, but are duplicated across all messages related to a given event, 
        /// enabling traceability across multiple parts of a software system.
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// REQUIRED(defaulted to new Guid): The message id attribute represents the identifier for an event message. These values are unique to each message, 
        /// and should not be duplicated. Additionally, the message id should be used as the key to determine whether a message 
        /// has already been processed in a given part of the system, resulting in actions that can be idempotent.
        /// </summary>
        public Guid MessageId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// REQUIRED: The version attribute provides a way for the structure of messages to change over time much the same way as an API or 
        /// component library is versioned. The responsibility of generating and managing the lifetime of a message version 
        /// belongs to the publisher of the message, with the subscriber handling the responsibility of ignoring versions it 
        /// does not support and resolving those it does using message filtering. The versioning standard we use at Vin is 
        /// semantic versioning. Visit the versioning event messages documentation for more details about how event publishers 
        /// should version events generated using Amazon SNS.
        /// </summary>
        public Version MessageVersion { get; set; }

        /// <summary>
        /// REQUIRED: The source attribute represents the origin of the event. Valid values for the source attribute can be things like 
        /// the domain or solution name. For example, if the event originates from the Common Customer platform, the value could 
        /// be “Common Customer” or if it originates from the Customer Merge solution the value could be “Customer Merge Process”. 
        /// This attribute is purposed specifically for message origin identification and should not be reused.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// REQUIRED: The type attribute represents the type of event a given message is related to. The value should be domain specific and 
        /// based on the business or technical action the related event represents. For example, for an event that creates a message 
        /// representing a business action where a salesperson sends an outbound email to a customer, the value could be 
        /// “Outbound Sales Email”. This attribute is purposed specifically for message type identification and should be reused.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// OPTIONAL: The href attribute represents a uri used to access the transaction details from the domain from which the event 
        /// message originated. Event messages are not intended to contain the entirety of a domain action’s transaction details, 
        /// only a subset that provide enough context for a subscriber to take action on the message. The publisher has the responsibility
        /// of providing the transaction link on the event message should the event subscribers need more transaction level detail. An 
        /// example of a properly formed transaction link would look like: https://api.vinmanager.com/customer/1234. In this example, the 
        /// link would result customer details for customer number 1234.
        /// </summary>
        public string Href { get; set; }

        /// <summary>
        /// REQUIRED(Defaulted to UtcNow): The timestamp attribute represents when the event message was generated. The format of the timestamp should be represented
        /// in UTC, with the subscriber holding the responsibility of converting to localized time where necessary.
        /// EX: 2019-03-20T18:34:27  
        /// </summary>
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    }

}
