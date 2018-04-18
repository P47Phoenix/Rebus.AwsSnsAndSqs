using System;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Counters
{
    public class DateTimeWrapper : IDateTimeWrapper
    {
        public DateTime UtcDateTime
        {
            get { return DateTime.UtcNow; }
        }
    }
}
