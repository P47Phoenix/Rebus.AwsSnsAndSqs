using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AwsSnsAndSqsTests
{
    [TestFixture]
    [Category(Category.AmazonSqs)]
    internal class AmazonSqsMessageExpiration : MessageExpiration<AmazonSqsTransportFactory>
    {
    }
}
