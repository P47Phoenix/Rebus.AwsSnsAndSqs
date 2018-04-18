using System;
using Rebus.AwsSnsAndSqsPerformanceTest.Counters;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    internal class SendResult
    {
        public Counter MessagesSentCounter { get; internal set; }
        public TimeSpan MessageSentTimePerTimePeriod { get; internal set; }
        public SendOptions SendOptions { get; internal set; }
    }
}