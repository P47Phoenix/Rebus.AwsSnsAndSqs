using System;
using System.Diagnostics;
using System.Linq;

namespace Rebus.AwsSnsAndSqs
{

#if NET45
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TopicContractAttribute : Attribute
    {
        public TopicContractAttribute(string topic)
        {
            
            bool invalidLength = topic.Length > 256 && topic.Length <= 0;

            bool hasBadChar = topic.Count(c => (char.IsLetterOrDigit(c) || c == '_' || c == '-') == false) != 0;

            Debug.WriteLine($"{nameof(invalidLength)}:{invalidLength}");
            Debug.WriteLine($"{nameof(hasBadChar)}:{hasBadChar}");

            if (invalidLength || hasBadChar)
            {
                throw new ArgumentException($"Topic names must be made up of only uppercase and lowercase ASCII letters, numbers, underscores, and hyphens, and must be between 1 and 256. {nameof(invalidLength)}:{invalidLength}, {nameof(hasBadChar)}:{hasBadChar}", nameof(topic));
            }
            Topic = topic;
        }

        public string Topic { get; }
    }
#endif
}
