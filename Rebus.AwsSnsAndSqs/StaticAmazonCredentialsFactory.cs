﻿namespace Rebus.AwsSnsAndSqs
{
    using global::Amazon.Runtime;

    public class StaticAmazonCredentialsFactory : IAmazonCredentialsFactory
    {
        private readonly AWSCredentials m_awsCredentials;

        public StaticAmazonCredentialsFactory(AWSCredentials awsCredentials)
        {
            m_awsCredentials = awsCredentials;
        }

        public AWSCredentials Create()
        {
            return m_awsCredentials;
        }
    }
}
