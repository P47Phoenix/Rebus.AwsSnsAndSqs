using System;
using System.Diagnostics;
using System.Linq;

namespace Rebus.AwsSnsAndSqs
{
#if NET45 || NETSTANDARD2_0
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TopicNameAttribute : Attribute
    {
        public TopicNameAttribute(string topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            var invalidLength = topic.Length > 256 || topic.Length <= 0;

            var hasInvalidChar = topic.Any(c => (char.IsLetterOrDigit(c) || c == '_' || c == '-') == false);

            Trace.WriteLineIf(invalidLength, nameof(invalidLength));
            Trace.WriteLineIf(hasInvalidChar, nameof(hasInvalidChar));

            if (invalidLength || hasInvalidChar)
            {
                throw new ArgumentException(message: $"Topic names must be made up of only uppercase and lowercase ASCII letters, numbers, underscores, and hyphens, and must be between 1 and 256. {nameof(invalidLength)}:{invalidLength}, {nameof(hasInvalidChar)}:{hasInvalidChar}", paramName: nameof(topic));
            }

            Topic = topic;
        }

        public string Topic { get; }
    }
#endif
}
