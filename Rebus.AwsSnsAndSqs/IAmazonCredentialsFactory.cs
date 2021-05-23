namespace Rebus.AwsSnsAndSqs
{
    using Amazon.Runtime;

    public interface IAmazonCredentialsFactory
    {
        AWSCredentials Create();
    }
}
