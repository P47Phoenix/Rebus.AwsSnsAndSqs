using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.AwsSnsAndSqs.RebusAmazon;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqsTests
{
    internal class AmazonSqsTransportFactory : ITransportFactory
    {
        private static ConnectionInfo _connectionInfo;

        private readonly Dictionary<string, AmazonSqsTransport> _queuesToDelete = new Dictionary<string, AmazonSqsTransport>();

        internal static ConnectionInfo ConnectionInfo => _connectionInfo ?? (_connectionInfo = ConnectionInfoFromFileOrNull() ?? ConnectionInfoFromEnvironmentVariable("rebus2_asqs_connection_string") ?? Throw("Could not find Amazon Sqs connetion Info!"));

        public ITransport CreateOneWayClient()
        {
            return Create(null, TimeSpan.FromSeconds(30));
        }

        public ITransport Create(string inputQueueAddress)
        {
            return Create(inputQueueAddress, TimeSpan.FromSeconds(30));
        }


        public void CleanUp()
        {
            CleanUp(false);
        }

        public ITransport Create(string inputQueueAddress, TimeSpan peeklockDuration, AmazonSnsAndSqsTransportOptions options = null)
        {
            return inputQueueAddress == null ? CreateTransport(null, peeklockDuration, options) : _queuesToDelete.GetOrAdd(inputQueueAddress, () => CreateTransport(inputQueueAddress, peeklockDuration, options));
        }

        public static AmazonSqsTransport CreateTransport(string inputQueueAddress, TimeSpan peeklockDuration, AmazonSnsAndSqsTransportOptions options = null)
        {
            var connectionInfo = ConnectionInfo;
            var amazonSqsConfig = new AmazonSQSConfig {RegionEndpoint = connectionInfo.RegionEndpoint};

            var consoleLoggerFactory = new ConsoleLoggerFactory(false);

            var transport = new AmazonSqsTransport(new AmazonInternalSettings(consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory), new FailbackAmazonCredentialsFactory()) {InputQueueAddress = inputQueueAddress, AmazonSqsConfig = amazonSqsConfig, AmazonSnsAndSqsTransportOptions = options ?? new AmazonSnsAndSqsTransportOptions(), AmazonSimpleNotificationServiceConfig = new AmazonSimpleNotificationServiceConfig(), MessageSerializer = new AmazonTransportMessageSerializer()});

            transport.Initialize(peeklockDuration);

            return transport;
        }

        public void CleanUp(bool deleteQueues)
        {
            if (deleteQueues == false)
            {
                return;
            }

            foreach (var queueAndTransport in _queuesToDelete)
            {
                var transport = queueAndTransport.Value;

                transport.DeleteQueue();
            }
        }


        private static ConnectionInfo ConnectionInfoFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using AmazonSqs connection info from env variable {0}", environmentVariableName);

            return ConnectionInfo.CreateFromString(value);
        }

        private static ConnectionInfo ConnectionInfoFromFileOrNull()
        {
            var awsCredentials = FallbackCredentialsFactory.GetCredentials();

            var immutableCredentials = awsCredentials.GetCredentials();

            return new ConnectionInfo(immutableCredentials.AccessKey, immutableCredentials.SecretKey, RegionEndpoint.USWest2.SystemName);
        }

        private static ConnectionInfo Throw(string message)
        {
            throw new RebusConfigurationException(message);
        }
    }
}
