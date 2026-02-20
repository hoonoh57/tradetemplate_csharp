using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        // 필터 관련
        private ToolStrip _toolbar;
        private ToolStripButton _btnFilter;
        private ToolStripTextBox _txtThreshold;
        private bool _isFilterEnabled = false;
        private double _filterThreshold = 0.0;

        // 전체 종목 데이터 캐시
        private readonly List<string> _allCodes = new List<string>();
        private readonly Dictionary<string, (string name, string price)> _initialData = new Dictionary<string, (string, string)>();
        private readonly Dictionary<string, MarketData> _marketDataMap = new Dictionary<string, MarketData>();
        private readonly Dictionary<string, string> _stockNameMap = new Dictionary<string, string>(); // 코드 -> 종목명 매핑 캐시

        // 종목별 체결건수 추적용
        private class TickTracker
        {
            public DateTime BarStart;       // 현재 봉 시작 시각
            public int CurrentBarTicks;     // 현재 봉의 양봉 체결건수 합계
            public int MaxDayTicks;         // 당일 최대 체결건수
            public int LastPrice;           // 마지막 체결가 (양봉 판정용)
        }
        private readonly Dictionary<string, TickTracker> _tickTrackers = new Dictionary<string, TickTracker>();

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
            _grid.AddColumn("Score",    "점수",     60);
            _grid.AddColumn("Rank",     "순위",     50);
            _grid.AddColumn("Signal",   "신호",     50);

            _grid.CellValueNeeded += (s, e) => { /* virtual mode not used here yet */ };
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            SetupToolbar();

            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_grid);
            panel.Controls.Add(_toolbar);
            this.Controls.Add(panel);
        }

        private void SetupToolbar()
        {
            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.White,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.Professional
            };

            _btnFilter = new ToolStripButton("필터 적용")
            {
                CheckOnClick = true,
                ForeColor = Color.Silver,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = SystemIcons.Question.ToBitmap() // Placeholder
            };
            _btnFilter.CheckedChanged += (s, e) => {
                _isFilterEnabled = _btnFilter.Checked;
                _btnFilter.ForeColor = _isFilterEnabled ? Color.Yellow : Color.Silver;
                RefreshGrid();
            };

            _toolbar.Items.Add(_btnFilter);
            _toolbar.Items.Add(new ToolStripLabel(" 등락% ≥") { ForeColor = Color.LightGray });

            _txtThreshold = new ToolStripTextBox
            {
                Text = "0.00",
                Width = 50,
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.Lime,
                TextBoxTextAlign = HorizontalAlignment.Center
            };
            _txtThreshold.TextChanged += (s, e) => {
                if (double.TryParse(_txtThreshold.Text, out double val))
                {
                    _filterThreshold = val;
                    if (_isFilterEnabled) RefreshGrid();
                }
            };
            _toolbar.Items.Add(_txtThreshold);
        }

        private void RefreshGrid()
        {
            _grid.ClearRows();
            _stockRowMap.Clear();

            foreach (var code in _allCodes)
            {
                bool hasMD = _marketDataMap.TryGetValue(code, out var md);
                double change = hasMD ? Math.Abs(md.ChangeRate) : 0;

                if (_isFilterEnabled && change < _filterThreshold)
                    continue;

                var rowData = new Dictionary<string, string>
                {
                    ["Code"] = code,
                    ["Name"] = _initialData.ContainsKey(code) ? _initialData[code].name : "",
                    ["Price"] = _initialData.ContainsKey(code) ? _initialData[code].price : "0"
                };

                int rowIdx = _grid.Rows.Count;
                _grid.AddRow(rowData);
                _stockRowMap[code] = rowIdx;

                if (hasMD)
                {
                    // 기존 데이터가 있으면 업데이트 (UI 갱신을 위해 직접 호출 보다는 UpdateMarketData 재사용 구조가 좋으나 여기선 즉시 반영)
                    UpdateRowValues(rowIdx, md);
                }
            }
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

            _allCodes.Clear();
            _initialData.Clear();
            _marketDataMap.Clear();

            var items = payload.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var fields = item.Split('|');
                if (fields.Length < 2) continue;

                string code = fields[0];
                string name = fields.Length > 1 ? fields[1] : "";
                string price = fields.Length > 2 ? fields[2] : "";

                _allCodes.Add(code);
                _initialData[code] = (name, price);
                _stockNameMap[code] = name; // 이름 캐시 업데이트

                // 실시간 등록
                _ = _mainForm?.SubscribeRealtimeAsync(code);
            }

            RefreshGrid();
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
            if (fields.Length < 2) return;

            string code = fields[0];
            string type = fields[1]; // "I": 편입, "D": 이탈
            string conditionName = fields.Length > 2 ? fields[2] : "";

            if (type == "I")
            {
                if (!_allCodes.Contains(code))
                {
                    _allCodes.Add(code);
                    
                    // 이름 결정: 캐시에 있으면 사용, 없으면 코드로 표시 (조건명 사용 안함)
                    string displayName = _stockNameMap.TryGetValue(code, out var cachedName) ? cachedName : code;
                    _initialData[code] = (displayName, "0");
                    
                    RefreshGrid();
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

            _marketDataMap[md.Code] = md;

            bool isCurrentlyVisible = _stockRowMap.TryGetValue(md.Code, out int rowIdx);
            bool shouldBeVisible = !_isFilterEnabled || Math.Abs(md.ChangeRate) >= _filterThreshold;

            if (isCurrentlyVisible != shouldBeVisible)
            {
                RefreshGrid();
                return;
            }

            if (shouldBeVisible && isCurrentlyVisible)
            {
                UpdateRowValues(rowIdx, md);
            }
        }

        private void UpdateRowValues(int rowIdx, MarketData md)
        {
            _grid.UpdateRow(rowIdx, "Price", md.Price.ToString("N0"));

            // 전일대비등락률(00.00%) 형식 (부호 포함, 자릿수 고정)
            _grid.UpdateRow(rowIdx, "Change", md.ChangeRate.ToString("+00.00;-00.00;00.00") + "%");

            // 거래량
            _grid.UpdateRow(rowIdx, "Volume", md.AccVolume.ToString("N0"));

            // 시가 대비
            if (md.Open > 0)
            {
                double openDiff = (double)(md.Price - md.Open) / md.Open * 100.0;
                _grid.UpdateRow(rowIdx, "OpenDiff", openDiff.ToString("+0.00;-0.00;0.00") + "%");
            }

            // 체결건수 (현재 분봉 시간구간의 양봉 체결건수 합계)
            if (!_tickTrackers.TryGetValue(md.Code, out var tracker))
            {
                tracker = new TickTracker
                {
                    BarStart = new DateTime(md.Time.Year, md.Time.Month, md.Time.Day, md.Time.Hour, md.Time.Minute, 0),
                    CurrentBarTicks = 0,
                    MaxDayTicks = 0,
                    LastPrice = md.Price
                };
                _tickTrackers[md.Code] = tracker;
            }

            var barStart = new DateTime(md.Time.Year, md.Time.Month, md.Time.Day, md.Time.Hour, md.Time.Minute, 0);
            if (barStart > tracker.BarStart)
            {
                // 새 봉으로 전환: 이전 봉의 체결건수를 최대값에 반영 후 리셋
                if (tracker.CurrentBarTicks > tracker.MaxDayTicks)
                    tracker.MaxDayTicks = tracker.CurrentBarTicks;
                tracker.BarStart = barStart;
                tracker.CurrentBarTicks = 0;
            }

            // 체결건수 누적 (사용자 로직: 1틱 체결 = 1건 증가)
            // 역사 데이터(120틱 캔들 등)와의 동기화를 위해 모든 체결을 누적
            if (md.Volume > 0)
            {
                tracker.CurrentBarTicks += 1;
            }
            tracker.LastPrice = md.Price;

            _grid.UpdateRow(rowIdx, "TickCnt", tracker.CurrentBarTicks.ToString("N0"));

            // 틱비율 (당일 최고 체결건수 대비 현재 체결건수 비율)
            int maxTicks = Math.Max(tracker.MaxDayTicks, tracker.CurrentBarTicks); // 실시간 현재봉도 포함
            double tickRatio = maxTicks > 0 ? (double)tracker.CurrentBarTicks / maxTicks * 100.0 : 0;
            _grid.UpdateRow(rowIdx, "TickRat", tickRatio.ToString("F0") + "%");

            // VI 가격 (정적VI: 시가 대비 ±10%)
            if (md.Open > 0)
            {
                int viUpPrice = (int)(md.Open * 1.10);
                _grid.UpdateRow(rowIdx, "VIPrice", viUpPrice.ToString("N0"));

                // VI 이격률 (현재가 → VI가격까지 남은 비율)
                double viDist = (double)(viUpPrice - md.Price) / md.Price * 100.0;
                _grid.UpdateRow(rowIdx, "VIDist", viDist.ToString("F1") + "%");
            }

            // [지능형 에이전트 연동] 점수 및 순위 표시
            var agent = App64.Agents.CoordinatorAgent.Instance;
            double score = agent.GetScore(md.Code);
            int rank = agent.GetRank(md.Code);

            _grid.UpdateRow(rowIdx, "Score", score.ToString("F1"));
            _grid.UpdateRow(rowIdx, "Rank", rank > 0 ? rank.ToString() : "-");

            // 신호: 체결건수 > 10 AND 틱비율 > 50% AND 에이전트 점수 > 70
            if (tracker.CurrentBarTicks > 10 && tickRatio > 50 && score > 70)
                _grid.UpdateRow(rowIdx, "Signal", "★");
            else
                _grid.UpdateRow(rowIdx, "Signal", "");
        }

        // ── 기존 OnTickData 호환 (deprecated – Phase 3에서 제거) ──
        public void OnTickData(string payload)
        {
            // MarketDataService.OnMarketDataUpdated → UpdateMarketData로 대체됨
        }

        public void ClearAll()
        {
            _allCodes.Clear();
            _initialData.Clear();
            _marketDataMap.Clear();
            _grid.ClearRows();
            _stockRowMap.Clear();
            _tickTrackers.Clear();
        }
    }
}
