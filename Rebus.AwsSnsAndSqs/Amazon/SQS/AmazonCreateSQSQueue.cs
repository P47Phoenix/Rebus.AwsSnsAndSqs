using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Logging;

namespace Rebus.AwsSnsAndSqs.Amazon.SQS
{
    public class AmazonCreateSQSQueue
    {
        private AmazonSQSTransportOptions _options;
        private AWSCredentials _credentials;
        private readonly AmazonPeekLockDuration _peekLockDuration;
        private ILog _log;
        private AmazonSQSConfig _amazonSqsConfig;

        public AmazonCreateSQSQueue(AmazonSQSTransportOptions options, AWSCredentials credentials, AmazonPeekLockDuration peekLockDuration, ILog log, AmazonSQSConfig amazonSqsConfig)
        {
            _options = options;
            _credentials = credentials;
            _peekLockDuration = peekLockDuration;
            _log = log;
            _amazonSqsConfig = amazonSqsConfig;
        }

        /// <summary>
        /// Creates the queue with the given name
        /// </summary>
        public void CreateQueue(string address)
        {
            if (_options.CreateQueues)
            {
                _log.Info("Creating queue {queueName} on region {regionEndpoint}", address, _amazonSqsConfig.RegionEndpoint);

                using (var client = new AmazonSQSClient(_credentials, _amazonSqsConfig))
                {
                    var queueName = GetQueueNameFromAddress(address);

                    // Check if queue exists
                    try
                    {
                        // See http://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TSQSGetQueueUrlRequest.html for options
                        var getQueueUrlTask = client.GetQueueUrlAsync(new GetQueueUrlRequest(queueName));
                        AmazonAsyncHelpers.RunSync(() => getQueueUrlTask);
                        var getQueueUrlResponse = getQueueUrlTask.Result;
                        if (getQueueUrlResponse.HttpStatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"Could not check for existing queue '{queueName}' - got HTTP {getQueueUrlResponse.HttpStatusCode}");
                        }

                        // See http://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TSQSSetQueueAttributesRequest.html for options
                        var setAttributesTask = client.SetQueueAttributesAsync(getQueueUrlResponse.QueueUrl, new Dictionary<string, string>
                        {
                            ["VisibilityTimeout"] = ((int) _peekLockDuration.PeekLockDuration.TotalSeconds).ToString(CultureInfo.InvariantCulture)
                        });
                        AmazonAsyncHelpers.RunSync(() => setAttributesTask);
                        var setAttributesResponse = setAttributesTask.Result;
                        if (setAttributesResponse.HttpStatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"Could not set attributes for queue '{queueName}' - got HTTP {setAttributesResponse.HttpStatusCode}");
                        }
                    }
                    catch (QueueDoesNotExistException)
                    {
                        // See http://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TSQSCreateQueueRequest.html for options
                        var createQueueRequest = new CreateQueueRequest(queueName)
                        {
                            Attributes =
                            {
                                ["VisibilityTimeout"] = ((int) _peekLockDuration.PeekLockDuration.TotalSeconds).ToString(CultureInfo.InvariantCulture)
                            }
                        };
                        var task = client.CreateQueueAsync(createQueueRequest);
                        AmazonAsyncHelpers.RunSync(() => task);
                        var response = task.Result;

                        if (response.HttpStatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"Could not create queue '{queueName}' - got HTTP {response.HttpStatusCode}");
                        }
                    }
                }


            }
        }

        private static string GetQueueNameFromAddress(string address)
        {
            if (!Uri.IsWellFormedUriString(address, UriKind.Absolute)) return address;

            var queueFullAddress = new Uri(address);

            return queueFullAddress.Segments[queueFullAddress.Segments.Length - 1];
        }
    }
}