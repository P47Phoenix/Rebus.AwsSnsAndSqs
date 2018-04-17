using System.IO;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public class Header : IMarkDownControl
    {
        public string Id { get; set; } = nameof(Header);

        public string Text { get; set; } = null;

        public HeaderLevel HeaderLevel { get; set; } = HeaderLevel.One;

        public void Write(Stream ste)
        {
            string headerPrefix;

            switch (HeaderLevel)
            {
                case HeaderLevel.One:
                    headerPrefix = new string('#', 1);
                    break;
                case HeaderLevel.Two:
                    headerPrefix = new string('#', 2);
                    break;
                case HeaderLevel.Three:
                    headerPrefix = new string('#', 3);
                    break;
                case HeaderLevel.Four:
                    headerPrefix = new string('#', 4);
                    break;
                case HeaderLevel.Five:
                    headerPrefix = new string('#', 5);
                    break;
                case HeaderLevel.Six:
                    headerPrefix = new string('#', 6);
                    break;
                default:
                    headerPrefix = new string('#', 1);
                    break;
            }

            using (StreamWriter streamWriter = ste.GetStreamWriterForStream())
            {
                streamWriter.WriteLine($"{headerPrefix} {Text ?? "(Not test was set)"}");
            }

        }
    }
}