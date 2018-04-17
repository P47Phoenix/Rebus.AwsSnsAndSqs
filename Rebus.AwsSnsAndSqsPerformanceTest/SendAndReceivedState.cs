namespace Rebus.AwsSnsAndSqsPerformanceTest
{
    internal class SendAndReceivedState
    {
        private int i;

        public SendAndReceivedState(int i)
        {
            this.i = i;
        }

        public int ID => i;
        public bool Sent { get; set; } = false;
        public bool Received { get; set; } = false;
    }
}