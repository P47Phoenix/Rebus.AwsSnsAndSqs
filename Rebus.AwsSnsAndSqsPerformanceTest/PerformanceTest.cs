using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Logger = Serilog.Log;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    internal class PerformanceTest
    {
        public static PerformanceTestResult RunTest(long numberOfMessages, int messageSizeKilobytes)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var performanceTest = new PerformanceTest();
            performanceTest.Setup();
            performanceTest.SendAndRecieve(numberOfMessages, messageSizeKilobytes);
            performanceTest.TearDown();
            stopwatch.Stop();
            return new PerformanceTestResult
            {
                MessageRecivedTimes = performanceTest.MessageRecievedTimeInMillisecondsCount,
                MessageSentTimes = performanceTest.MessageSentTimeInMillisecondsCount,
                TotalTestTimeMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }


        private IBus _Client;
        private IBus _Worker;

        private BuiltinHandlerActivator _clientBuiltinHandlerActivator;
        private BuiltinHandlerActivator _workerBuiltinHandlerActivator;

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private long _messagesRecievedCounter;

        private long _messsagesToSend;

        private TimeCounter MessageSentTimeInMillisecondsCount { get; } = new TimeCounter();
        private TimeCounter MessageRecievedTimeInMillisecondsCount { get; } = new TimeCounter();



        private void Setup()
        {
            MessageRecievedTimeInMillisecondsCount.Clear();
            MessageSentTimeInMillisecondsCount.Clear();

            _workerBuiltinHandlerActivator = new BuiltinHandlerActivator();

            _workerBuiltinHandlerActivator.Handle<PerformanceTestMessage>(message =>
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var milliseconds = now > message.UnixTimeMilliseconds ? now - message.UnixTimeMilliseconds : message.UnixTimeMilliseconds - now;

                if (milliseconds <= 0)
                {
                    throw new Exception($"Time not correct {nameof(milliseconds)}:{milliseconds}");
                }

                MessageRecievedTimeInMillisecondsCount.AddTime(milliseconds);
                
                if (MessageRecievedTimeInMillisecondsCount.Count >= Interlocked.Read(ref _messsagesToSend))
                {
                    _autoResetEvent.Set();
                }

                return Task.CompletedTask;

            });

            
            _clientBuiltinHandlerActivator = new BuiltinHandlerActivator();


            var queueName = nameof(SendAndRecieve);

            _Client = CreateClient();

            var task = _Client.Subscribe<PerformanceTestMessage>();
            AsyncHelpers.RunSync(() => task);

        }

        private IBus CreateClient()
        {
            throw new NotImplementedException();
        }

        private IBus CreateWorker(string queueName)
        {
            return Configure
                .With(_workerBuiltinHandlerActivator)
                .Logging(configurer => configurer.Serilog(Logger.Logger))
                .Transport(t =>
                {
                    // set the worker queue name
                    t.UseAmazonSnsAndSqs(workerQueueAddress: queueName);
                    //t.UseAmazonSnsAndSqsAsOneWayClient();
                })
                .Routing(r =>
                {
                    // Map the message type to the queue
                    r.TypeBased().Map<PerformanceTestMessage>(queueName);
                })
                .Options(configurer =>
                {
                    configurer.SetMaxParallelism(Environment.ProcessorCount);
                    configurer.SetNumberOfWorkers(Environment.ProcessorCount);
                })
                .Start();
        }

        private void TearDown()
        {
            Console.WriteLine("Cleaning up bus");
            _Client.Dispose();

            _workerBuiltinHandlerActivator.Dispose();


        }

        private void SendAndRecieve(long numberOfMessages, int messageSizeKilobytes)
        {
            Interlocked.Exchange(ref _messsagesToSend, numberOfMessages);
            Interlocked.Exchange(ref _messagesRecievedCounter, 0);

            Guid runId = Guid.NewGuid();

            var bufferBlock = new BufferBlock<PerformanceTestMessage>();


            var actionBlock = new ActionBlock<PerformanceTestMessage>(message =>
            {
                message.UnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var task = _Client.Publish(message, optionalHeaders: new Dictionary<string, string>()
                {
                    {
                        "Test-Run",
                        $"{message.Number}-{runId}"
                    }
                });
                AsyncHelpers.RunSync(() => task);
                stopWatch.Stop();
                MessageSentTimeInMillisecondsCount.AddTime(stopWatch.ElapsedMilliseconds);
                stopWatch.Reset();

            }, dataflowBlockOptions: new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                MaxMessagesPerTask = 1
            });


            bufferBlock.LinkTo(actionBlock, linkOptions: new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            var msg = new string('1', count: messageSizeKilobytes * 1024);

            for (var i = 0; i < numberOfMessages; i++)
            {
                bufferBlock.Post(item: new PerformanceTestMessage
                {
                    Message = msg,
                    Number = i
                });
            }

            while (_autoResetEvent.WaitOne(3000) == false)
            {
                Console.WriteLine(value: $"Recieved:{MessageRecievedTimeInMillisecondsCount.Count} Sent:{MessageSentTimeInMillisecondsCount.Count}");
            }
            
            bufferBlock.Complete();

            bufferBlock.Completion.ContinueWith(delegate
            {
                actionBlock.Complete();
            }).Wait();

            actionBlock.Completion.Wait();
        }
    }
}
