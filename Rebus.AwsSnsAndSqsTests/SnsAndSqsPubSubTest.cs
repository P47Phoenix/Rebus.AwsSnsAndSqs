using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.RabbitMq.Tests
{
    using AwsSnsAndSqs.Config;
    using Routing.TypeBased;

    [TestFixture, Category("snsAndSqsPubSub")]
    public class SnsAndSqsPubSubTest : FixtureBase
    {
        readonly string _publisherQueueName = TestConfig.GetName("publisher");
        readonly string _subscriber1QueueName = TestConfig.GetName("sub1");
        readonly string _subscriber2QueueName = TestConfig.GetName("sub2");
        BuiltinHandlerActivator _publisher;

        protected override void SetUp()
        {
            _publisher = GetBus(_publisherQueueName);
        }

        [Test]
        public async Task PubSubTest()
        {
            var sub1GotEvent = new ManualResetEvent(false);
            var sub2GotEvent = new ManualResetEvent(false);

            var sub1 = GetBus(_subscriber1QueueName, async str =>
            {
                if (str == "weehoo!!")
                {
                    sub1GotEvent.Set();
                }
            });

            var sub2 = GetBus(_subscriber2QueueName, async str =>
            {
                if (str == "weehoo!!")
                {
                    sub2GotEvent.Set();
                }
            });

            await sub1.Bus.Subscribe<string>();
            await sub2.Bus.Subscribe<string>();

            await _publisher.Bus.Publish("weehoo!!");

            sub1GotEvent.WaitOrDie(TimeSpan.FromSeconds(30));
            sub2GotEvent.WaitOrDie(TimeSpan.FromSeconds(30));
        }

        BuiltinHandlerActivator GetBus(string queueName, Func<string, Task> handlerMethod = null)
        {
            var activator = Using(new BuiltinHandlerActivator());

            if (handlerMethod != null)
            {
                activator.Handle(handlerMethod);
            }

            Configure.With(activator)
                .Transport(t =>
                {
                    t.UseAmazonSnsAndSqs(workerQueueAddress: queueName);
                })
                .Routing(r => r.TypeBased().Map<string>(queueName))
                .Start();

            return activator;
        }
    }
}