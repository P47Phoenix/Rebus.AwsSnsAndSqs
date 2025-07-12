using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Timeouts;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
    using Time;

    [TestFixture]
    public class AlternativeTimeoutManager : FixtureBase
    {
        private const string QueueName = "alt-timeout-man-1";
        private const string TimeoutManagerQueueName = "alt-timeout-man-2";

        private BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            AmazonSqsManyMessagesTransportFactory.PurgeQueue(QueueName);

            _activator = new BuiltinHandlerActivator();

            Using(_activator);
        }

        [Test]
        public async Task CanUseAlternativeTimeoutManager()
        {
            var gotTheString = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                Console.WriteLine($"Received string: '{str}'");
                gotTheString.Set();
            });

            var bus = Configure
                .With(_activator)
                .Transport(t => 
                    t.UseAmazonSnsAndSqs(
                        workerQueueAddress: QueueName, 
                        amazonSnsAndSqsTransportOptions: new AmazonSnsAndSqsTransportOptions {UseNativeDeferredMessages = false}))
                .Timeouts(t => 
                    t.Register(c => new InMemoryTimeoutManager(new DefaultRebusTime()))).Start();

            await bus.DeferLocal(TimeSpan.FromSeconds(5), "hej med dig min ven!!!!!");

            gotTheString.WaitOrDie(TimeSpan.FromSeconds(10), "Did not receive the string withing 10 s timeout");
        }

        [Test]
        public async Task CanUseDedicatedAlternativeTimeoutManager()
        {
            // start the timeout manager
            Configure.With(Using(new BuiltinHandlerActivator())).Transport(t => t.UseAmazonSnsAndSqs(workerQueueAddress: TimeoutManagerQueueName, amazonSnsAndSqsTransportOptions: new AmazonSnsAndSqsTransportOptions {UseNativeDeferredMessages = false})).Timeouts(t => t.Register(c => new InMemoryTimeoutManager(new DefaultRebusTime()))).Start();

            var gotTheString = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                Console.WriteLine($"Received string: '{str}'");

                gotTheString.Set();
            });

            var bus = Configure.With(_activator).Transport(t => t.UseAmazonSnsAndSqs(workerQueueAddress: QueueName, amazonSnsAndSqsTransportOptions: new AmazonSnsAndSqsTransportOptions {UseNativeDeferredMessages = false})).Timeouts(t => t.UseExternalTimeoutManager(TimeoutManagerQueueName)).Start();

            await bus.DeferLocal(TimeSpan.FromSeconds(5), "hej med dig min ven!!!!!");

            gotTheString.WaitOrDie(TimeSpan.FromSeconds(10), "Did not receive the string withing 10 s timeout");
        }
    }
}
