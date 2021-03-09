using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.Runtime.Internal.Util;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Rebus.Logging;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.RebusAmazon.Extensions
{
    internal static class AmazonInternalSettingsExtensions
    {
        internal class LoggerSettingsHelper
        {
            private readonly ILog m_logger;

            public LoggerSettingsHelper(IRebusLoggerFactory rebusLoggerFactory)
            {
                m_logger = rebusLoggerFactory.GetLogger<LoggerSettingsHelper>();
            }

            public ILog Logger => m_logger;
        }

        private static LoggerSettingsHelper s_loggerSettingsHelper = null;

        private static ILog GetLogger(this IRebusLoggerFactory rebusLoggerFactory)
        {
            s_loggerSettingsHelper = s_loggerSettingsHelper ?? new LoggerSettingsHelper(rebusLoggerFactory);

            return s_loggerSettingsHelper.Logger;
        }
        private static readonly ConcurrentDictionary<string, string> s_topicArnCache = new ConcurrentDictionary<string, string>();

        public static IAmazonSimpleNotificationService CreateSnsClient(this IAmazonSnsSettings amazonSnsSettings, ITransactionContext transactionContext)
        {
            return transactionContext.GetOrAdd(AmazonConstaints.SnsClientContextKey, () =>
            {
                var client = new AmazonSimpleNotificationServiceClient(amazonSnsSettings.AmazonCredentialsFactory.Create(), amazonSnsSettings.AmazonSimpleNotificationServiceConfig);

                transactionContext.OnDisposed((ITransactionContext context) => client.Dispose());

                return client;
            });
        }

        public static IAmazonSQS CreateSqsClient(this IAmazonSqsSettings amazonSqsSettings, ITransactionContext transactionContext)
        {
            return transactionContext.GetOrAdd(AmazonConstaints.SqsClientContextKey, () =>
            {
                var amazonSqsClient = new AmazonSQSClient(amazonSqsSettings.AmazonCredentialsFactory.Create(), amazonSqsSettings.AmazonSqsConfig);
                transactionContext.OnDisposed((ITransactionContext context) => amazonSqsClient.Dispose());
                return amazonSqsClient;
            });
        }

        public static async Task<string> GetTopicArn(this IAmazonInternalSettings amazonInternalSettings, string topic, RebusTransactionScope scope = null)
        {
            var result = await Task.FromResult(s_topicArnCache.GetOrAdd(topic, s =>
            {
                var rebusTransactionScope = scope ?? new RebusTransactionScope();
                try
                {
                    var logger = amazonInternalSettings.RebusLoggerFactory.GetLogger();

                    var snsClient = amazonInternalSettings.CreateSnsClient(rebusTransactionScope.TransactionContext);

                    var formatedTopicName = amazonInternalSettings.TopicFormatter.FormatTopic(topic);

                var findTopicAsync = snsClient.FindTopicAsync(formatedTopicName);

                AsyncHelpers.RunSync(() => findTopicAsync);

                var findTopicResult = findTopicAsync.Result;

                string topicArn = findTopicResult?.TopicArn;

                if (findTopicResult == null)
                {
                        logger.Debug($"Did not find sns topic {0}", formatedTopicName);
                    var task = snsClient.CreateTopicAsync(new CreateTopicRequest(formatedTopicName));
                    AsyncHelpers.RunSync(() => task);
                    topicArn = task.Result?.TopicArn;
                        logger.Debug($"Created sns topic {0} => {1}", formatedTopicName, topicArn);
                }

                    logger.Debug($"Using sns topic {0} => {1}", formatedTopicName, topicArn);
                return topicArn;
                }
                finally
                {
                    if (scope == null)
                    {
                        rebusTransactionScope.Dispose();
                    }
                }
            }));

            return result;
        }

        public static async Task CheckSqsPolicy(this IAmazonInternalSettings amazonInternalSettings, ITransactionContext transactionContext, string destinationQueueUrlByName, SqsInfo sqsInformation, string topicArn)
        {
            var logger = amazonInternalSettings.RebusLoggerFactory.GetLogger();

            var sqsClient = amazonInternalSettings.CreateSqsClient(transactionContext);

            var attributes = await sqsClient.GetAttributesAsync(destinationQueueUrlByName);

            var policyKey = "Policy";

            var statement = new Statement(Statement.StatementEffect.Allow).WithPrincipals(Principal.AllUsers).WithResources(new Resource(sqsInformation.Arn)).WithConditions(ConditionFactory.NewSourceArnCondition(topicArn)).WithActionIdentifiers(SQSActionIdentifiers.SendMessage);

            Policy sqsPolicy;

            var setPolicy = false;

            if (attributes.ContainsKey(policyKey))
            {
                logger.Debug($"Updating existing policy on sqs queue {0} for topic {1}", sqsInformation.Url, topicArn);
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
                logger.Debug($"Creating policy on sqs queue {0} for topic {1}", sqsInformation.Url, topicArn);
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
