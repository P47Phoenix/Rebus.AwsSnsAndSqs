using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public static class Helpers
    {
        public static StreamWriter GetStreamWriterForStream(this Stream ste)
        {
            return new StreamWriter(ste, Encoding.UTF8, 1024, true);
        }
    }
}
