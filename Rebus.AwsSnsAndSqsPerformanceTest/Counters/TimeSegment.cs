using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Counters
{
    internal class TimeSegment
    {
        internal TimeSegment(
            long timeSegmentEnd)
        {
            this.TimeSegmentEnd = timeSegmentEnd;
            this.Count = new Counter();
        }
        internal long TimeSegmentEnd { get; private set; }
        internal Counter Count { get; private set; }

        public override string ToString()
        {
            return string.Format("ending at: {0} Count: {1}",
                new DateTime(this.TimeSegmentEnd, DateTimeKind.Utc),
                this.Count.Value);
        }
    }
}
