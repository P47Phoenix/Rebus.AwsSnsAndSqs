using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.Logging;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonCreateSQSQueue
    {
        private readonly IAmazonInternalSettings m_AmazonInternalSettings;
        private readonly ILog m_log;

        public AmazonCreateSQSQueue(IAmazonInternalSettings amazonInternalSettings)
        {
            m_AmazonInternalSettings = amazonInternalSettings ?? throw new ArgumentNullException(nameof(amazonInternalSettings));
            m_log = m_AmazonInternalSettings.RebusLoggerFactory.GetLogger<AmazonCreateSQSQueue>();
        }

        /// <summary>
        ///     Creates the queue with the given name
        /// </summary>
        public void CreateQueue(string address)
        {
            m_log.Info("Creating queue {queueName} on region {regionEndpoint}", address, m_AmazonInternalSettings.AmazonSqsConfig.RegionEndpoint);

            var amazonSqsConfig = m_AmazonInternalSettings.AmazonSqsConfig;
            var awsCredentials = m_AmazonInternalSettings.AmazonCredentialsFactory.Create();

            using (var client = new AmazonSQSClient(awsCredentials, amazonSqsConfig))
            {
                var queueName = GetQueueNameFromAddress(address);

                // Check if queue exists
                try
                {
                    var request = new GetQueueUrlRequest(queueName);
                    // See http://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TSQSGetQueueUrlRequest.html for options
                    var getQueueUrlTask = client.GetQueueUrlAsync(request);
                    AsyncHelpers.RunSync(() => getQueueUrlTask);
                    var getQueueUrlResponse = getQueueUrlTask.Result;

                    if (getQueueUrlResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Could not check for existing queue '{queueName}' - got HTTP {getQueueUrlResponse.HttpStatusCode}");
                    }

                    var visibilityTimeout = ((int) m_AmazonInternalSettings.AmazonPeekLockDuration.PeekLockDuration.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                    // See http://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TSQSSetQueueAttributesRequest.html for options
                    var setAttributesTask = client.SetQueueAttributesAsync(getQueueUrlResponse.QueueUrl, new Dictionary<string, string> {["VisibilityTimeout"] = visibilityTimeout});
                    AsyncHelpers.RunSync(() => setAttributesTask);
                    var setAttributesResponse = setAttributesTask.Result;

                    if (setAttributesResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Could not set attributes for queue '{queueName}' - got HTTP {setAttributesResponse.HttpStatusCode}");
                    }
                }
                catch (QueueDoesNotExistException)
                {
                    // See http://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TSQSCreateQueueRequest.html for options
                    var createQueueRequest = new CreateQueueRequest(queueName) {Attributes = {["VisibilityTimeout"] = ((int) m_AmazonInternalSettings.AmazonPeekLockDuration.PeekLockDuration.TotalSeconds).ToString(CultureInfo.InvariantCulture)}};
                    var task = client.CreateQueueAsync(createQueueRequest);
                    AsyncHelpers.RunSync(() => task);
                    var response = task.Result;

                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Could not create queue '{queueName}' - got HTTP {response.HttpStatusCode}");
                    }
                }
            }
        }

        private static string GetQueueNameFromAddress(string address)
        {
            if (!Uri.IsWellFormedUriString(address, UriKind.Absolute))
            {
                return address;
            }

            var queueFullAddress = new Uri(address);

            return queueFullAddress.Segments[queueFullAddress.Segments.Length - 1];
        }
    }
}
