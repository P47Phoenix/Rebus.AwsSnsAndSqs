using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.AwsSnsAndSqs.RebusAmazon;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AwsSnsAndSqsTests
{
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
            var amazonSqsConfig = new AmazonSQSConfig {RegionEndpoint = connectionInfo.RegionEndpoint};

            var transport = new AmazonSQSTransport(new AmazonInternalSettings(consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory)) {InputQueueAddress = queueName, AmazonSqsConfig = amazonSqsConfig});
            transport.Initialize();
            transport.Purge();
        }
    }
}
