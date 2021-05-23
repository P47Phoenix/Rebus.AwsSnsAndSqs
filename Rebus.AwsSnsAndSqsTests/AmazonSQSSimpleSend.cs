namespace Rebus.AwsSnsAndSqsTests
{
    using NUnit.Framework;
    using Tests.Contracts.Transports;

    [TestFixture]
    [Category(Category.AmazonSqs)]
    internal class AmazonSqsSimpleSend : BasicSendReceive<AmazonSqsTransportFactory>
    {
    }
}
