using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace App64.Forms
{
    public class ChartForm : DockContent
    {
        public string StockCode { get; }
        public string StockName { get; }

        private readonly Controls.FastChart _chart;

        public ChartForm(string stockCode, string stockName)
        {
            StockCode = stockCode;
            StockName = stockName;

            this.Text = $"{stockName} ({stockCode})";
            this.DockAreas = DockAreas.Document | DockAreas.Float;
            this.ShowHint = DockState.Document;

            _chart = new Controls.FastChart
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(_chart);
        }

        public void LoadChartData(string code, string name, System.Collections.Generic.List<Common.Models.CandleData> candles)
        {
            _chart.LoadStockData(code, name, candles);
        }
    }
}
