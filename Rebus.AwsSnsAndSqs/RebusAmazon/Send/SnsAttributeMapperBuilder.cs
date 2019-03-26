namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;
    using System.Collections.Generic;
    using Amazon.SimpleNotificationService.Model;

    public class SnsAttributeMapperBuilder
    {
        private List<ISnsAttributeMapper> _snsAttributeMappers = new List<ISnsAttributeMapper>();

        public void AddMap<T>(Func<T, IDictionary<string, string>, IDictionary<string, MessageAttributeValue>> func)
        {
            func = func ?? throw new ArgumentNullException(nameof(func));
            _snsAttributeMappers.Add(new SnsAttributeMapper<T>(func));
        }

        internal List<ISnsAttributeMapper> GetSnsAttributeMaping() => _snsAttributeMappers;
    }
}
