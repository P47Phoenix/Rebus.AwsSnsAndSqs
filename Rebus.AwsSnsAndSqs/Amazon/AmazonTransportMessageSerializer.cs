﻿using Newtonsoft.Json;

namespace Rebus.AwsSnsAndSqs.Amazon
{
    public class AmazonTransportMessageSerializer
    {
        public string Serialize(AmazonTransportMessage message) => JsonConvert.SerializeObject(message);

        public AmazonTransportMessage Deserialize(string value) => value == null ? null : JsonConvert.DeserializeObject<AmazonTransportMessage>(value);
    }
}
