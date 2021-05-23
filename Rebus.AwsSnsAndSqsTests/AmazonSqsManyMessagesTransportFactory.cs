namespace Rebus.AwsSnsAndSqsTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Activation;
    using Amazon.SQS;
    using AwsSnsAndSqs.Config;
    using AwsSnsAndSqs.RebusAmazon;
    using Bus;
    using Config;
    using Logging;
    using Tests.Contracts.Transports;
    using Threading.TaskParallelLibrary;

    public class AmazonSqsManyMessagesTransportFactory : IBusFactory
    {
        private readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            PurgeQueue(inputQueueAddress);

            var bus = Configure.With(builtinHandlerActivator).Transport(t =>
            {
                var info = AmazonSqsTransportFactory.ConnectionInfo;

                var amazonSqsConfig = new AmazonSQSConfig {RegionEndpoint = info.RegionEndpoint};

                t.UseAmazonSnsAndSqs(amazonSqsConfig: amazonSqsConfig, workerQueueAddress: inputQueueAddress);
            }).Options(o =>
            {
                o.SetNumberOfWorkers(10);
                o.SetMaxParallelism(10);
            }).Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }

        public static void PurgeQueue(string queueName)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);

            var connectionInfo = AmazonSqsTransportFactory.ConnectionInfo;
            var amazonSqsConfig = new AmazonSQSConfig
            {
                ServiceURL = SnsHttpLocalhost,
                RegionEndpoint = connectionInfo.RegionEndpoint
            };

            var transport = new AmazonSqsTransport(
                new AmazonInternalSettings(
                    consoleLoggerFactory, 
                    new TplAsyncTaskFactory(consoleLoggerFactory))
                {
                    InputQueueAddress = queueName, 
                    AmazonSqsConfig = amazonSqsConfig
                });
            transport.Initialize();
            transport.Purge();
        }

        public const string 
            SnsHttpLocalhost = "http://localhost:9324",
            SqsHttpLocalHost = "http://localhost:9911";
    }
}
