using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rebus.AwsSnsAndSqs
{
    public class ConventionBasedTopicFormatter : ITopicFormatter
    {
        private static readonly ConcurrentDictionary<string, string> m_topicNameCache = new ConcurrentDictionary<string, string>();

        public string FormatTopic(string topic)
        {
            return m_topicNameCache.GetOrAdd(topic, topicKey =>
            {
                var newWord = new List<char>(topicKey.Length);
                var lastLetter = new char();

                foreach (var c in topicKey)
                {
                    if (char.IsDigit(c) || char.IsLetter(c) || c == '_' || c == '-')
                    {
                        newWord.Add(c);
                    }
                    else if (c == '.')
                    {
                        if (lastLetter == '_')
                        {
                            continue;
                        }

                        newWord.Add('_');
                    }
                    else
                    {
                        if (lastLetter == '-')
                        {
                            continue;
                        }

                        newWord.Add('-');
                    }

                    lastLetter = c;
                }

                var topicNameFinal = new string(newWord.ToArray());

                if (topicNameFinal.Length > 256)
                {
                    throw new ArgumentOutOfRangeException(nameof(topic), $"The topic {topicNameFinal} is to long. If you want to keep the namespace as the topic make it shorter.");
                }

                return topicNameFinal;
            });
        }
    }
}
