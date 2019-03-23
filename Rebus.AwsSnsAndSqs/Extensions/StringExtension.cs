using System;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal static class StringExtension
    {
        public static byte[] GetBodyBytes(this string bodyText)
        {
            return Convert.FromBase64String(bodyText);
        }
    }

}
