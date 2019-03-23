namespace Rebus.AwsSnsAndSqs.RebusAmazon.Send
{
    using System;
    using System.Threading.Tasks;
    using Injection;
    using Pipeline;

    public class SnsAttributeMapperOutBoundStep : IOutgoingStep
    {
        public const string SnsAttributeKey = nameof(SnsAttributeKey);

        private readonly ISnsAttributeMapperFactory _snsAttributeMapperFactory;

        public SnsAttributeMapperOutBoundStep(IResolutionContext context)
        {
            _snsAttributeMapperFactory = context.Get<ISnsAttributeMapperFactory>();
        }

        /// <summary>
        /// Carries out whichever logic it takes to do something good for the outgoing message :)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var messageContext = MessageContext.Current;

            var body = messageContext.Message.Body;

            var processor = _snsAttributeMapperFactory.Create(body.GetType());

            if (processor != null)
            {
                var attributes = processor.GetAttributes(body);

                if (attributes.Count > 10)
                {
                    throw new InvalidOperationException($"You can only map up to 10 attributes with an sns message. The number of attributes mapped is {attributes.Count} and the keys are {string.Join(", ", attributes.Keys)}");
                }

                context.Save(SnsAttributeKey, attributes);
            }

            await next();
        }
    }
}
