namespace Rebus.NewRelicApi
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Config;
    using NewRelic.Api.Agent;
    using Pipeline;
    using Pipeline.Receive;

    public class NewRelicTraceIncomingStep : IIncomingStep
    {
        [Transaction]
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var messageContext = MessageContext.Current;

            var messagesTypeName = messageContext.Message.Body.GetType().Name;

            const string transactionName = nameof(transactionName);
            NewRelic.SetTransactionName(transactionName, $"{messagesTypeName}");

            foreach (var messageContextHeader in messageContext.Headers)
            {
                NewRelic
                    .GetAgent()?
                    .CurrentTransaction?
                    .AddCustomAttribute(messageContextHeader.Key, messageContextHeader.Value);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                await next();
            }
            catch (Exception error)
            {
                NewRelic.NoticeError(error, messageContext.Headers);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                NewRelic.RecordResponseTimeMetric($"{messagesTypeName}",
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
