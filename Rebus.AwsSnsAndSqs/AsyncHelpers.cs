using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.AwsSnsAndSqs.RebusAmazon;

namespace Rebus.AwsSnsAndSqs
{
    internal static class AsyncHelpers
    {
        /// <summary>
        ///     Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues
        ///     continuations
        /// </summary>
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
