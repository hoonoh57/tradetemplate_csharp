using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using App64.Controls;

namespace App64.Forms
{
    public class DataViewForm : Form
    {
        private FastGrid _grid;
        private string _stockCode;

        public DataViewForm(string stockCode, string stockName, List<FastChart.OHLCV> data, List<FastChart.CustomSeries> seriesList)
        {
            _stockCode = stockCode;
            this.Text = $"데이터 보기 - [{_stockCode}] {stockName} (총 {data.Count}개 데이터)";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.Black;

            _grid = new FastGrid { Dock = DockStyle.Fill };
            this.Controls.Add(_grid);

            InitializeGrid(data, seriesList);
        }

        private void InitializeGrid(List<FastChart.OHLCV> data, List<FastChart.CustomSeries> seriesList)
        {
            _grid.AddColumn("Index", "No", 50);
            _grid.AddColumn("Time", "시간", 150);
            _grid.AddColumn("Open", "시가", 80);
            _grid.AddColumn("High", "고가", 80);
            _grid.AddColumn("Low", "저가", 80);
            _grid.AddColumn("Close", "종가", 80);
            _grid.AddColumn("Volume", "거래량", 100);
            _grid.AddColumn("TickCount", "틱( Raw)", 80);

            // 지표 추가
            foreach (var s in seriesList)
            {
                if (string.IsNullOrEmpty(s.Title)) continue;
                _grid.AddColumn(s.SeriesName, s.Title, 100);
            }

            // 데이터 채우기 (최신 데이터가 위로 오게 역순으로 하면 보기 편함)
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var d = data[i];
                var row = new Dictionary<string, string>();
                row["Index"] = i.ToString();
                row["Time"] = d.DateVal.ToString("yyyy-MM-dd HH:mm:ss");
                row["Open"] = d.Open.ToString("N0");
                row["High"] = d.High.ToString("N0");
                row["Low"] = d.Low.ToString("N0");
                row["Close"] = d.Close.ToString("N0");
                row["Volume"] = d.Volume.ToString("N0");
                row["TickCount"] = d.TickCount.ToString("N0");

                foreach (var s in seriesList)
                {
                    if (string.IsNullOrEmpty(s.Title)) continue;
                    if (i < s.Values.Count)
                    {
                        double val = s.Values[i];
                        row[s.SeriesName] = val.ToString("N3");
                    }
                    else
                    {
                        row[s.SeriesName] = "-";
                    }
                }

                _grid.AddRow(row);
            }
        }
    }
}
