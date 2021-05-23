#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using System;

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
