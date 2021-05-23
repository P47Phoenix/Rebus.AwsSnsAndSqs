namespace Rebus.AwsSnsAndSqsTests.Bugs
{
    using System;
    using System.Threading.Tasks;
    using AwsSnsAndSqs.RebusAmazon;
    using Extensions;
    using NUnit.Framework;
    using Tests.Contracts.Extensions;
    using Transport;

    [TestFixture]
    public class CanSendMoreThan10MessagesInABatch : SqsFixtureBase
    {
        private AmazonSqsTransport _transport;
        private string _inputQueueAddress;

        protected override void SetUp()
        {
            _inputQueueAddress = $"queue-{DateTime.Now:yyyyMMdd-HHmmss}";
            _transport = AmazonSqsTransportFactory.CreateTransport(_inputQueueAddress, TimeSpan.FromMinutes(1));
        }

        [TestCase(15)]
        public async Task ItWorks(int messageCount)
        {
            using (var scope = new RebusTransactionScope())
            {
                var context = scope.TransactionContext;

                messageCount.Times(() => _transport.Send(_inputQueueAddress, MessageWith("message-1"), context).Wait());

                await scope.CompleteAsync();
            }

            var receivedMessages = await _transport.ReceiveAll();

            Assert.That(receivedMessages.Count, Is.EqualTo(messageCount));
        }
    }
}
