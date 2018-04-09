using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    internal class AmazonConstaints
    {
        public const string OutgoingMessagesItemsKey = "SQS_OutgoingMessages";

        public const string ClientContextKey = "SQS_Client";

        public const string SqsTimeoutManagerText = "A disabled timeout manager was installed as part of the SQS configuration, becuase the transport has native support for deferred messages";
    }
}
