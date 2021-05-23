#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Activation;
    using Amazon.SQS;
    using AwsSnsAndSqs.Config;
    using Config;
    using NUnit.Framework;
    using Tests.Contracts;
    using Tests.Contracts.Extensions;

    [TestFixture]
    [Category(Category.AmazonSqs)]
    public class DeferMessageTest : SqsFixtureBase
    {
        private BuiltinHandlerActivator _activator;
        private RebusConfigurer _configurer;

        protected override void SetUp()
        {
            var connectionInfo = AmazonSqsTransportFactory.ConnectionInfo;

            var accessKeyId = connectionInfo.AccessKeyId;
            var secretAccessKey = connectionInfo.SecretAccessKey;
            var amazonSqsConfig = new AmazonSQSConfig {RegionEndpoint = connectionInfo.RegionEndpoint};

            var queueName = TestConfig.GetName("defertest");

            AmazonSqsManyMessagesTransportFactory.PurgeQueue(queueName);

            _activator = Using(new BuiltinHandlerActivator());

            _configurer = Configure.With(_activator).Transport(t => t.UseAmazonSnsAndSqs(amazonSqsConfig: amazonSqsConfig, workerQueueAddress: queueName)).Options(o => o.LogPipeline());
        }

        [Test]
        public async Task CanDeferMessage()
        {
            var gotTheMessage = new ManualResetEvent(false);

            var receiveTime = DateTime.MaxValue;

            _activator.Handle<string>(async str =>
            {
                receiveTime = DateTime.UtcNow;
                gotTheMessage.Set();
            });

            var bus = _configurer.Start();
            var sendTime = DateTime.UtcNow;

            await bus.DeferLocal(TimeSpan.FromSeconds(10), "hej med dig!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(20));

            var elapsed = receiveTime - sendTime;

            Assert.That(elapsed, Is.GreaterThan(TimeSpan.FromSeconds(8)));
        }
    }
}
