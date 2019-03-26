namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;
    using System.Collections.Generic;
    using Amazon.SimpleNotificationService.Model;

    public class SnsAttributeMapper<T> : ISnsAttributeMapper
    {
        private readonly Func<T, IDictionary<string, string>, IDictionary<string, MessageAttributeValue>> _map;

        public SnsAttributeMapper(Func<T, IDictionary<string, string>, IDictionary<string, MessageAttributeValue>> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public Type MapperForType => typeof(T);

        public IDictionary<string, MessageAttributeValue> GetAttributes(object value, IDictionary<string, string> headers)
        {
            if (value is T valueOfT)
            {
                return _map(valueOfT, headers);
            }

            throw new ArgumentOutOfRangeException(nameof(value), $"Expected type of {typeof(T).FullName} and was passed in {value?.GetType()?.FullName ?? "a null value"}");
        }
    }
}
