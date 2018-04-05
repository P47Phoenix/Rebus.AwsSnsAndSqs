using Newtonsoft.Json;

namespace Rebus.AwsSnsAndSqs.AmazonSQS
{
    class AmazonSQSTransportMessageSerializer
    {
        public string Serialize(AmazonSQSTransportMessage message) => JsonConvert.SerializeObject(message);

        public AmazonSQSTransportMessage Deserialize(string value) => value == null ? null : JsonConvert.DeserializeObject<AmazonSQSTransportMessage>(value);
    }
}
