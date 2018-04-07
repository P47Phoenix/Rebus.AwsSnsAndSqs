using System;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqs.Amazon
{
    public class SnsRebusExption : Exception
    {
        public SnsRebusExption(string message) : base(message) { }
        public SnsRebusExption(string message, Exception innerException) : base(message, innerException) { }
    }
}
