using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    /// <summary>
    ///     Synchronization context that can be "pumped" in order to have it execute continuations posted back to it
    /// </summary>
    internal class AmazonSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _items = new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();
        private readonly Func<Task> _task;
        private readonly AutoResetEvent _workItemsWaiting = new AutoResetEvent(false);

        private ExceptionDispatchInfo _caughtException;

        private bool _done;

        public AmazonSynchronizationContext(Func<Task> task)
        {
            _task = task ?? throw new ArgumentNullException(paramName: nameof(task), message: "Please remember to pass a Task to be executed");
        }

        public override void Post(SendOrPostCallback function, object state)
        {
            _items.Enqueue(item: Tuple.Create(function, state));
            _workItemsWaiting.Set();
        }

        /// <summary>
        ///     Enqueues the function to be executed and executes all resulting continuations until it is completely done
        /// </summary>
        public void Run()
        {
            Post(function: async _ =>
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
                    Post(function: state => _done = true, state: null);
                }
            }, state: null);

            while (!_done)
            {

                if (_items.TryDequeue(result: out Tuple<SendOrPostCallback, object> task))
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
