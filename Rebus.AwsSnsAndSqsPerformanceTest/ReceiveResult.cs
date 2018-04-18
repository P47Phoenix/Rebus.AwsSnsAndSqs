using Rebus.Activation;
using Rebus.AwsSnsAndSqsPerformanceTest.Counters;

namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    using System;
    using System.Diagnostics;

    internal class ReceiveResult
    {
        public IDisposable Worker { get; internal set; }
        public Counter MessagesReceivedCounter { get; internal set; }
        public Stopwatch MessageRecievedTimePerTimePeriod { get; internal set; }
        public ReceiveOptions ReceiveOptions { get; internal set; }
    }
}