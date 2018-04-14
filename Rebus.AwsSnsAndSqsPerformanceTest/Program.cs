using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
#if DEBUG
                PerformanceTest pt = new PerformanceTest();

                pt.Setup();
                pt.SendAndRecieve(100, 4);
                pt.TearDown();
#else
                var result = BenchmarkRunner.Run<PerformanceTest>();
#endif
            }
            finally
            {
                Console.WriteLine("Complete. Press enter to close.");
                Console.ReadLine();
            }
        }
    }

    public class PerformanceTest
    {
        private IBus _bus;
        private BuiltinHandlerActivator _builtinHandlerActivator;

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        public class PerformanceTestMessage
        {
            public string Message { get; set; }
        }

        private long _messageCounter;
        private long _messagesSent;
        private long _waitingFor;

        [GlobalSetup]
        public void Setup()
        {
            _messageCounter = 0;

            _builtinHandlerActivator = new BuiltinHandlerActivator();

            _builtinHandlerActivator.Handle<PerformanceTestMessage>(message =>
            {
                var val = Interlocked.Increment(ref _messageCounter);

                if (val >= Interlocked.Read(ref _waitingFor))
                {
                    _autoResetEvent.Set();
                }

                return Task.CompletedTask;

            });

            var queueName = nameof(SendAndRecieve);

            _bus = Configure
                .With(_builtinHandlerActivator)
                .Logging(configurer => configurer.Trace())
                .Transport(t =>
                {
                    // set the worker queue name
                    t.UseAmazonSnsAndSqs(workerQueueAddress: queueName);
                    //t.UseAmazonSnsAndSqsAsOneWayClient();
                })
                .Routing(r =>
                {
                    // Map the message type to the queue
                    r.TypeBased().Map<PerformanceTestMessage>(queueName);
                })
                .Start();

            var task = _bus.Subscribe<PerformanceTestMessage>();
            AsyncHelpers.RunSync(() => task);

        }

        [GlobalCleanup]
        public void TearDown()
        {
            _bus.Dispose();

            _builtinHandlerActivator.Dispose();
        }

        [Benchmark]
        [Arguments(10, 4)]
        [Arguments(10, 8)]
        [Arguments(10, 16)]
        [Arguments(10, 32)]
        [Arguments(10, 64)]
        [Arguments(10, 128)]
        [Arguments(100, 4)]
        [Arguments(100, 8)]
        [Arguments(100, 16)]
        [Arguments(100, 32)]
        [Arguments(100, 64)]
        [Arguments(100, 128)]
        [Arguments(1000, 4)]
        [Arguments(1000, 8)]
        [Arguments(1000, 16)]
        [Arguments(1000, 32)]
        [Arguments(1000, 64)]
        [Arguments(1000, 128)]
        [Arguments(10000, 4)]
        [Arguments(10000, 8)]
        [Arguments(10000, 16)]
        [Arguments(10000, 32)]
        [Arguments(10000, 64)]
        [Arguments(10000, 128)]
        public void SendAndRecieve(long numberOfMessages, int messageSizeKilobytes)
        {
            Interlocked.Exchange(ref _waitingFor, numberOfMessages);
            Interlocked.Exchange(ref _messageCounter, 0);
            Interlocked.Exchange(ref _messagesSent, 0);

            for (int i = 0; i < numberOfMessages; i++)
            {
                var task = _bus.Publish(new PerformanceTestMessage
                {
                    Message = new string('1', messageSizeKilobytes * 1024)
                });

                AsyncHelpers.RunSync(() => task);
                Interlocked.Increment(ref _messagesSent);
            }

            while (_autoResetEvent.WaitOne(3000) == false)
            {
                Console.WriteLine($"Recieved:{Interlocked.Read(ref _messageCounter)} Sent:{Interlocked.Read(ref _messagesSent)}");
            }

            Interlocked.Exchange(ref _messageCounter, 0);
        }
    }
}
