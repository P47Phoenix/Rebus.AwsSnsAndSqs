﻿#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
#if NET45 || NETSTANDARD2_0
    [TopicName("12345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456782")]
    public class ToLongTopic
    {
        public string Message { get; set; }
    }
#endif
}
