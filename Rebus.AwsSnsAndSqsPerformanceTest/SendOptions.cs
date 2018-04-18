namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    using System;

    internal class SendOptions
    {
        public int MessageSizeKilobytes { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int MaxMessagesPerTask { get; set; }
        public TimeSpan HowLongToSend { get; set; } = TimeSpan.FromMinutes(5);
    }
}