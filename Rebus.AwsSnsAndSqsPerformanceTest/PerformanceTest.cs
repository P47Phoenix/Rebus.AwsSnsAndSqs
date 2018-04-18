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
    using AwsSnsAndSqs.RebusAmazon;
    using Counters;
    using Transport;

    internal class PerformanceTest
    {
        public static async Task<PerformanceTestResult> RunTest(SendOptions sendOptions, ReceiveOptions receiveOptions)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var performanceTest = new PerformanceTest();
            var receiver = await performanceTest.Receive(receiveOptions);
            using (receiver.Worker)
            {
                try
                {
                    var send = await performanceTest.Send(sendOptions);

                    stopwatch.Stop();
                    performanceTest.PurgeQueue();
                    return new PerformanceTestResult
                    {
                        MessageReceivedTimes = receiver,
                        MessageSentTimes = send,
                        TotalTestTimeMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }
                finally
                {
                    receiver.MessageRecievedTimePerTimePeriod.Stop();
                }
            }

        }


        private async Task<SendResult> Send(SendOptions sendOptions)
        {
            Stopwatch sw = new Stopwatch();
            Counter MessagesSentCounter = new Counter();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            using (BuiltinHandlerActivator clientBuiltinHandlerActivator = new BuiltinHandlerActivator())
            {
                var client = Configure.With(clientBuiltinHandlerActivator)
                    .Logging(configurer => configurer.Serilog(Logger.Logger))
                    .Transport(t =>
                    {
                        t.UseAmazonSnsAndSqsAsOneWayClient();
                        t.Decorate(context => context.Get<ITransport>());
                    }).Start();

                Guid runId = Guid.NewGuid();

                var bufferBlock = new BufferBlock<PerformanceTestMessage>();


                var actionBlock = new ActionBlock<PerformanceTestMessage>(async message =>
                {
                    message.UnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var stopWatch = new Stopwatch();

                    stopWatch.Start();

                    await client.Publish(message, optionalHeaders: new Dictionary<string, string>() { { "Test-Run", $"{message.Number}-{runId}" } });

                    stopWatch.Stop();


                    MessagesSentCounter.Increment();

                    stopWatch.Reset();

                }, dataflowBlockOptions: new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = sendOptions.MaxDegreeOfParallelism,
                    MaxMessagesPerTask = sendOptions.MaxMessagesPerTask
                });


                bufferBlock.LinkTo(actionBlock, linkOptions: new DataflowLinkOptions() { PropagateCompletion = true });

                var msg = new string('1', count: sendOptions.MessageSizeKilobytes * 1024);
                cancellationTokenSource.CancelAfter(sendOptions.HowLongToSend);
                sw.Start();
                int i = 0;
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (bufferBlock.Count > 10000 || actionBlock.InputCount > 10000)
                    {
                        Console.WriteLine(value: $"Sent:{MessagesSentCounter.Value}");
                        Thread.Sleep(500);
                        continue;
                    }

                    bufferBlock.Post(item: new PerformanceTestMessage { Message = msg, Number = ++i });
                }
                sw.Stop();

                bufferBlock.Complete();

                bufferBlock.Completion.ContinueWith(delegate { actionBlock.Complete(); }).Wait(TimeSpan.FromMinutes(1));

                actionBlock.Completion.Wait(TimeSpan.FromMinutes(1));

                return new SendResult
                {
                    MessageSentTimePerTimePeriod = sw.Elapsed,
                    MessagesSentCounter = MessagesSentCounter,
                    SendOptions = sendOptions
                };
            }
        }

        private long receivedMessage = 0;

        private async Task<ReceiveResult> Receive(ReceiveOptions receiveOptions)
        {
            Stopwatch sw = new Stopwatch();
            Counter MessagesReceivedCounter = new Counter();

            var workerBuiltinHandlerActivator = new BuiltinHandlerActivator();

            workerBuiltinHandlerActivator.Handle<PerformanceTestMessage>(async message =>
            {
                if (Interlocked.Exchange(ref receivedMessage, 1) == 0)
                {
                    sw.Start();
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var milliseconds = now > message.UnixTimeMilliseconds ? now - message.UnixTimeMilliseconds : message.UnixTimeMilliseconds - now;

                if (milliseconds <= 0)
                {
                    throw new Exception($"Time not correct {nameof(milliseconds)}:{milliseconds}");
                }

                MessagesReceivedCounter.Increment();
            });



            var worker = Configure.With(workerBuiltinHandlerActivator).Logging(configurer => configurer.Serilog(Logger.Logger)).Transport(t =>
            {
                // set the worker queue name
                t.UseAmazonSnsAndSqs(workerQueueAddress: nameof(Send));
            }).Routing(r =>
            {
                // Map the message type to the queue
                r.TypeBased().Map<PerformanceTestMessage>(nameof(Send));
            }).Options(configurer =>
            {
                configurer.SetMaxParallelism(receiveOptions.RebusMaxParallelism);
                configurer.SetNumberOfWorkers(receiveOptions.RebusNumberOfWorkers);
            }).Start();


            await worker.Subscribe<PerformanceTestMessage>();

            return new ReceiveResult
            {
                Worker = workerBuiltinHandlerActivator,
                MessageRecievedTimePerTimePeriod = sw,
                MessagesReceivedCounter = MessagesReceivedCounter,
                ReceiveOptions = receiveOptions
            };
        }

        private void PurgeQueue()
        {
            TransportWrapperSingleton.GetAmazonSqsTransport(nameof(Send))?.Purge();
        }
    }
}
