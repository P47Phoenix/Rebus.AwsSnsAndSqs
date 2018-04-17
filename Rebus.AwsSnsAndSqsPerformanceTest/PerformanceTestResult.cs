namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    public class PerformanceTestResult
    {
        public TimeCounter MessageSentTimes { get; set; }

        public TimeCounter MessageRecivedTimes { get; set; }

        public long TotalTestTimeMilliseconds { get; set; }
    }
}
