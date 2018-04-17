using System;
using System.Collections.Generic;
using System.IO;
using Rebus.AwsSnsAndSqsPerformanceTest.Markdown;
using Rebus.Config;
using Serilog;
using Logger = Serilog.Log;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
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
                tableControl.AddColumn("Total test durration");
                tableControl.AddColumn("Publish Count");
                tableControl.AddColumn("Publish Taken Min");
                tableControl.AddColumn("Publish Taken Max");
                tableControl.AddColumn("Publish Taken Avg");
                tableControl.AddColumn("Receive Count");
                tableControl.AddColumn("Receive Taken Min");
                tableControl.AddColumn("Receive Taken Max");
                tableControl.AddColumn("Receive Taken Avg");

                RunTest(100, 4, tableControl);

                using (FileStream fs = new FileStream("..\\..\\..\\LoadResults.md", FileMode.CreateNew))
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

        private static void RunTest(long numberOfMessages, int messageSizeKilobytes, TableControl tableControl)
        {
            var result = PerformanceTest.RunTest(numberOfMessages, messageSizeKilobytes);

            var timeText = $"{numberOfMessages} messages at {messageSizeKilobytes} kilobytes";

            var total = TimeSpan.FromMilliseconds(result.TotalTestTimeMilliseconds);

            var sendMin = TimeSpan.FromMilliseconds(result.MessageSentTimes.MinValue);
            var sendMax = TimeSpan.FromMilliseconds(result.MessageSentTimes.MaxValue);
            var sendAvg = TimeSpan.FromMilliseconds(result.MessageSentTimes.TolalValue / result.MessageSentTimes.Count);

            var receivedMin = TimeSpan.FromMilliseconds(result.MessageRecivedTimes.MinValue);
            var recievedMax = TimeSpan.FromMilliseconds(result.MessageRecivedTimes.MaxValue);
            var receivedAvg = TimeSpan.FromMilliseconds(result.MessageRecivedTimes.TolalValue / result.MessageRecivedTimes.Count);

            tableControl.AddRow(new List<TableCellControl>()
            {
                new TableCellControl(timeText), 
                new TableCellControl($"{total}"), 
                new TableCellControl($"{result.MessageSentTimes.Count}"), 
                new TableCellControl($"{sendMin}"), 
                new TableCellControl($"{sendMax}"), 
                new TableCellControl($"{sendAvg}"), 
                new TableCellControl($"{result.MessageRecivedTimes.Count}"), 
                new TableCellControl($"{receivedMin}"), 
                new TableCellControl($"{recievedMax}"), 
                new TableCellControl($"{receivedAvg}"),
            });
        }
    }


    public class MarkDownBuilder
    {
        private MarkDownPage _markDownPage = new MarkDownPage();
    }
}
