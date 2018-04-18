using Rebus.AwsSnsAndSqs;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
#if NET45 || NETSTANDARD2_0
    [TopicName("&^76")]
    public class BadTopic
    {
        public string Message { get; set; }
    }
#endif
}
