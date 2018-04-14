using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.Exceptions;
using Rebus.Logging;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class AmazonSQSQueuePurgeUtility
    {
        private readonly IAmazonInternalSettings m_AmazonInternalSettings;
        private readonly ILog m_log;

        public AmazonSQSQueuePurgeUtility(IAmazonInternalSettings amazonInternalSettings)
        {
            m_AmazonInternalSettings = amazonInternalSettings;
            m_log = amazonInternalSettings.RebusLoggerFactory.GetLogger<AmazonSQSQueuePurgeUtility>();
        }

        /// <summary>
        ///     Deletes all messages from the input queue (which is done by receiving them in batches and deleting them, as long as
        ///     it takes)
        /// </summary>
        public void Purge(string queueUrl)
        {
            m_log.Info("Purging queue {queueUrl}", queueUrl);

            try
            {
                // we purge the queue by receiving all messages as fast as we can...
                //the native purge function is not used because it is only allowed to use it
                // once every 60 s
                using (var client = new AmazonSQSClient(m_AmazonInternalSettings.AmazonCredentialsFactory.Create(), m_AmazonInternalSettings.AmazonSqsConfig))
                {
                    var stopwatch = Stopwatch.StartNew();

                    while (true)
                    {
                        var request = new ReceiveMessageRequest(queueUrl) {MaxNumberOfMessages = 10};
                        var receiveTask = client.ReceiveMessageAsync(request);
                        AsyncHelpers.RunSync(() => receiveTask);
                        var response = receiveTask.Result;

                        if (!response.Messages.Any())
                        {
                            break;
                        }

                        var deleteTask = client.DeleteMessageBatchAsync(queueUrl, response.Messages.Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle)).ToList());

                        AsyncHelpers.RunSync(() => deleteTask);

                        var deleteResponse = deleteTask.Result;

                        if (deleteResponse.Failed.Any())
                        {
                            var errors = string.Join(Environment.NewLine, deleteResponse.Failed.Select(f => $"{f.Message} ({f.Id})"));

                            throw new RebusApplicationException($@"Error {deleteResponse.HttpStatusCode} while purging: {errors}");
                        }
                    }

                    m_log.Info("Purging {queueUrl} took {elapsedSeconds} s", queueUrl, stopwatch.Elapsed.TotalSeconds);
                }
            }
            catch (AmazonSQSException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
            {
                if (exception.Message.Contains("queue does not exist"))
                {
                    return;
                }

                throw;
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Error while purging {queueUrl}");
            }
        }
    }
}
