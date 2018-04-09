using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Amazon;
using Rebus.AwsSnsAndSqs.Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AwsSnsAndSqsTests
{
    using AwsSnsAndSqs;

    public class AmazonSqsManyMessagesTransportFactory : IBusFactory
    {
        readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            PurgeQueue(inputQueueAddress);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(t =>
                {
                    var info = AmazonSqsTransportFactory.ConnectionInfo;

                    var amazonSqsConfig = new AmazonSQSConfig { RegionEndpoint = info.RegionEndpoint };

                    t.UseAmazonSQS(info.AccessKeyId, info.SecretAccessKey, amazonSqsConfig, inputQueueAddress);
                })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(10);
                    o.SetMaxParallelism(10);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        public static void PurgeQueue(string queueName)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);

            var connectionInfo = AmazonSqsTransportFactory.ConnectionInfo;
            var amazonSqsConfig = new AmazonSQSConfig { RegionEndpoint = connectionInfo.RegionEndpoint };
            
            var transport = new AmazonSQSTransport(
                queueName,
                new FailbackAmazonCredentialsFactory(), 
                amazonSqsConfig, consoleLoggerFactory,
                new TplAsyncTaskFactory(consoleLoggerFactory)
            );
            transport.Initialize();
            transport.Purge();
        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}