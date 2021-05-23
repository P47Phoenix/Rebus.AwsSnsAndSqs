namespace Rebus.AwsSnsAndSqsTests
{
    using NUnit.Framework;
    using Tests.Contracts.Transports;

    [TestFixture]
    [Category(Category.AmazonSqs)]
    internal class AmazonSqsMessageExpiration : MessageExpiration<AmazonSqsTransportFactory>
    {
    }
}
