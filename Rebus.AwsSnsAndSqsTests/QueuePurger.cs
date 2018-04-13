using System;

namespace Rebus.AwsSnsAndSqsTests
{
    /// <summary>
    ///     Purges the queue when it is disposed
    /// </summary>
    internal class QueuePurger : IDisposable
    {
        private readonly string _queueName;

        public QueuePurger(string queueName)
        {
            _queueName = queueName;
        }

        public void Dispose()
        {
            AmazonSqsTransportFactory.CreateTransport(_queueName, TimeSpan.FromMinutes(5)).Purge();
        }
    }
}
