using System.Collections.Generic;
using System.IO;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public class TableCellControl : IMarkDownControl
    {
        public string Id { get; set; } = nameof(TableCellControl);

        private readonly List<IMarkDownControl> _markDownControls = new List<IMarkDownControl>();


        public TableCellControl() { }
     
        public TableCellControl(string text)
        {
            _markDownControls.Add(new TextControl()
            {
                Text = text
            });
        }

        public void AddText(string text)
        {
            _markDownControls.Add(new TextControl()
            {
                Text = text
            });
        }

        public void Write(Stream ste)
        {
            foreach (var control in _markDownControls)
            {
                control.Write(ste);
            }
        }
    }
}