using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Rebus.AwsSnsAndSqs.Amazon;
using Rebus.AwsSnsAndSqs.Amazon.SQS;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqsTests
{
    using AwsSnsAndSqs;

    public class AmazonSqsTransportFactory : ITransportFactory
    {
        static ConnectionInfo _connectionInfo;

        internal static ConnectionInfo ConnectionInfo => _connectionInfo ?? (_connectionInfo = ConnectionInfoFromFileOrNull()
                                                                                               ?? ConnectionInfoFromEnvironmentVariable("rebus2_asqs_connection_string")
                                                                                               ?? Throw("Could not find Amazon Sqs connetion Info!"));

        public ITransport Create(string inputQueueAddress, TimeSpan peeklockDuration, AmazonSQSTransportOptions options = null)
        {
            return inputQueueAddress == null ? CreateTransport(null, peeklockDuration, options) : _queuesToDelete.GetOrAdd(inputQueueAddress, () => CreateTransport(inputQueueAddress, peeklockDuration, options));
        }

        public static AmazonSQSTransport CreateTransport(string inputQueueAddress, TimeSpan peeklockDuration, AmazonSQSTransportOptions options = null)
        {
            var connectionInfo = ConnectionInfo;
            var amazonSqsConfig = new AmazonSQSConfig { RegionEndpoint = connectionInfo.RegionEndpoint };

            var consoleLoggerFactory = new ConsoleLoggerFactory(false);

            var transport = new AmazonSQSTransport(
                inputQueueAddress,
                new FailbackAmazonCredentialsFactory(), 
                amazonSqsConfig,
                consoleLoggerFactory,
                new TplAsyncTaskFactory(consoleLoggerFactory),
                options
            );

            transport.Initialize(peeklockDuration);
           
            return transport;
        }

        public ITransport CreateOneWayClient()
        {
            return Create(null, TimeSpan.FromSeconds(30));
        }

        public ITransport Create(string inputQueueAddress)
        {
            return Create(inputQueueAddress, TimeSpan.FromSeconds(30));
        }

        readonly Dictionary<string, AmazonSQSTransport> _queuesToDelete = new Dictionary<string, AmazonSQSTransport>();


        public void CleanUp()
        {
            CleanUp(false);
        }

        public void CleanUp(bool deleteQueues)
        {
            if (deleteQueues == false) return;

            foreach (var queueAndTransport in _queuesToDelete)
            {
                var transport = queueAndTransport.Value;

                transport.DeleteQueue();
            }
        }


        static ConnectionInfo ConnectionInfoFromEnvironmentVariable(string environmentVariableName)
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

        static ConnectionInfo ConnectionInfoFromFileOrNull()
        {
            var awsCredentials = FallbackCredentialsFactory.GetCredentials();

            var immutableCredentials = awsCredentials.GetCredentials();

            return new ConnectionInfo(immutableCredentials.AccessKey, immutableCredentials.SecretKey, RegionEndpoint.USWest2.SystemName);
        }

        static ConnectionInfo Throw(string message)
        {
            throw new RebusConfigurationException(message);
        }
    }
}