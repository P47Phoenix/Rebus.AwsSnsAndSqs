using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    /// <summary>
    /// Synchronization context that can be "pumped" in order to have it execute continuations posted back to it
    /// </summary>
    internal class AmazonSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _items = new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();
        private readonly AutoResetEvent _workItemsWaiting = new AutoResetEvent(false);
        private readonly Func<Task> _task;

        private ExceptionDispatchInfo _caughtException;

        private bool _done;

        public AmazonSynchronizationContext(Func<Task> task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task), "Please remember to pass a Task to be executed");
        }

        public override void Post(SendOrPostCallback function, object state)
        {
            _items.Enqueue(Tuple.Create(function, state));
            _workItemsWaiting.Set();
        }

        /// <summary>
        /// Enqueues the function to be executed and executes all resulting continuations until it is completely done
        /// </summary>
        public void Run()
        {
            Post(async _ =>
            {
                try
                {
                    await _task();
                }
                catch (Exception exception)
                {
                    _caughtException = ExceptionDispatchInfo.Capture(exception);
                    throw;
                }
                finally
                {
                    Post(state => _done = true, null);
                }
            }, null);

            while (!_done)
            {
                Tuple<SendOrPostCallback, object> task;

                if (_items.TryDequeue(out task))
                {
                    task.Item1(task.Item2);

                    if (_caughtException == null)
                    {
                        continue;
                    }

                    _caughtException.Throw();
                }
                else
                {
                    _workItemsWaiting.WaitOne();
                }
            }
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Cannot send to same thread");
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}