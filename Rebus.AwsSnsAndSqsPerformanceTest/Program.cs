using System;
using System.Collections.Generic;
using System.IO;
using Rebus.AwsSnsAndSqsPerformanceTest.Markdown;
using Rebus.Config;
using Serilog;
using Logger = Serilog.Log;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    using System.Threading.Tasks;
    using Amazon.Runtime.Internal;
    using AwsSnsAndSqs;

    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Logger.Logger = new LoggerConfiguration()
                    .Enrich.WithRebusCorrelationId("Test-Run")
                     .WriteTo.File("logs-*.txt")
                     .CreateLogger();
                var markDownPage = new MarkDownPage();

                markDownPage.AddMarkDown(new Header
                {
                    HeaderLevel = HeaderLevel.One,
                    Text = "Aws rebus sns and sqs load test results"
                });

                markDownPage.AddMarkDown(TextControl.Newline);

                var tableControl = new TableControl();

                markDownPage.AddMarkDown(tableControl);

                tableControl.AddColumn("Test");
                tableControl.AddColumn("max # concurrent publishes");
                tableControl.AddColumn("Publish per second");
                tableControl.AddColumn("# of workers");
                tableControl.AddColumn("Receive max parallelism");
                tableControl.AddColumn("Receive per second");

#if DEBUG
                AsyncHelpers.RunSync(() => RunTest(
                    new SendOptions()
                    {
                        MessageSizeKilobytes = 4,
                        MaxDegreeOfParallelism = 200,
                        MaxMessagesPerTask = 1,
                    },
                    new ReceiveOptions()
                    {
                        RebusMaxParallelism = 200,
                        RebusNumberOfWorkers = Environment.ProcessorCount
                    }, tableControl));
#else
                 AsyncHelpers.RunSync(() => RunTest(
                    new SendOptions()
                    {
                        MessageSizeKilobytes = 4,
                        MaxDegreeOfParallelism = 200,
                        MaxMessagesPerTask = 1
                    },
                    new ReceiveOptions()
                    {
                        RebusMaxParallelism = 200,
                        RebusNumberOfWorkers = Environment.ProcessorCount
                    }, tableControl));
                AsyncHelpers.RunSync(() => RunTest(
                    new SendOptions()
                    {
                        MessageSizeKilobytes = 16,
                        MaxDegreeOfParallelism = 200,
                        MaxMessagesPerTask = 1
                    },
                    new ReceiveOptions()
                    {
                        RebusMaxParallelism = 200,
                        RebusNumberOfWorkers = Environment.ProcessorCount
                    }, tableControl));
                AsyncHelpers.RunSync(() => RunTest(
                    new SendOptions()
                    {
                        MessageSizeKilobytes = 32,
                        MaxDegreeOfParallelism = 200,
                        MaxMessagesPerTask = 1
                    },
                    new ReceiveOptions()
                    {
                        RebusMaxParallelism = 200,
                        RebusNumberOfWorkers = Environment.ProcessorCount
                    }, tableControl));
                AsyncHelpers.RunSync(() => RunTest(
                    new SendOptions()
                    {
                        MessageSizeKilobytes = 64,
                        MaxDegreeOfParallelism = 200,
                        MaxMessagesPerTask = 1
                    },
                    new ReceiveOptions()
                    {
                        RebusMaxParallelism = 200,
                        RebusNumberOfWorkers = Environment.ProcessorCount
                    }, tableControl));
                AsyncHelpers.RunSync(() => RunTest(
                    new SendOptions()
                    {
                        MessageSizeKilobytes = 128,
                        MaxDegreeOfParallelism = 200,
                        MaxMessagesPerTask = 1
                    },
                    new ReceiveOptions()
                    {
                        RebusMaxParallelism = 200,
                        RebusNumberOfWorkers = Environment.ProcessorCount
                    }, tableControl));
#endif

                Console.WriteLine("Creating load test result markdown");

                var file = "..\\..\\..\\LoadResults.md";

                using (FileStream fs = new FileStream(file, FileMode.Create))
                {
                    markDownPage.Write(fs);
                }

            }
            finally
            {
                Console.WriteLine("Complete. Press enter to close.");
                Console.ReadLine();
            }
        }

        private static async Task RunTest(SendOptions sendOptions, ReceiveOptions receiveOptions, TableControl tableControl)
        {
            var result = await PerformanceTest.RunTest(sendOptions, receiveOptions);

            var text = $"send msg for {sendOptions.MessageSizeKilobytes} kilobytes";

            var messagesSentPerSecond = result.MessageSentTimes.MessagesSentCounter.Value / result.MessageSentTimes.MessageSentTimePerTimePeriod.TotalSeconds;

            var messagesReceivedPerSecond = result.MessageReceivedTimes.MessagesReceivedCounter.Value / result.MessageReceivedTimes.MessageRecievedTimePerTimePeriod.Elapsed.TotalSeconds;

            tableControl.AddRow(new List<TableCellControl>()
            {
                new TableCellControl(text),
                new TableCellControl($"{sendOptions.MaxDegreeOfParallelism}"),
                new TableCellControl($"{messagesSentPerSecond}"),
                new TableCellControl($"{receiveOptions.RebusNumberOfWorkers}"),
                new TableCellControl($"{receiveOptions.RebusMaxParallelism}"),
                new TableCellControl($"{messagesReceivedPerSecond}"),
            });
        }
    }
}
