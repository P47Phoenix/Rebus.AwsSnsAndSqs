using System;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Counters
{
    using System.Collections.Concurrent;

    public class TimeSpanCounter
	{
		private readonly long _Span;

		private readonly long _SpanSegment;

		private readonly ConcurrentQueue<TimeSegment> _TimeSegments = new ConcurrentQueue<TimeSegment>();

		private TimeSegment _CurrentSegment = null;

		private long _CurrentCount = 0;

		private readonly IDateTimeWrapper _dateTimeWrapper;
        
		public TimeSpanCounter(TimeSpan span, IDateTimeWrapper datetime = null)
		{
		    datetime = datetime ?? new DateTimeWrapper();
			if (span.TotalSeconds <= 0)
			{
				throw new ArgumentOutOfRangeException("span", "Timespan must be greated then a minute.");
			}

			this._Span = span.Ticks;
			/*
			 * We only want 600 segments of time for a given interval
			 * the more segments the more precise the counter
			 * Also more segments is more memory being used
			 * 600 will use a little more then 9.6 kilobytes per counter
			 */
			this._SpanSegment = this._Span / 600;

			this._dateTimeWrapper = datetime;

			var now = _dateTimeWrapper.UtcDateTime.Ticks;

			_CurrentSegment = new TimeSegment(long.MinValue);
			_TimeSegments.Enqueue(_CurrentSegment);
		}

		public void Increment()
		{
			var now = _dateTimeWrapper.UtcDateTime.Ticks;

			SegmentCheckAndIncrement(now);

			var then = now - this._Span;

			RemoveOldTimes(then);


		}

		private void SegmentCheckAndIncrement(long now)
		{
			var updateCurrent = false;
			lock (this)
			{
				// If we do not have a current segment of the segment we have is old.
				// Create a new segment and increment.
				if (_CurrentSegment.TimeSegmentEnd < now)
				{
					AddnewSegment(now);
				}
				else
				{
					updateCurrent = true;
				}
			}

			if (updateCurrent)
			{
				this.IncrementCount();
			}
		}

		private void AddnewSegment(long now)
		{
			_CurrentSegment = new TimeSegment(now + this._SpanSegment);
			_TimeSegments.Enqueue(_CurrentSegment);

			IncrementCount();

		}

		private void IncrementCount()
		{
			if (_CurrentSegment == null)
			{
				throw new InvalidOperationException("Current segment has not been created");
			}
			_CurrentSegment.Count.Increment();
			System.Threading.Interlocked.Increment(ref this._CurrentCount);
		}

		public TimeSpan Span
		{
			get
			{
				return new TimeSpan(this._Span);
			}
		}

		public long CurrentPeriodCount
		{
			get
			{
				var now = _dateTimeWrapper.UtcDateTime.Ticks;
				var then = now - this._Span;
				RemoveOldTimes(then);
				return System.Threading.Interlocked.Read(ref this._CurrentCount);
			}
		}

		private void RemoveOldTimes(long removeTimesBeforeThisTime)
		{
			bool gotTime;
			do
			{
				if (_TimeSegments.Count == 0)
					return;

				TimeSegment time;
				gotTime = _TimeSegments.TryPeek(out time);

				if (gotTime != true)
				{
					continue;
				}

				if (time.TimeSegmentEnd >= removeTimesBeforeThisTime)
				{
					continue;
				}

				TimeSegment timeToRemove;
				if (!_TimeSegments.TryDequeue(out timeToRemove))
				{
					gotTime = false;
					continue;
				}

				var countTodecrement = timeToRemove.Count.Value;

				System.Threading.Interlocked.Add(ref this._CurrentCount, -countTodecrement);
				gotTime = false;
			}
			while (gotTime == false);
		}
	}
}
