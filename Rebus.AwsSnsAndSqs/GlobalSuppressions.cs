﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "<Pending>", Scope = "type", Target = "~T:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonSynchronizationContext")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonSQSTransport.#ctor(Rebus.AwsSnsAndSqs.RebusAmazon.IAmazonInternalSettings)")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonSynchronizationContext.Send(System.Threading.SendOrPostCallback,System.Object)")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonSQSQueuePurgeUtility.Purge(System.String)")]
[assembly: SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "<Pending>", Scope = "type", Target = "~T:Rebus.AwsSnsAndSqs.RebusAmazon.SnsRebusException")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonTransportMessageSerializer.Deserialize(System.String)~Rebus.AwsSnsAndSqs.RebusAmazon.AmazonTransportMessage")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonTransportMessageSerializer.Serialize(Rebus.AwsSnsAndSqs.RebusAmazon.AmazonTransportMessage)~System.String")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.RebusAmazon.AmazonSQSQueueContext.GetDestinationQueueUrlByName(System.String,Rebus.Transport.ITransactionContext)~System.String")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:Rebus.AwsSnsAndSqs.Config.AmazonOneWayConfigExtension.UseAmazonSnsAndSqsAsOneWayClient(Rebus.Config.StandardConfigurer{Rebus.Transport.ITransport},Rebus.AwsSnsAndSqs.IAmazonCredentialsFactory,Amazon.SQS.AmazonSQSConfig,Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceConfig,Rebus.AwsSnsAndSqs.Config.AmazonSnsAndSqsTransportOptions,Rebus.AwsSnsAndSqs.ITopicFormatter,Rebus.AwsSnsAndSqs.RebusAmazon.Send.SnsAttributeMapperBuilder)")]
[assembly: SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "<Pending>", Scope = "type", Target = "~T:Rebus.AwsSnsAndSqs.RebusAmazon.SnsRebusException")]
[assembly: SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "<Pending>", Scope = "type", Target = "~T:Rebus.AwsSnsAndSqs.RebusAmazon.Extensions.AmazonWebServiceException")]
