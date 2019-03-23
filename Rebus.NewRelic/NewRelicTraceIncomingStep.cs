namespace Rebus.NewRelicApi
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using NewRelic.Api.Agent;
    using Pipeline;

    public class NewRelicTraceIncomingStep : IIncomingStep
    {
        [Transaction]
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var messageContext = MessageContext.Current;

            var messagesTypeName = messageContext.Message.Body.GetType().Name;

            const string transactionName = "Messaging";
            NewRelic.SetTransactionName(nameof(transactionName), $"{messagesTypeName}");

            foreach (var messageContextHeader in messageContext.Headers)
            {
                NewRelic.AddCustomParameter(messageContextHeader.Key, messageContextHeader.Value);
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

    public static 
}
