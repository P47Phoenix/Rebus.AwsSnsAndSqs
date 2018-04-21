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
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var performanceTest = new PerformanceTest();

                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    var receiver = await performanceTest.Receive(receiveOptions, cancellationTokenSource.Token);

                    using (receiver.Worker)
                    {

                        var send = await performanceTest.Send(sendOptions, cancellationTokenSource);

                        stopwatch.Stop();

                        receiver.MessageRecievedTimePerTimePeriod.Stop();

                        try
                        {
                            performanceTest.PurgeQueue();
                        }
                        catch (Exception e)
                        {
                            Logger.Logger.Error(e, "Error purging queue of extra records");
                        }

                        return new PerformanceTestResult { MessageReceivedTimes = receiver, MessageSentTimes = send, TotalTestTimeMilliseconds = stopwatch.ElapsedMilliseconds };

                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Logger.Error(exception, "Error running test");
            }

            return null;

        }


        private async Task<SendResult> Send(SendOptions sendOptions, CancellationTokenSource cancellationTokenSource)
        {
            Stopwatch sw = new Stopwatch();
            Counter MessagesSentCounter = new Counter();

            try
            {

                using (BuiltinHandlerActivator clientBuiltinHandlerActivator = new BuiltinHandlerActivator())
                {
                    var client = Configure.With(clientBuiltinHandlerActivator).Logging(configurer => configurer.Serilog(Logger.Logger)).Transport(t =>
                    {
                        t.UseAmazonSnsAndSqsAsOneWayClient();
                        t.Decorate(context => context.Get<ITransport>());
                    }).Start();

                    Guid runId = Guid.NewGuid();

                    var bufferBlock = new BufferBlock<PerformanceTestMessage>(new DataflowBlockOptions { CancellationToken = cancellationTokenSource.Token });

                    var actionBlock = new ActionBlock<PerformanceTestMessage>(async message =>
                    {
                        var stopWatch = new Stopwatch();

                        try
                        {
                            if (cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                return;
                            }

                            message.UnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                            stopWatch.Start();

                            await client.Publish(message, optionalHeaders: new Dictionary<string, string>() { { "Test-Run", $"{message.Number}-{runId}" } });

                            stopWatch.Stop();


                            MessagesSentCounter.Increment();

                        }
                        catch (Exception error)
                        {
                            Logger.Logger.Error(error, "Error sending message");
                        }
                        finally
                        {
                            stopWatch.Reset();
                        }

                    }, dataflowBlockOptions: new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = sendOptions.MaxDegreeOfParallelism, MaxMessagesPerTask = sendOptions.MaxMessagesPerTask, CancellationToken = cancellationTokenSource.Token });


                    bufferBlock.LinkTo(actionBlock, linkOptions: new DataflowLinkOptions() { PropagateCompletion = true });

                    var msg = new string('1', count: sendOptions.MessageSizeKilobytes * 1024);
                    sw.Start();
                    int i = 0;
                    cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));
                    while (cancellationTokenSource.Token.IsCancellationRequested == false)
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

                    bufferBlock.Completion.ContinueWith(delegate { actionBlock.Complete(); }).Wait(TimeSpan.FromMinutes(5));

                    actionBlock.Completion.Wait(TimeSpan.FromMinutes(5));
                }
            }
            catch (Exception error)
            {
                Logger.Logger.Error(error, "Error for performance test send");
            }
            finally
            {
                sw.Stop();
            }

            var result = await Task.FromResult(new SendResult { MessageSentTimePerTimePeriod = sw.Elapsed, MessagesSentCounter = MessagesSentCounter, SendOptions = sendOptions });

            return result;


        }

        private long receivedMessage = 0;

        private async Task<ReceiveResult> Receive(ReceiveOptions receiveOptions, CancellationToken cancellationToken)
        {
            Stopwatch sw = new Stopwatch();

            Counter MessagesReceivedCounter = new Counter();

            var workerBuiltinHandlerActivator = new BuiltinHandlerActivator();

            try
            {
                workerBuiltinHandlerActivator.Handle<PerformanceTestMessage>(message =>
                {

                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return Task.CompletedTask;
                        }

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
                    }
                    catch (Exception error)
                    {
                        Logger.Logger.Error(error, "Error in PerformanceTestMessage handler");
                    }
                    finally
                    {
                        MessagesReceivedCounter.Increment();
                    }

                    return Task.CompletedTask;

                });



                var worker = Configure
                    .With(workerBuiltinHandlerActivator)
                    .Logging(configurer => configurer.Serilog(Logger.Logger))
                    .Transport(t =>
                    {
                        // set the worker queue name
                        t.UseAmazonSnsAndSqs(workerQueueAddress: QueueName);
                    }).Routing(r =>
                    {
                        // Map the message type to the queue
                        r.TypeBased().Map<PerformanceTestMessage>(QueueName);
                    }).Options(configurer =>
                    {
                        configurer.SetMaxParallelism(receiveOptions.RebusMaxParallelism);
                        configurer.SetNumberOfWorkers(receiveOptions.RebusNumberOfWorkers);
                    }).Start();


                await worker.Subscribe<PerformanceTestMessage>();
            }
            catch (Exception error)
            {
                Logger.Logger.Error(error, "Error starting up worker");
            }

            var result = await Task.FromResult(new ReceiveResult { Worker = workerBuiltinHandlerActivator, MessageRecievedTimePerTimePeriod = sw, MessagesReceivedCounter = MessagesReceivedCounter, ReceiveOptions = receiveOptions });

            return result;
        }

        public string QueueName { get; set; } = nameof(Send);

        private void PurgeQueue()
        {
            TransportWrapperSingleton.GetAmazonSqsTransport(QueueName)?.Purge();
        }
    }
}
