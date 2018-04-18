using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Counters
{
    public interface IDateTimeWrapper
    {
        DateTime UtcDateTime { get; }
    }
}
