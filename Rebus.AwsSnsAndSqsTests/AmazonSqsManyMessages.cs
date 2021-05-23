namespace Rebus.AwsSnsAndSqsTests
{
    using NUnit.Framework;
    using Tests.Contracts.Transports;

    [TestFixture]
    [Category(Category.AmazonSqs)]
    public class AmazonSqsManyMessages : TestManyMessages<AmazonSqsManyMessagesTransportFactory>
    {
    }
}
