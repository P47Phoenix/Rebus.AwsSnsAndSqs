namespace Rebus.AwsSnsAndSqs.RebusAmazon.Extensions
{
    using System;

    [Serializable]
    public class AmazonWebServiceException : Exception
    {
        public AmazonWebServiceException(string message) : base(message)
        {
        }

        public AmazonWebServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
