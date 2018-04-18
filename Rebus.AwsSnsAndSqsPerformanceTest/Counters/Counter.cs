namespace Rebus.AwsSnsAndSqsPerformanceTest.Counters
{
    using System.Threading;

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
