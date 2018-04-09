namespace Rebus.AwsSnsAndSqs
{
    using global::Amazon.Runtime;

    public class FailbackAmazonCredentialsFactory : IAmazonCredentialsFactory
    {
        public AWSCredentials Create()
        {
            return FallbackCredentialsFactory.GetCredentials();
        }
    }
}
