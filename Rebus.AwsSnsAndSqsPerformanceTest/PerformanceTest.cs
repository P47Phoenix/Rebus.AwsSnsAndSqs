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

    public class PerformanceTestResult
    {
        public TimeCounter MessageSentTimes { get; set; }

        public TimeCounter MessageRecivedTimes { get; set; }

        public long TotalTestTimeMilliseconds { get; set; }
    }

    internal class PerformanceTest
    {
        public static PerformanceTestResult RunTest(long numberOfMessages, int messageSizeKilobytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var pt = new PerformanceTest();
            pt.Setup();
            pt.SendAndRecieve(numberOfMessages, messageSizeKilobytes);
            pt.TearDown();
            sw.Stop();
            return new PerformanceTestResult
            {
                MessageRecivedTimes = pt.MessageRecievedTimeInMillisecondsCount,
                MessageSentTimes = pt.MessageSentTimeInMillisecondsCount,
                TotalTestTimeMilliseconds = sw.ElapsedMilliseconds
            };
        }


        private IBus _bus;
        private BuiltinHandlerActivator _builtinHandlerActivator;

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private long _messagesRecievedCounter;

        private long _messsagesToSend;

        private TimeCounter MessageSentTimeInMillisecondsCount { get; } = new TimeCounter();
        private TimeCounter MessageRecievedTimeInMillisecondsCount { get; } = new TimeCounter();
        private readonly ConcurrentDictionary<int, SendAndReceivedState> _sendAndReceiveds = new ConcurrentDictionary<int, SendAndReceivedState>();


        private void Sent(int id)
        {
            var sentOrReceived = _sendAndReceiveds.GetOrAdd(id, i => new SendAndReceivedState(i));
            sentOrReceived.Sent = true;
        }

        private bool IsReceivedAlready(int id)
        {
            var sendAndReceived = _sendAndReceiveds[id];
            lock (sendAndReceived)
            {
                if (sendAndReceived.Received)
                {
                    return true;
                }

                sendAndReceived.Received = true;
            }
            return false;
        }

        private void Setup()
        {
            MessageRecievedTimeInMillisecondsCount.Clear();
            MessageSentTimeInMillisecondsCount.Clear();
            _sendAndReceiveds.Clear();

            _builtinHandlerActivator = new BuiltinHandlerActivator();

            _builtinHandlerActivator.Handle<PerformanceTestMessage>(message =>
            {
                if (IsReceivedAlready(message.Number))
                {
                    return Task.CompletedTask;
                }

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

            var queueName = nameof(SendAndRecieve);

            _bus = Configure
                .With(_builtinHandlerActivator)
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
                    configurer.SetMaxParallelism(Environment.ProcessorCount * 2);
                    configurer.SetNumberOfWorkers(Environment.ProcessorCount);
                })
                .Start();

            var task = _bus.Subscribe<PerformanceTestMessage>();
            AsyncHelpers.RunSync(() => task);

        }

        private void TearDown()
        {
            Console.WriteLine("Cleaning up bus");
            _bus.Dispose();

            _builtinHandlerActivator.Dispose();


        }

        private void SendAndRecieve(long numberOfMessages, int messageSizeKilobytes)
        {
            Interlocked.Exchange(ref _messsagesToSend, numberOfMessages);
            Interlocked.Exchange(ref _messagesRecievedCounter, 0);

            Guid runId = Guid.NewGuid();

            var actionBlock = new ActionBlock<PerformanceTestMessage>(message =>
            {
                message.UnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                Sent(message.Number);
                var task = _bus.Publish(message, new Dictionary<string, string>()
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

            }, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = 4,
                MaxMessagesPerTask = 20
            });

            var msg = new string('1', messageSizeKilobytes * 1024);

            for (var i = 0; i < numberOfMessages; i++)
            {
                actionBlock.Post(new PerformanceTestMessage
                {
                    Message = msg,
                    Number = i
                });
            }

            while (_autoResetEvent.WaitOne(3000) == false)
            {
                Console.WriteLine($"Recieved:{MessageRecievedTimeInMillisecondsCount.Count} Sent:{MessageSentTimeInMillisecondsCount.Count}");
            }

        }
    }
}
