namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    internal class ReceiveOptions
    {
        public int RebusNumberOfWorkers { get; set; }
        public int RebusMaxParallelism { get; set; }
    }
}