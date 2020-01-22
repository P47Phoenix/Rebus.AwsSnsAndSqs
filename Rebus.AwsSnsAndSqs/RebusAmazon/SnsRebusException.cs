using System;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    [Serializable]
    public class SnsRebusException : Exception
    {
        public SnsRebusException(string message) : base(message)
        {
        }

        public SnsRebusException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
