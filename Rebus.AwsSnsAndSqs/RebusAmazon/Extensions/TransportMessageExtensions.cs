using System;
using Rebus.Messages;
using Rebus.Time;
using Message = Amazon.SQS.Model.Message;

namespace Rebus.AwsSnsAndSqs.RebusAmazon.Extensions
{
    using System.Globalization;

    internal static class TransportMessageExtensions
    {
        public static bool MessageIsExpired(this TransportMessage message, Message sqsMessage)
        {
            if (message.Headers.TryGetValue(Headers.TimeToBeReceived, out var value) == false)
            {
                return false;
            }

            var timeToBeReceived = TimeSpan.Parse(value, CultureInfo.InvariantCulture);

            return MessageIsExpiredUsingRebusSentTime(message, timeToBeReceived) || MessageIsExpiredUsingNativeSqsSentTimestamp(sqsMessage, timeToBeReceived);
        }

        public static bool MessageIsExpiredUsingRebusSentTime(this TransportMessage message, TimeSpan timeToBeReceived)
        {
            if (message.Headers.TryGetValue(Headers.SentTime, out var rebusUtcTimeSentAttributeValue) == false)
            {
                return false;
            }

            var rebusUtcTimeSent = DateTimeOffset.ParseExact(rebusUtcTimeSentAttributeValue, "O", null);

            return RebusTime.Now.UtcDateTime - rebusUtcTimeSent > timeToBeReceived;
        }

        private static bool MessageIsExpiredUsingNativeSqsSentTimestamp(Message message, TimeSpan timeToBeReceived)
        {
            if (message.Attributes.TryGetValue("SentTimestamp", out var sentTimeStampString) == false)
            {
                return false;
            }

            var sentTime = GetTimeFromUnixTimestamp(sentTimeStampString);
            return RebusTime.Now.UtcDateTime - sentTime > timeToBeReceived;
        }

        private static DateTime GetTimeFromUnixTimestamp(string sentTimeStampString)
        {
            var unixTime = long.Parse(sentTimeStampString, CultureInfo.InvariantCulture);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var sentTime = epoch.AddMilliseconds(unixTime);
            return sentTime;
        }
    }
}
