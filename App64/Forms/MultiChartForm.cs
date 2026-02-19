using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common.Enums;
using Common.Models;
using App64.Controls;
using App64.Services;
using WeifenLuo.WinFormsUI.Docking;

namespace App64.Forms
{
    /// <summary>
    /// 키움 멀티차트 스타일 — NxM 그리드에 여러 종목을 동시에 표시.
    /// 대장주 추종 자동매매/수동매매를 지원합니다.
    /// </summary>
    public class MultiChartForm : DockContent
    {
        // ── 서비스 ──
        private readonly CandleService _candleService;
        private readonly OrderService _orderService;

        // ── 레이아웃 ──
        private int _rows = 3, _cols = 3;
        private TableLayoutPanel _grid;
        private ToolStrip _toolbar;

        // ── 차트 패널 목록 ──
        private List<ChartPanel> _panels = new List<ChartPanel>();
        private ChartPanel _focusedPanel = null;

        // ── 상단 컨트롤 ──
        private ToolStripTextBox _txtStockCode;
        private ToolStripComboBox _cmbTimeFrame;
        private ToolStripComboBox _cmbLayout;
        private ToolStripLabel _lblFocusInfo;

        // ── 전략/매매 설정 ──
        private string _globalStrategy = "없음";
        private CandleType _globalTimeFrame = CandleType.Minute;
        private int _globalInterval = 1; // 분봉 간격

        // ═══════════════════════════════════════════
        //  내부 클래스: 개별 차트 패널
        // ═══════════════════════════════════════════
        private class ChartPanel : Panel
        {
            public FastChart Chart { get; private set; }
            public string StockCode { get; set; } = "";
            public string StockName { get; set; } = "";
            public bool StrategyEnabled { get; set; } = false;
            public bool AutoTradeEnabled { get; set; } = false;

            private Panel _header;
            private Label _lblTitle;
            private Label _lblSignal; // 신호 표시용 (★)
            private Label _lblStrategy;
            private Label _lblAutoTrade;
            private CheckBox _chkStrategy;
            private CheckBox _chkAutoTrade;

            public event Action<ChartPanel> OnFocused;
            public event Action<ChartPanel, string> OnStockChanged;

            public ChartPanel()
            {
                this.BackColor = Color.FromArgb(18, 18, 28);
                this.BorderStyle = BorderStyle.FixedSingle;
                this.Padding = new Padding(0);

                // ── 헤더 바 ──
                _header = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 24,
                    BackColor = Color.FromArgb(30, 30, 45),
                    Cursor = Cursors.Hand
                };
                _header.Click += (s, e) => OnFocused?.Invoke(this);

                _lblTitle = new Label
                {
                    Text = "(빈 차트)",
                    ForeColor = Color.White,
                    Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),
                    Location = new Point(4, 3),
                    AutoSize = true
                };
                _lblTitle.Click += (s, e) => OnFocused?.Invoke(this);

                _chkStrategy = new CheckBox
                {
                    Text = "전략",
                    ForeColor = Color.FromArgb(100, 200, 100),
                    BackColor = Color.Transparent,
                    Font = new Font("맑은 고딕", 7.5f),
                    AutoSize = true,
                    Checked = false
                };
                _chkStrategy.CheckedChanged += (s, e) =>
                {
                    StrategyEnabled = _chkStrategy.Checked;
                    _chkStrategy.ForeColor = StrategyEnabled ? Color.LimeGreen : Color.Gray;
                };

                _lblSignal = new Label
                {
                    Text = "",
                    ForeColor = Color.Yellow,
                    Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };

                _chkAutoTrade = new CheckBox
                {
                    Text = "자동",
                    ForeColor = Color.FromArgb(200, 100, 100),
                    BackColor = Color.Transparent,
                    Font = new Font("맑은 고딕", 7.5f),
                    AutoSize = true,
                    Checked = false
                };
                _chkAutoTrade.CheckedChanged += (s, e) =>
                {
                    AutoTradeEnabled = _chkAutoTrade.Checked;
                    _chkAutoTrade.ForeColor = AutoTradeEnabled ? Color.Tomato : Color.Gray;
                };

