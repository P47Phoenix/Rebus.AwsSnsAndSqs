using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public class AmazonTransportMessage
    {
        public AmazonTransportMessage() : this(null, null)
        {
        }

        public AmazonTransportMessage(Dictionary<string, string> headers, string body)
        {
            Headers = headers ?? new Dictionary<string, string>();
            Body = body ?? string.Empty;
        }

        [JsonProperty(PropertyName = "headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty(PropertyName = "body")]
        public string Body { get; set; }
    }
}
