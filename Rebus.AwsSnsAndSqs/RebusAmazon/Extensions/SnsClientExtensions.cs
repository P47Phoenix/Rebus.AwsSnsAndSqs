using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;

namespace Rebus.AwsSnsAndSqs.RebusAmazon.Extensions
{
    internal static class SnsClientExtensions
    {
        public static async Task<string> GetTopicArn(this IAmazonSimpleNotificationService snsClient, IAmazonInternalSettings m_AmazonInternalSettings, string topic)
        {
            var formatedTopicName = m_AmazonInternalSettings.TopicFormatter.FormatTopic(topic);

            var findTopicResult = await snsClient.FindTopicAsync(formatedTopicName);

            var topicArn = findTopicResult?.TopicArn;

            if (topicArn == null)
            {
                var createTopicResponse = await snsClient.CreateTopicAsync(formatedTopicName);

                if (createTopicResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new SnsRebusException($"Error creating topic {formatedTopicName}.", createTopicResponse.CreateAmazonExceptionFromResponse());
                }

                topicArn = createTopicResponse.TopicArn;
            }

            return topicArn;
        }
    }
}
