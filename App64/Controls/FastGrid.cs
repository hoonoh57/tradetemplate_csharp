using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace App64.Controls
{
    public class FastGrid : Control
    {
        private VScrollBar _vScrollBar;
        private int _rowHeight = 24;
        private int _headerHeight = 30;
        private int _virtualRowCount = 0;
        private int _selectedRowIndex = -1;

        public class GridColumn
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public int Width { get; set; }
        }

        private List<GridColumn> _columns = new List<GridColumn>();
        private List<Dictionary<string, string>> _rows = new List<Dictionary<string, string>>();
        private List<Color> _rowColors = new List<Color>(); // To support per-row text colors if needed

        public List<GridColumn> Columns => _columns;
        public List<Dictionary<string, string>> Rows => _rows;

        public event EventHandler<DataGridViewCellEventArgs> CellDoubleClick;
        public event EventHandler<DataGridViewCellEventArgs> CellClick;
        public event EventHandler<CellValueNeededEventArgs> CellValueNeeded;

        public FastGrid()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            _vScrollBar = new VScrollBar();
            _vScrollBar.Dock = DockStyle.Right;
            _vScrollBar.Visible = false;
            this.Controls.Add(_vScrollBar);

            _vScrollBar.Scroll += OnScroll;
        }

        public void AddColumn(string name, string title, int width)
        {
            _columns.Add(new GridColumn { Name = name, Title = title, Width = width });
            this.Invalidate();
        }

        public void AddRow(Dictionary<string, string> rowData)
        {
            _rows.Add(new Dictionary<string, string>(rowData));
            UpdateScroll();
            this.Invalidate();
        }

        public void ClearRows()
        {
            _rows.Clear();
            _selectedRowIndex = -1;
            UpdateScroll();
            this.Invalidate();
        }

        public void UpdateRow(int index, string columnName, string value)
        {
            if (index >= 0 && index < _rows.Count)
            {
                _rows[index][columnName] = value;
                this.Invalidate();
            }
        }

        public string GetCellValue(int rowIndex, string columnName)
        {
            if (rowIndex >= 0 && rowIndex < _rows.Count)
            {
                if (_rows[rowIndex].TryGetValue(columnName, out string val)) return val;
            }
            return "";
        }

        public int RowCount
        {
            get => Math.Max(_rows.Count, _virtualRowCount);
            set
            {
                _virtualRowCount = value;
                UpdateScroll();
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Draw Header
            var headerRect = new Rectangle(0, 0, this.Width, _headerHeight);
            using (var b = new SolidBrush(Color.FromArgb(40, 40, 40)))
            {
                g.FillRectangle(b, headerRect);
            }
            g.DrawLine(Pens.Gray, 0, _headerHeight - 1, this.Width, _headerHeight - 1);

            int hX = 0;
            foreach (var col in _columns)
            {
                var cellRect = new Rectangle(hX, 0, col.Width, _headerHeight);
                TextRenderer.DrawText(g, col.Title, this.Font, cellRect, Color.Silver, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                g.DrawLine(Pens.DimGray, cellRect.Right, 0, cellRect.Right, _headerHeight);
                hX += col.Width;
            }

            // 2. Draw Data Rows
            int scrollOffset = _vScrollBar.Value;
            int visibleRows = GetVisibleRowCount() + 1;
            int totalRows = this.RowCount;

            int firstRow = scrollOffset;
            int lastRow = Math.Min(totalRows - 1, firstRow + visibleRows);

            for (int i = firstRow; i <= lastRow; i++)
            {
                int y = _headerHeight + (i - firstRow) * _rowHeight;
                int rowWidth = this.Width - (_vScrollBar.Visible ? _vScrollBar.Width : 0);
                var rowRect = new Rectangle(0, y, rowWidth, _rowHeight);

                // Background
                if (i == _selectedRowIndex)
                {
                    using (var b = new SolidBrush(Color.FromArgb(60, 60, 80)))
                    {
                        g.FillRectangle(b, rowRect);
                    }
                    g.DrawRectangle(Pens.DodgerBlue, rowRect.X, rowRect.Y, rowRect.Width - 1, rowRect.Height - 1);
                }
                else if (i % 2 == 0)
                {
                    g.FillRectangle(Brushes.Black, rowRect);
                }
                else
                {
                    using (var b = new SolidBrush(Color.FromArgb(20, 20, 20)))
                    {
                        g.FillRectangle(b, rowRect);
                    }
                }

                // Cell Values
                int x = 0;
                for (int c = 0; c < _columns.Count; c++)
                {
                    var col = _columns[c];
                    var cellRect = new Rectangle(x, y, col.Width, _rowHeight);

                    string val = "";
                    Color textColor = Color.WhiteSmoke;

                    if (i < _rows.Count && _rows[i].ContainsKey(col.Name))
                    {
                        val = _rows[i][col.Name];
                    }
                    else
                    {
                        var args = new CellValueNeededEventArgs(i, c);
                        CellValueNeeded?.Invoke(this, args);
                        if (args.Value != null) val = args.Value.ToString();
                        if (!args.TextColor.IsEmpty) textColor = args.TextColor;
                    }

                    TextRenderer.DrawText(g, val, this.Font, cellRect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis);
                    g.DrawLine(Pens.DimGray, cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom);
                    x += col.Width;
                }
                g.DrawLine(Pens.DimGray, rowRect.Left, rowRect.Bottom, rowRect.Right, rowRect.Bottom);
            }
        }

        private void UpdateScroll()
        {
            int totalRows = this.RowCount;
            int visibleRows = GetVisibleRowCount();
            if (totalRows > visibleRows)
            {
                _vScrollBar.Visible = true;
                _vScrollBar.Maximum = totalRows - 1;
                _vScrollBar.LargeChange = visibleRows;
            }
            else
            {
                _vScrollBar.Visible = false;
                _vScrollBar.Value = 0;
            }
        }

        private int GetVisibleRowCount()
        {
            return Math.Max(1, (this.Height - _headerHeight) / _rowHeight);
        }

        private void OnScroll(object sender, ScrollEventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScroll();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_vScrollBar.Visible)
            {
                int newValue = _vScrollBar.Value - (e.Delta / 120) * 3;
                int maxVal = Math.Max(0, _vScrollBar.Maximum - _vScrollBar.LargeChange + 1);
                newValue = Math.Max(0, Math.Min(newValue, maxVal));
                _vScrollBar.Value = newValue;
                this.Invalidate();
            }
            base.OnMouseWheel(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int y = e.Y - _headerHeight;
            if (y < 0) return;
            int clickedRowIndex = _vScrollBar.Value + (y / _rowHeight);
            if (clickedRowIndex >= 0 && clickedRowIndex < this.RowCount)
            {
                _selectedRowIndex = clickedRowIndex;
                this.Invalidate();
                CellClick?.Invoke(this, new DataGridViewCellEventArgs(0, clickedRowIndex));
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int y = e.Y - _headerHeight;
            if (y < 0) return;
            int clickedRowIndex = _vScrollBar.Value + (y / _rowHeight);
            if (clickedRowIndex >= 0 && clickedRowIndex < this.RowCount)
            {
                CellDoubleClick?.Invoke(this, new DataGridViewCellEventArgs(0, clickedRowIndex));
            }
        }
    }

    public class CellValueNeededEventArgs : EventArgs
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public object Value { get; set; }
        public Color TextColor { get; set; } = Color.Empty;

        public CellValueNeededEventArgs(int r, int c)
        {
            RowIndex = r;
            ColumnIndex = c;
        }
    }
}
