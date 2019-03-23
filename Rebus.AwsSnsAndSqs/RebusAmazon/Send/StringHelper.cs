namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;

    internal static class StringHelper
    {
        public static string GetBody(byte[] bodyBytes)
        {
            return Convert.ToBase64String(bodyBytes);
        }
    }
}