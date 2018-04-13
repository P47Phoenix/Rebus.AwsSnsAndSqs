using System;
using System.Diagnostics;
using System.Linq;

namespace Rebus.AwsSnsAndSqs
{

#if NET45
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TopicNameAttribute : Attribute
    {
        public TopicNameAttribute(string topic)
        {
            if(topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            var invalidLength = topic.Length > 256 || topic.Length <= 0;

            var hasInvalidChar = topic.Count(c => (char.IsLetterOrDigit(c) || c == '_' || c == '-') == false) != 0;

            Trace.WriteLineIf(invalidLength , nameof(invalidLength));
            Trace.WriteLineIf(hasInvalidChar , nameof(hasInvalidChar));

            if (invalidLength || hasInvalidChar)
            {
                throw new ArgumentException($"Topic names must be made up of only uppercase and lowercase ASCII letters, numbers, underscores, and hyphens, and must be between 1 and 256. {nameof(invalidLength)}:{invalidLength}, {nameof(hasInvalidChar)}:{hasInvalidChar}", nameof(topic));
            }
            Topic = topic;
        }

        public string Topic { get; }
    }
#endif
}
