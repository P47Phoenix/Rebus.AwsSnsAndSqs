﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Timeouts;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonDisabledTimeoutManager : ITimeoutManager
    {
        public Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            var messageIdToPrint = headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>";

            var message = $"Received message with ID {messageIdToPrint} which is supposed to be deferred until {approximateDueTime} -" + " this is a problem, because the internal handling of deferred messages is" + " disabled when using SQS as the transport layer in, which" + " case the native support for a specific future enqueueing time is used...";

            throw new InvalidOperationException(message);
        }

        public Task<DueMessagesResult> GetDueMessages()
        {
            return Task.FromResult(DueMessagesResult.Empty);
        }
    }
}
