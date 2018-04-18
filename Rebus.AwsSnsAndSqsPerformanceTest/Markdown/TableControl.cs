using System;
using System.Collections.Generic;
using System.IO;

namespace Rebus.AwsSnsAndSqsPerformanceTest.Markdown
{
    public class TableControl : IMarkDownControl
    {
        public string Id { get; set; } = nameof(TableControl);

        private readonly List<TableCellControl> _tableColumns = new List<TableCellControl>();

        private readonly List<List<TableCellControl>> _tableRows = new List<List<TableCellControl>>();

        public void AddColumn(TableCellControl cellControl)
        {
            _tableColumns.Add(cellControl);
        }

        public void AddRow(List<TableCellControl> rowData, bool throwIfColumnCountDoesNotMatchRowCount = true)
        {
            if (throwIfColumnCountDoesNotMatchRowCount && rowData?.Count != _tableColumns?.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(rowData), $"Row cellControl count:{rowData?.Count} does not match the column count:{_tableColumns?.Count}");
            }

            _tableRows.Add(rowData);
        }

        internal void AddColumn(string text)
        {
            _tableColumns.Add(new TableCellControl(text));
        }

        public void Write(Stream ste)
        {
            bool isFirstColumn = true;
            foreach (var column in _tableColumns)
            {
                if (isFirstColumn == false)
                {
                    TextControl.Space.Write(ste);
                    TextControl.Pipe.Write(ste);
                    TextControl.Space.Write(ste);
                }
                else
                {
                    isFirstColumn = false;
                }

                column.Write(ste);
            }
            
            TextControl.Newline.Write(ste);
            isFirstColumn = true;

            for (var i = 0; i < _tableColumns.Count; i++)
            {
                if (isFirstColumn == false)
                {
                    TextControl.Space.Write(ste);
                    TextControl.Pipe.Write(ste);
                    TextControl.Space.Write(ste);
                }
                else
                {
                    isFirstColumn = false;
                }

                TextControl.Hyphen.Write(ste);
                TextControl.Hyphen.Write(ste);
            }

            TextControl.Newline.Write(ste);

            foreach (var tableRow in _tableRows)
            {
                isFirstColumn = true;

                foreach (var tableCellControl in tableRow)
                {
                    if (isFirstColumn == false)
                    {
                        TextControl.Space.Write(ste);
                        TextControl.Pipe.Write(ste);
                        TextControl.Space.Write(ste);
                    }
                    else
                    {
                        isFirstColumn = false;
                    }
                    tableCellControl.Write(ste);
                }

                TextControl.Newline.Write(ste);
            }
        }
    }
}