namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    internal class SnsAttributeMapperFactory : ISnsAttributeMapperFactory
    {
        private readonly ReadOnlyDictionary<Type, ISnsAttributeMapper> _mapperLookup;

        public SnsAttributeMapperFactory(List<ISnsAttributeMapper> snsAndSqsAttributeMappers)
        {
            _mapperLookup = new ReadOnlyDictionary<Type, ISnsAttributeMapper>(snsAndSqsAttributeMappers.ToDictionary(k => k.MapperForType, v => v));
        }

        public ISnsAttributeMapper Create(Type messageType)
        {
            return _mapperLookup.ContainsKey(messageType) ? _mapperLookup[messageType] : null;
        }
    }
}
