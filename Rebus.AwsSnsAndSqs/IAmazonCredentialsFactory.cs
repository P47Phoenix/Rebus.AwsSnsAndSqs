using Amazon.Runtime;

namespace Rebus.AwsSnsAndSqs
{
    public interface IAmazonCredentialsFactory
    {
        AWSCredentials Create();
    }
}
