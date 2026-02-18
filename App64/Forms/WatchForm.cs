using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Common.Models;
using WeifenLuo.WinFormsUI.Docking;

namespace App64.Forms
{
    public class WatchForm : DockContent
    {
        private readonly MainForm _mainForm;
        private readonly Controls.FastGrid _grid;
        private readonly Dictionary<string, int> _stockRowMap = new Dictionary<string, int>();

        public WatchForm() : this(null) { }

        public WatchForm(MainForm mainForm)
        {
            _mainForm = mainForm;

            this.Text = "종목감시";
            this.DockAreas = DockAreas.DockLeft | DockAreas.DockRight
                           | DockAreas.Float | DockAreas.Document;
            this.ShowHint = DockState.DockLeft;

            _grid = new Controls.FastGrid
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.FromArgb(200, 220, 200),
                Font = new Font("Consolas", 9f)
            };

            _grid.AddColumn("Code",     "코드",     70);
            _grid.AddColumn("Name",     "종목명",   100);
            _grid.AddColumn("Price",    "현재가",   80);
            _grid.AddColumn("Change",   "등락%",    60);
            _grid.AddColumn("Volume",   "거래량",   80);
            _grid.AddColumn("OpenDiff", "시가대비", 70);
            _grid.AddColumn("TickCnt",  "체결건수", 80);
            _grid.AddColumn("TickRat",  "틱비율",   70);
            _grid.AddColumn("VIPrice",  "VI가격",   80);
            _grid.AddColumn("VIDist",   "VI%",      60);
            _grid.AddColumn("Signal",   "신호",     50);

            _grid.CellDoubleClick += Grid_CellDoubleClick;
            this.Controls.Add(_grid);
        }

        // ── 더블클릭 → 차트 열기 ──
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var code = _grid.GetCellValue(e.RowIndex, "Code");
            var name = _grid.GetCellValue(e.RowIndex, "Name");
            if (!string.IsNullOrEmpty(code))
                _mainForm?.ShowChart(code, name ?? code);
        }

        // ── 조건검색 결과 수신 ──
        public void OnConditionResult(string payload)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnConditionResult), payload);
                return;
            }

            _grid.ClearRows();
            _stockRowMap.Clear();

            var items = payload.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var fields = item.Split('|');
                if (fields.Length < 2) continue;

                var rowData = new Dictionary<string, string>
                {
                    ["Code"] = fields[0],
                    ["Name"] = fields.Length > 1 ? fields[1] : "",
                    ["Price"] = fields.Length > 2 ? fields[2] : ""
                };

                int rowIdx = _grid.Rows.Count;
                _grid.AddRow(rowData);
                _stockRowMap[fields[0]] = rowIdx;

                // 실시간 등록
                _ = _mainForm?.SubscribeRealtimeAsync(fields[0]);
            }
        }
        // ── 실시간 조건 편입/이탈 ──
        public void OnConditionRealtime(string payload)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnConditionRealtime), payload);
                return;
            }

            var fields = payload.Split('|');
            if (fields.Length < 4) return;

            string code = fields[0];
            string type = fields[1]; // "I": 편입, "D": 이탈
            string name = fields[2];

            if (type == "I")
            {
                if (!_stockRowMap.ContainsKey(code))
                {
                    var rowData = new Dictionary<string, string>
                    {
                        ["Code"] = code,
                        ["Name"] = name,
                        ["Price"] = "0"
                    };
                    int rowIdx = _grid.Rows.Count;
                    _grid.AddRow(rowData);
                    _stockRowMap[code] = rowIdx;
                    _ = _mainForm?.SubscribeRealtimeAsync(code);
                }
            }
            else if (type == "D")
            {
                // 이탈 시 그리드에서 즉시 삭제할 수도 있고, 색만 바꿀 수도 있음
                // 여기서는 일단 로그 위주로 하고 그리드에서는 유지하거나,
                // 필요시 Filter 등 구현 예정
            }
        }

        // ── MarketData 실시간 업데이트 ──
        public void UpdateMarketData(MarketData md)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<MarketData>(UpdateMarketData), md);
                return;
            }

            if (!_stockRowMap.TryGetValue(md.Code, out int rowIdx)) return;
            if (rowIdx >= _grid.Rows.Count) return;

            _grid.UpdateRow(rowIdx, "Price", md.Price.ToString("N0"));

            double changeRate = md.ChangeRate;
            string sign = changeRate >= 0 ? "+" : "";
            _grid.UpdateRow(rowIdx, "Change", sign + changeRate.ToString("F2") + "%");
            _grid.UpdateRow(rowIdx, "Volume", md.AccVolume.ToString("N0"));

            // 시가 대비
            if (md.Open > 0)
            {
                double openDiff = (double)(md.Price - md.Open) / md.Open * 100.0;
                _grid.UpdateRow(rowIdx, "OpenDiff", openDiff.ToString("+0.00;-0.00;0.00") + "%");
            }
        }

        // ── 기존 OnTickData 호환 (deprecated – Phase 3에서 제거) ──
        public void OnTickData(string payload)
        {
            // MarketDataService.OnMarketDataUpdated → UpdateMarketData로 대체됨
        }

        public void ClearAll()
        {
            _grid.Rows.Clear();
            _stockRowMap.Clear();
        }
    }
}
