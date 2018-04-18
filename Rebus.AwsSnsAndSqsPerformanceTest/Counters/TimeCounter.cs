namespace Rebus.AwsSnsAndSqsPerformanceTest.Counters
{
    using System.Threading;

    public class TimeCounter
    {
        private long _MaxValue = long.MinValue;
        private long _MinValue = long.MaxValue;
        private readonly Counter _Counter = new Counter();
        private long _TotalValue = 0;

        public long MaxValue => Interlocked.Read(ref _MaxValue);

        public long MinValue => Interlocked.Read(ref _MinValue);

        public long TolalValue => Interlocked.Read(ref _TotalValue);

        public long Count => _Counter.Value;

        public void AddTime(long timeTaken)
        {
            this._Counter.Increment();
            Interlocked.Add(ref _TotalValue, timeTaken);
            if (Interlocked.Read(ref _MinValue) > timeTaken)
                Interlocked.Exchange(ref _MinValue, timeTaken);
            if (Interlocked.Read(ref _MaxValue) >= timeTaken)
                return;
            Interlocked.Exchange(ref _MaxValue, timeTaken);
        }

        public void Clear()
        {
            this._Counter.Clear();
            Interlocked.Exchange(ref _TotalValue, 0);
            Interlocked.Exchange(ref _MaxValue, long.MinValue);
            Interlocked.Exchange(ref _MinValue, long.MaxValue);
        }
    }
}
