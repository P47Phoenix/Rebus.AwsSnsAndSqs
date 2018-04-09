using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.Amazon.Extensions
{
    internal static class AmazonInternalSettingsExtensions
    {
        public static IAmazonSimpleNotificationService CreateSnsClient(this IAmazonSnsSettings amazonSnsSettings)
        {
            return new AmazonSimpleNotificationServiceClient(amazonSnsSettings.AmazonCredentialsFactory.Create(),
                amazonSnsSettings.AmazonSimpleNotificationServiceConfig);
        }

        public static IAmazonSQS CreateSqsClient(this IAmazonSqsSettings amazonSqsSettings, ITransactionContext transactionContext)
        {
            return transactionContext.GetOrAdd(AmazonConstaints.ClientContextKey, () =>
            {
                var amazonSqsClient = new AmazonSQSClient(amazonSqsSettings.AmazonCredentialsFactory.Create(), amazonSqsSettings.AmazonSqsConfig);
                transactionContext.OnDisposed(amazonSqsClient.Dispose);
                return amazonSqsClient;
            });
        }
    }
}
