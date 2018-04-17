using System.IO;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public interface IMarkDownWriter
    {
        void Write(Stream ste);
    }
}