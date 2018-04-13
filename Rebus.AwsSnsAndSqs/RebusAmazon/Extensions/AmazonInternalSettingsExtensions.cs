using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using NUnit.Framework.Constraints;
using Rebus.AwsSnsAndSqs.RebusAmazon.SQS;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.RebusAmazon.Extensions
{
    internal static class AmazonInternalSettingsExtensions
    {
        private static ConcurrentDictionary<string, string> s_topicArnCache = new ConcurrentDictionary<string, string>();

        public static IAmazonSimpleNotificationService CreateSnsClient(this IAmazonSnsSettings amazonSnsSettings, ITransactionContext transactionContext)
        {
            return transactionContext.GetOrAdd(AmazonConstaints.SnsClientContextKey, () =>
            {
                var client = new AmazonSimpleNotificationServiceClient(amazonSnsSettings.AmazonCredentialsFactory.Create(), amazonSnsSettings.AmazonSimpleNotificationServiceConfig);

                transactionContext.OnDisposed(client.Dispose);

                return client;
            });
        }

        public static IAmazonSQS CreateSqsClient(this IAmazonSqsSettings amazonSqsSettings, ITransactionContext transactionContext)
        {
            return transactionContext.GetOrAdd(AmazonConstaints.SqsClientContextKey, () =>
            {
                var amazonSqsClient = new AmazonSQSClient(amazonSqsSettings.AmazonCredentialsFactory.Create(), amazonSqsSettings.AmazonSqsConfig);
                transactionContext.OnDisposed(amazonSqsClient.Dispose);
                return amazonSqsClient;
            });
        }

        public static async Task<string> GetTopicArn(this IAmazonInternalSettings m_AmazonInternalSettings, ITransactionContext transactionContext, string topic)
        {
            return s_topicArnCache.GetOrAdd(topic, s =>
            {
                var snsClient = m_AmazonInternalSettings.CreateSnsClient(transactionContext);

                var formatedTopicName = m_AmazonInternalSettings.TopicFormatter.FormatTopic(topic);

                var findTopicAsync = snsClient.FindTopicAsync(formatedTopicName);

                AmazonAsyncHelpers.RunSync(() => findTopicAsync);

                var findTopicResult = findTopicAsync.Result;

                if (findTopicResult == null)
                {
                    throw new ArgumentOutOfRangeException($"The topic {formatedTopicName} does not exist");
                }

                return findTopicResult.TopicArn;
            });
        }

        public static async Task CheckSqsPolicy(this IAmazonInternalSettings amazonInternalSettings, ITransactionContext transactionContext, string destinationQueueUrlByName, SqsInfo sqsInformation, string topicArn)
        {
            var sqsClient = amazonInternalSettings.CreateSqsClient(transactionContext);

            var attributes = await sqsClient.GetAttributesAsync(destinationQueueUrlByName);

            var policyKey = "Policy";

            var statement = new Statement(Statement.StatementEffect.Allow).WithPrincipals(Principal.AllUsers).WithResources(new Resource(sqsInformation.Arn)).WithConditions(ConditionFactory.NewSourceArnCondition(topicArn)).WithActionIdentifiers(SQSActionIdentifiers.SendMessage);

            Policy sqsPolicy;

            var setPolicy = false;

            if (attributes.ContainsKey(policyKey))
            {
                var policyString = attributes[policyKey];

                attributes = new Dictionary<string, string>();

                sqsPolicy = Policy.FromJson(policyString);

                if (sqsPolicy.CheckIfStatementExists(statement) == false)
                {
                    sqsPolicy = sqsPolicy.WithStatements(statement);
                    attributes.Add(policyKey, sqsPolicy.ToJson());
                    setPolicy = true;
                }
            }
            else
            {
                attributes = new Dictionary<string, string>();
                sqsPolicy = new Policy().WithStatements(statement);
                attributes.Add(policyKey, sqsPolicy.ToJson());
                setPolicy = true;
            }

            if (setPolicy)
            {
                await sqsClient.SetAttributesAsync(sqsInformation.Url, attributes);
            }
        }
    }
}
