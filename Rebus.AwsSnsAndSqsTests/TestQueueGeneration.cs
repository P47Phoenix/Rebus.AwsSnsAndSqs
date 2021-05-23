namespace Rebus.AwsSnsAndSqsTests
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using AwsSnsAndSqs;
    using NUnit.Framework;

    [TestFixture]
    public class TestQueueGeneration
    {
        [Test]
        public async Task Run()
        {
            var config = new AmazonSQSConfig {RegionEndpoint = RegionEndpoint.USWest2};

            using (var client = new AmazonSQSClient(new FailbackAmazonCredentialsFactory().Create(), config))
            {
                var queueName = "test1";
                var response = await client.CreateQueueAsync(new CreateQueueRequest(queueName));

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Could not create queue '{queueName}' - got HTTP {response.HttpStatusCode}");
                }
            }
        }
    }
}
