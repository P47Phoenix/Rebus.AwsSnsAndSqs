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
            return new AmazonSimpleNotificationServiceClient(amazonSnsSettings.Credentials,
                amazonSnsSettings.AmazonSimpleNotificationServiceConfig);
        }

        public static IAmazonSQS CreateSqsClient(this IAmazonSqsSettings amazonSqsSettings, ITransactionContext transactionContext)
        {
            return amazonSqsSettings.AmazonSQSTransportOptions.GetOrCreateClient(transactionContext,
                amazonSqsSettings.Credentials, amazonSqsSettings.AmazonSqsConfig);
        }
    }
}