                _header.Controls.Add(_lblTitle);
                _header.Controls.Add(_lblSignal);
                _header.Controls.Add(_chkAutoTrade);
                _header.Controls.Add(_chkStrategy);

                // ── 차트 ──
                Chart = new FastChart
                {
                    Dock = DockStyle.Fill
                };
                Chart.Click += (s, e) => OnFocused?.Invoke(this);

                this.Controls.Add(Chart);
                this.Controls.Add(_header);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                // 체크박스 우측 정렬
                if (_chkAutoTrade != null && _header != null)
                {
                    _chkAutoTrade.Location = new Point(_header.Width - _chkAutoTrade.Width - 4, 3);
                    _chkStrategy.Location = new Point(_chkAutoTrade.Left - _chkStrategy.Width - 4, 3);
                    _lblSignal.Location = new Point(_chkStrategy.Left - _lblSignal.Width - 8, 1);
                }
            }

            public void SetFocused(bool focused)
            {
                _header.BackColor = focused
                    ? Color.FromArgb(50, 50, 100)
                    : Color.FromArgb(30, 30, 45);
                this.BorderStyle = focused ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
            }

            public void UpdateTitle()
            {
                if (string.IsNullOrEmpty(StockCode))
                    _lblTitle.Text = "(빈 차트)";
                else
                    _lblTitle.Text = $"{StockName} ({StockCode})";
            }

            public void SetStock(string code, string name)
            {
                StockCode = code;
                StockName = name;
                UpdateTitle();
                SetSignal(false); // 종목 변경 시 신호 초기화
            }

