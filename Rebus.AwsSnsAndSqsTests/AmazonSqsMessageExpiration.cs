using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AwsSnsAndSqsTests
{
    [TestFixture, Category(Category.AmazonSqs)]
    public class AmazonSqsMessageExpiration : MessageExpiration<AmazonSqsTransportFactory> { }
}