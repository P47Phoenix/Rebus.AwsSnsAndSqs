namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    internal class SqsInfo
    {
        public string Arn { get; set; }
        public string Url { get; internal set; }
        public string AccountId { get; internal set; }
        public string Name { get; internal set; }
        public string Region { get; internal set; }
    }
}
