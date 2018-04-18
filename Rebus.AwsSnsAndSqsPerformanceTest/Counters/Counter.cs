using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    public class Counter
    {
        protected long _Value;

        public long Value => Interlocked.Read(ref _Value);

        public virtual void Increment()
        {
            Interlocked.Increment(ref this._Value);
        }

        public void Clear()
        {
            Interlocked.Exchange(ref this._Value, 0L);
        }
    }
}
