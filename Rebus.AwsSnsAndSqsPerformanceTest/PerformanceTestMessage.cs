namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    public class PerformanceTestMessage
    {
        public string Message { get; set; }
        public long UnixTimeMilliseconds { get; set; }
        public int Number { get; set; }
    }
}
