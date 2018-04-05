using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    static class AmazonAsyncHelpers
    {
        /// <summary>
        /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
        ///  </summary>
        public static void RunSync(Func<Task> task)
        {
            var currentContext = SynchronizationContext.Current;
            var customContext = new AmazonSynchronizationContext(task);

            try
            {
                SynchronizationContext.SetSynchronizationContext(customContext);

                customContext.Run();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(currentContext);
            }
        }
    }
}