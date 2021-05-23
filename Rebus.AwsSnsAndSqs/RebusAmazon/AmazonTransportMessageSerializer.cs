namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    using Newtonsoft.Json;

    public class AmazonTransportMessageSerializer
    {
        public string Serialize(AmazonTransportMessage message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public AmazonTransportMessage Deserialize(string value)
        {
            return value == null ? null : JsonConvert.DeserializeObject<AmazonTransportMessage>(value);
        }
    }
}
