using System;
using System.Collections.Concurrent;
using System.Net;
using Amazon.Runtime;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Amazon.Extensions;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Exceptions;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.Amazon.SQS
{
    internal class AmazonSQSQueueContext
    {

        private readonly ConcurrentDictionary<string, string> _queueUrls = new ConcurrentDictionary<string, string>();
        private readonly AmazonInternalSettings m_AmazonInternalSettings;

        public AmazonSQSQueueContext(AmazonInternalSettings m_AmazonInternalSettings)
        {
            this.m_AmazonInternalSettings = m_AmazonInternalSettings;
        }

        public string GetDestinationQueueUrlByName(string address, ITransactionContext transactionContext)
        {
            var url = _queueUrls.GetOrAdd(address.ToLowerInvariant(), key =>
            {
                if (Uri.IsWellFormedUriString(address, UriKind.Absolute))
                {
                    return address;
                }

                var client = m_AmazonInternalSettings.CreateSqsClient(transactionContext);
                var task = client.GetQueueUrlAsync(address);

                AmazonAsyncHelpers.RunSync(() => task);

                var urlResponse = task.Result;

                if (urlResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    return urlResponse.QueueUrl;
                }

                throw new RebusApplicationException($"could not find Url for address: {address} - got errorcode: {urlResponse.HttpStatusCode}");
            });

            return url;

        }

        public string GetInputQueueUrl(string Address)
        {
            try
            {
                using (var scope = new RebusTransactionScope())
                {
                    var inputQueueUrl = GetDestinationQueueUrlByName(Address, scope.TransactionContext);

                    return inputQueueUrl;
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not get URL of own input queue '{Address}'");
            }
        }
    }

}
