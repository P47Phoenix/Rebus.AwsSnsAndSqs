using System;
using System.IO;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public class TextControl : IMarkDownControl
    {
        public string Id { get; set; } = nameof(Text);

        public string Text { get; set; }

        public void Write(Stream ste)
        {
            using (var streamWriter = ste.GetStreamWriterForStream())
            {
                streamWriter.Write(Text);
            }
        }

        internal static TextControl Space { get; } = new TextControl
        {
            Text = " "
        };

        internal static TextControl Hyphen { get; } = new TextControl
        {
            Text = "-"
        };

        internal static TextControl Pipe { get; } = new TextControl
        {
            Text = "|"
        };

        internal static TextControl Newline { get; } = new TextControl
        {
            Text = Environment.NewLine
        };
    }
}