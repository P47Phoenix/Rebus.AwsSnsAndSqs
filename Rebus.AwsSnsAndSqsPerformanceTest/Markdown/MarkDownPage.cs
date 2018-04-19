using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    internal class MarkDownPage : IMarkDownWriter
    {
        private readonly List<IMarkDownControl> _markDownWriters = new List<IMarkDownControl>();

        public void Write(Stream ste)
        {
            foreach (var markDownWriter in _markDownWriters)
            {
                markDownWriter.Write(ste);
            }
        }

        public void AddMarkDown(IMarkDownControl markDownWriter)
        {
            _markDownWriters.Add(markDownWriter);
        }

        public void ClearMarkDown()
        {
            _markDownWriters.Clear();
        }

        public void Remove(IMarkDownControl markDownWriter)
        {
            _markDownWriters.Remove(markDownWriter);
        }

        internal void AddTextNewLine(string text)
        {
            _markDownWriters.Add(TextControl.Newline);
            _markDownWriters.Add(new TextControl()
            {
                Text = text
            });
        }
    }
}