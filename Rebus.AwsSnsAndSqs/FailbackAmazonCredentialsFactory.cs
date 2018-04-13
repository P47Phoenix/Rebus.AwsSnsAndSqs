namespace Rebus.AwsSnsAndSqs
{
    using Amazon.Runtime;

    public class FailbackAmazonCredentialsFactory : IAmazonCredentialsFactory
    {
        public AWSCredentials Create()
        {
            return FallbackCredentialsFactory.GetCredentials();
        }
    }
}
