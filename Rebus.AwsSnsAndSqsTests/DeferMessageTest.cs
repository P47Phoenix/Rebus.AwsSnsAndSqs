﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
    [TestFixture, Category(Category.AmazonSqs)]
    public class DeferMessageTest : SqsFixtureBase
    {
        BuiltinHandlerActivator _activator;
        RebusConfigurer _configurer;

        protected override void SetUp()
        {
            var connectionInfo = AmazonSqsTransportFactory.ConnectionInfo;

            var accessKeyId = connectionInfo.AccessKeyId;
            var secretAccessKey = connectionInfo.SecretAccessKey;
            var amazonSqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = connectionInfo.RegionEndpoint
            };

            var queueName = TestConfig.GetName("defertest");

            AmazonSqsManyMessagesTransportFactory.PurgeQueue(queueName);

            _activator = Using(new BuiltinHandlerActivator());

            _configurer = Configure.With(_activator)
                .Transport(t => t.UseAmazonSQS(accessKeyId, secretAccessKey, amazonSqsConfig, queueName))
                .Options(o => o.LogPipeline());
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