            public void SetSignal(bool active)
            {
                _lblSignal.Text = active ? "★" : "";
            }
        }

        // ═══════════════════════════════════════════
        //  생성자
        // ═══════════════════════════════════════════
        public MultiChartForm(CandleService candleService, OrderService orderService = null)
        {
            _candleService = candleService;
            _orderService = orderService;

            this.Text = "멀티차트";
            this.DockAreas = DockAreas.Document | DockAreas.Float;
            this.ShowHint = DockState.Document;

            SetupToolbar();
            SetupGrid(_rows, _cols);
        }

        private void SetupToolbar()
        {
            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(25, 25, 38),
                ForeColor = Color.White,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.Professional
            };

            // ── 레이아웃 선택 ──
            _toolbar.Items.Add(new ToolStripLabel("배치:") { ForeColor = Color.Gray });
            _cmbLayout = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.White
            };
            _cmbLayout.Items.AddRange(new object[] { "1×1", "1×2", "2×2", "2×3", "3×3", "3×4", "4×4" });
            _cmbLayout.SelectedIndex = 4; // 3×3 기본
            _cmbLayout.SelectedIndexChanged += (s, e) => ApplyLayout();
            _toolbar.Items.Add(_cmbLayout);

            _toolbar.Items.Add(new ToolStripSeparator());

            // ── 타임프레임 ──
            _toolbar.Items.Add(new ToolStripLabel("시간:") { ForeColor = Color.Gray });
            _cmbTimeFrame = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.White
            };
            _cmbTimeFrame.Items.AddRange(new object[] { "1분", "3분", "5분", "15분", "30분", "60분" });
            _cmbTimeFrame.SelectedIndex = 0;
            _cmbTimeFrame.SelectedIndexChanged += (s, e) => ApplyTimeFrame();
            _toolbar.Items.Add(_cmbTimeFrame);

            _toolbar.Items.Add(new ToolStripSeparator());

            // ── 종목코드 입력 (포커스된 차트에 적용) ──
            _toolbar.Items.Add(new ToolStripLabel("종목:") { ForeColor = Color.Gray });
            _txtStockCode = new ToolStripTextBox
            {
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.White,
                Font = new Font("Consolas", 10f),
                Size = new Size(80, 25)
            };
            _txtStockCode.KeyDown += TxtStockCode_KeyDown;
            _toolbar.Items.Add(_txtStockCode);

            var btnApply = new ToolStripButton("적용")
            {
                ForeColor = Color.Cyan,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            btnApply.Click += async (s, e) => await ApplyStockCodeAsync();
            _toolbar.Items.Add(btnApply);

            _toolbar.Items.Add(new ToolStripSeparator());

            // ── 일괄 지표 삽입 ──
            var btnIndicator = new ToolStripDropDownButton("지표삽입")
            {
                ForeColor = Color.FromArgb(255, 200, 100),
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            btnIndicator.DropDownItems.Add("MA (전체)", null, (s, e) => ApplyIndicatorAll("MA"));
            btnIndicator.DropDownItems.Add("RSI (전체)", null, (s, e) => ApplyIndicatorAll("RSI"));
            btnIndicator.DropDownItems.Add("MACD (전체)", null, (s, e) => ApplyIndicatorAll("MACD"));
            btnIndicator.DropDownItems.Add("SuperTrend (전체)", null, (s, e) => ApplyIndicatorAll("SuperTrend"));
            btnIndicator.DropDownItems.Add(new ToolStripSeparator());
            btnIndicator.DropDownItems.Add("틱강도 ÷120 (전체)", null, (s, e) => ApplyTickIntensityAll(120));
            btnIndicator.DropDownItems.Add("틱강도 ÷60 (전체)", null, (s, e) => ApplyTickIntensityAll(60));
            btnIndicator.DropDownItems.Add(new ToolStripSeparator());
            btnIndicator.DropDownItems.Add("지표 전체 삭제", null, (s, e) => ClearIndicatorsAll());
            _toolbar.Items.Add(btnIndicator);

            _toolbar.Items.Add(new ToolStripSeparator());

            // ── 전략 일괄 적용 ──
            var btnStrategy = new ToolStripDropDownButton("전략")
            {
                ForeColor = Color.LimeGreen,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            btnStrategy.DropDownItems.Add("전체 전략 ON", null, (s, e) => SetStrategyAll(true));
            btnStrategy.DropDownItems.Add("전체 전략 OFF", null, (s, e) => SetStrategyAll(false));
            btnStrategy.DropDownItems.Add(new ToolStripSeparator());
            btnStrategy.DropDownItems.Add("전체 자동매매 ON", null, (s, e) => SetAutoTradeAll(true));
            btnStrategy.DropDownItems.Add("전체 자동매매 OFF", null, (s, e) => SetAutoTradeAll(false));
            _toolbar.Items.Add(btnStrategy);

            // ── 포커스 정보 ──
            _lblFocusInfo = new ToolStripLabel("선택: 없음")
            {
                ForeColor = Color.FromArgb(180, 180, 255),
                Alignment = ToolStripItemAlignment.Right
            };
            _toolbar.Items.Add(_lblFocusInfo);

            this.Controls.Add(_toolbar);
        }

        // ═══════════════════════════════════════════
        //  그리드 레이아웃
        // ═══════════════════════════════════════════
        private void SetupGrid(int rows, int cols)
        {
            if (_grid != null)
            {
                this.Controls.Remove(_grid);
                _grid.Dispose();
            }

            _rows = rows;
            _cols = cols;

            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 22),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                ColumnCount = cols,
                RowCount = rows,
                Padding = new Padding(1)
            };

            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();
            for (int c = 0; c < cols; c++)
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++)
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            // 기존 패널의 종목코드 보존
            var oldCodes = _panels.Select(p => new { p.StockCode, p.StockName }).ToList();
            _panels.Clear();

            int total = rows * cols;
            for (int i = 0; i < total; i++)
            {
                var panel = new ChartPanel { Dock = DockStyle.Fill };
                panel.OnFocused += OnPanelFocused;

                // 기존 종목코드 복원
                if (i < oldCodes.Count && !string.IsNullOrEmpty(oldCodes[i].StockCode))
                {
                    panel.SetStock(oldCodes[i].StockCode, oldCodes[i].StockName);
                }

                _panels.Add(panel);
                _grid.Controls.Add(panel, i % cols, i / cols);
            }

            this.Controls.Add(_grid);
            _grid.BringToFront();

            // 기존 종목코드가 있으면 데이터 다시 로드
            foreach (var p in _panels.Where(p => !string.IsNullOrEmpty(p.StockCode)))
            {
                _ = LoadChartDataAsync(p);
            }

            if (_panels.Count > 0)
                OnPanelFocused(_panels[0]);
        }

        private void ApplyLayout()
        {
            string selected = _cmbLayout.SelectedItem?.ToString() ?? "3×3";
            switch (selected)
            {
                case "1×1": SetupGrid(1, 1); break;
                case "1×2": SetupGrid(1, 2); break;
                case "2×2": SetupGrid(2, 2); break;
                case "2×3": SetupGrid(2, 3); break;
                case "3×3": SetupGrid(3, 3); break;
                case "3×4": SetupGrid(3, 4); break;
                case "4×4": SetupGrid(4, 4); break;
            }
        }

        // ═══════════════════════════════════════════
        //  포커스 처리
        // ═══════════════════════════════════════════
        private void OnPanelFocused(ChartPanel panel)
        {
            _focusedPanel = panel;
            foreach (var p in _panels)
                p.SetFocused(p == panel);

            _txtStockCode.Text = panel.StockCode;
            _lblFocusInfo.Text = string.IsNullOrEmpty(panel.StockCode)
                ? "선택: (빈 차트)"
                : $"선택: {panel.StockName} ({panel.StockCode})";
        }

        // ═══════════════════════════════════════════
        //  종목코드 변경
        // ═══════════════════════════════════════════
        private void TxtStockCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = ApplyStockCodeAsync();
            }
        }

        private async Task ApplyStockCodeAsync()
        {
            if (_focusedPanel == null) return;
            string code = _txtStockCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            _focusedPanel.SetStock(code, code); // 이름은 나중에 업데이트
            await LoadChartDataAsync(_focusedPanel);
        }

        private async Task LoadChartDataAsync(ChartPanel panel)
        {
            if (_candleService == null || string.IsNullOrEmpty(panel.StockCode)) return;

            try
            {
                var candles = await _candleService.GetCandlesAsync(
                    panel.StockCode, _globalTimeFrame, _globalInterval, 900);

                if (candles != null && candles.Count > 0)
                {
                    panel.Chart.LoadStockData(panel.StockCode, panel.StockName, candles.ToList());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MultiChart] 캔들 로드 실패 ({panel.StockCode}): {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════
        //  일괄 종목 설정 (외부에서 여러 종목을 한번에 넣기)
        // ═══════════════════════════════════════════
        public async Task SetStocksAsync(List<(string code, string name)> stocks)
        {
            for (int i = 0; i < Math.Min(stocks.Count, _panels.Count); i++)
            {
                _panels[i].SetStock(stocks[i].code, stocks[i].name);
            }

            // 병렬 데이터 로드
            var tasks = _panels
                .Where(p => !string.IsNullOrEmpty(p.StockCode))
                .Select(p => LoadChartDataAsync(p));
            await Task.WhenAll(tasks);
        }

        // ═══════════════════════════════════════════
        //  타임프레임 일괄 변경
        // ═══════════════════════════════════════════
        private async void ApplyTimeFrame()
        {
            string selected = _cmbTimeFrame.SelectedItem?.ToString() ?? "1분";
            switch (selected)
            {
                case "1분": _globalInterval = 1; break;
                case "3분": _globalInterval = 3; break;
                case "5분": _globalInterval = 5; break;
                case "15분": _globalInterval = 15; break;
                case "30분": _globalInterval = 30; break;
                case "60분": _globalInterval = 60; break;
            }
            _globalTimeFrame = CandleType.Minute;

            // 모든 차트 리로드
            var tasks = _panels
                .Where(p => !string.IsNullOrEmpty(p.StockCode))
                .Select(p => LoadChartDataAsync(p));
            await Task.WhenAll(tasks);
        }

        // ═══════════════════════════════════════════
        //  지표 일괄 삽입
        // ═══════════════════════════════════════════
        private void ApplyIndicatorAll(string indicatorName)
        {
            foreach (var panel in _panels)
            {
                if (panel.Chart.Data == null || panel.Chart.Data.Count == 0) continue;

                FastChart.CustomSeries series = null;
                switch (indicatorName)
                {
                    case "MA":
                        series = IndicatorCalculation.CalculateMA(panel.Chart.Data, 20, SkiaSharp.SKColors.Yellow);
                        break;
                    case "RSI":
                        series = IndicatorCalculation.CalculateRSI(panel.Chart.Data, 14);
                        break;
                    case "MACD":
                        series = IndicatorCalculation.CalculateMACD(panel.Chart.Data);
                        break;
                    case "SuperTrend":
                        series = IndicatorCalculation.CalculateSuperTrend(panel.Chart.Data);
                        break;
                }
                if (series != null) panel.Chart.AddSeries(series);
            }
        }

        private void ApplyTickIntensityAll(int divisor)
        {
            foreach (var panel in _panels)
            {
                if (panel.Chart.Data == null || panel.Chart.Data.Count == 0) continue;
                panel.Chart.BuildTickIntensitySeries();
            }
        }

        private void ClearIndicatorsAll()
        {
            foreach (var panel in _panels)
            {
                panel.Chart.ClearIndicators();
            }
        }

        // ═══════════════════════════════════════════
        //  전략/자동매매 일괄 설정
        // ═══════════════════════════════════════════
        private void SetStrategyAll(bool enabled)
        {
            foreach (var panel in _panels)
                panel.StrategyEnabled = enabled;
        }

        private void SetAutoTradeAll(bool enabled)
        {
            foreach (var panel in _panels)
                panel.AutoTradeEnabled = enabled;
        }

        // ═══════════════════════════════════════════
        //  실시간 시세 수신
        // ═══════════════════════════════════════════
        public void OnMarketDataUpdated(MarketData md)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<MarketData>(OnMarketDataUpdated), md);
                return;
            }

            foreach (var panel in _panels)
            {
                if (panel.StockCode == md.Code)
                {
                    panel.Chart.UpdateRealtime((float)md.Price, md.Volume, md.Time);

                    // 자동매매 로직 (전략 ON + 자동매매 ON인 경우)
                    if (panel.StrategyEnabled && panel.AutoTradeEnabled)
                    {
                        EvaluateAutoTrade(panel, md);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        //  자동매매 판단 (기본 골격 — 추후 전략 확장)
        // ═══════════════════════════════════════════
        private void EvaluateAutoTrade(ChartPanel panel, MarketData md)
        {
            // 틱강도 데이터 기반 신호 평가
            // FastChart 내부의 _tickCounts와 _maxDayTicks를 활용 (Reflection 또는 Public Getter 필요하나 여기서는 단순화하여 내부 변수 유사 계산)
            
            var cntSeries = panel.Chart.GetSeries("TICK_CNT");
            var ratSeries = panel.Chart.GetSeries("TICK_RAT");

            if (cntSeries != null && ratSeries != null && cntSeries.Values.Count > 0)
            {
                double lastCnt = cntSeries.Values[cntSeries.Values.Count - 1] * 120; // 원래 체결건수로 복원 (기본 120기준)
                double lastRat = ratSeries.Values[ratSeries.Values.Count - 1];

                bool isSignal = lastCnt > 10 && lastRat > 50;
                panel.SetSignal(isSignal);

                if (isSignal && panel.AutoTradeEnabled)
                {
                    // TODO: 실제 주문 로직 호출
                    // _orderService?.SendOrderAsync(...)
                    // System.Diagnostics.Debug.WriteLine($"[자동매매] 신호 발생: {panel.StockCode} (건수:{lastCnt:N0}, 비율:{lastRat:F1}%)");
                }
            }
        }
    }
}
