using Amazon.Runtime;

namespace Rebus.AwsSnsAndSqs
{
    public class FailbackAmazonCredentialsFactory : IAmazonCredentialsFactory
    {
        public AWSCredentials Create()
        {
            return FallbackCredentialsFactory.GetCredentials();
        }
    }
}
