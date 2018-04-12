using Rebus.AwsSnsAndSqs;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
#if NET45

    [TopicContract(nameof(SomeMessageTopic))]
    public class SomeMessageTopic
    {
        public string Message { get; set; }
    }
    [TopicContract("&^76")]
    public class BadTopic
    {
        public string Message { get; set; }
    }

    [TopicContract("1234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678")]
    public class ToLongTopic
    {
        public string Message { get; set; }
    }
#endif
}