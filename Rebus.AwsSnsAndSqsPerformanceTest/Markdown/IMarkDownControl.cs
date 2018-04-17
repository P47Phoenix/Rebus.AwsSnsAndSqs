namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public interface IMarkDownControl : IMarkDownWriter
    {
        string Id { get; set; }
    }
}