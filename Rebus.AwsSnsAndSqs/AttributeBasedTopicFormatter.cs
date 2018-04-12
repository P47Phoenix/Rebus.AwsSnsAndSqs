﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Rebus.AwsSnsAndSqs
{
#if NET45
    public class AttributeBasedTopicFormatter : ITopicFormatter
    {
        private ConcurrentDictionary<string, string> _TopicCache = new ConcurrentDictionary<string, string>();

        public string FormatTopic(string topic)
        {

            try
            {
                return _TopicCache.GetOrAdd(topic, s =>
                {
                    var typeStringArray = topic
                        .Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r?.Trim())
                        .ToArray();

                    if (typeStringArray.Length != 2)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"The topic received from rebus was {topic} which pasrsed to {JObject.FromObject(new { TypeDetail = typeStringArray })}");
                    }
                    var assembly = Assembly.Load(typeStringArray[1]);

                    var type = assembly.GetType(typeStringArray[0], false);

                    if (type == null)
                    {
                        throw new InvalidOperationException(
                        $"Unable to find type {typeStringArray[0]} in assembly {assembly.FullName}.");
                    }

                    var attribute = type.GetCustomAttribute<TopicContractAttribute>();

                    if (attribute == null)
                    {
                        throw new InvalidOperationException(
                        $"You must set a {nameof(TopicContractAttribute)} when using {nameof(AttributeBasedTopicFormatter)}");
                    }
                    return attribute.Topic;
                });
            }
            catch (Exception innerException)
            {
                throw new ArgumentException(
                    $"Unable to get topic name for topic {topic}. See inner error for details.", nameof(topic),
                    innerException);
            }
        }
    }
#endif
}
