﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqsTests
{
    public abstract class SqsFixtureBase : FixtureBase
    {
        private readonly Encoding _defaultEncoding = Encoding.UTF8;

        protected async Task WithContext(Func<ITransactionContext, Task> contextAction, bool completeTransaction = true)
        {
            using (var scope = new RebusTransactionScope())
            {
                await contextAction(AmbientTransactionContext.Current);

                if (completeTransaction)
                {
                    await scope.CompleteAsync();
                }
            }
        }

        protected string GetStringBody(TransportMessage transportMessage)
        {
            if (transportMessage == null)
            {
                throw new InvalidOperationException("Cannot get string body out of null message!");
            }

            return _defaultEncoding.GetString(transportMessage.Body);
        }

        protected TransportMessage MessageWith(string stringBody)
        {
            var headers = new Dictionary<string, string> {{Headers.MessageId, Guid.NewGuid().ToString()}, {Headers.CorrelationId, Guid.NewGuid().ToString()}};
            var body = _defaultEncoding.GetBytes(stringBody);
            return new TransportMessage(headers, body);
        }
    }
}
