using Rebus.AwsSnsAndSqsPerformanceTest.Counters;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    public class PerformanceTestResult
    {
        public long TotalTestTimeMilliseconds { get; set; }
        internal ReceiveResult MessageReceivedTimes { get; set; }
        internal SendResult MessageSentTimes { get; set; }
    }
}
