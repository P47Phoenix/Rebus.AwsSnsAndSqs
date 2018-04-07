using System;

namespace Rebus.AwsSnsAndSqs.Amazon.Extensions
{
    public class AmazonWebServiceException : Exception
    {
        public AmazonWebServiceException(string message) : base(message) { }
        public AmazonWebServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
