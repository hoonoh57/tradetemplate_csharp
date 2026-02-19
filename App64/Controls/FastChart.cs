using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Common.Models;
using Common.Enums;

namespace App64.Controls
{
    public class FastChart : UserControl
    {
        // --- Events ---
        public event EventHandler<CandleClickEventArgs> CandleClicked;
        public event EventHandler<CrosshairEventArgs> CrosshairMoved;
        public event EventHandler ScaleChanged;
        public event EventHandler<string> IndicatorAddRequested;
        public event EventHandler<string> IndicatorDeleted;
        public event EventHandler<string> IndicatorSettingsRequested;
        public event EventHandler<CandleClickEventArgs> CandleDoubleClicked;
        public event EventHandler<string> LegendDoubleClicked;
        public event EventHandler<string> ComparisonSymbolRequested;

        #region Themes
        public class ChartTheme
        {
            public SKColor Background { get; set; }
            public SKColor Text { get; set; }
            public SKColor GridLine { get; set; }
            public SKColor CandleUp { get; set; }
            public SKColor CandleDown { get; set; }
            public SKColor Wick { get; set; }
            public SKColor Crosshair { get; set; }
            public SKColor CrosshairLabelBg { get; set; }
            public SKColor CrosshairLabelText { get; set; }
            public SKColor Border { get; set; }
            public SKColor VolumeUp { get; set; }
            public SKColor VolumeDown { get; set; }

            public static ChartTheme CreateDarkTheme()
            {
                return new ChartTheme
                {
                    Background = SKColor.Parse("#131722"),
                    Text = SKColor.Parse("#d1d4dc"),
                    GridLine = SKColor.Parse("#363c4e"),
                    CandleUp = SKColor.Parse("#089981"),
                    CandleDown = SKColor.Parse("#f23645"),
                    Wick = SKColor.Parse("#787b86"),
                    Crosshair = SKColor.Parse("#9598a1"),
                    CrosshairLabelBg = SKColor.Parse("#4c525e"),
                    CrosshairLabelText = SKColors.White,
                    Border = SKColor.Parse("#2a2e39"),
                    VolumeUp = SKColor.Parse("#089981").WithAlpha(128),
                    VolumeDown = SKColor.Parse("#f23645").WithAlpha(128)
                };
            }
        }
        #endregion

        #region Data Structures
        public struct OHLCV
        {
            public float Open;
            public float High;
            public float Low;
            public float Close;
            public long Volume;
            public DateTime DateVal;
            public string TimeStr;
        }

        public enum TimeFrame
        {
            Tick, Min1, Min3, Min5, Min10, Min15, Min30, Min60, Day, Week, Month
        }

        public class CandleClickEventArgs : EventArgs
        {
            public int Index { get; set; }
            public OHLCV Data { get; set; }
            public MouseButtons MouseButton { get; set; }
        }

        public class CrosshairEventArgs : EventArgs
        {
            public float Price { get; set; }
            public DateTime Time { get; set; }
            public int Index { get; set; }
        }

        public enum PanelType { Overlay, Bottom }
        public enum PlotType { Line, Histogram, Scatter, StepLine }
        public enum ShapeType { Circle, Square, TriangleUp, TriangleDown, Diamond, Cross }
        public enum LeftAxisModeType { None, VsPrevClose, VsTodayOpen }

        public class PlotConfig
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public string PanelName { get; set; }
            public Color Color { get; set; }
            public Color? ColorUp { get; set; }
            public Color? ColorDown { get; set; }
            public PlotType Type { get; set; }
            public float Thickness { get; set; }
            public double? BaseLine { get; set; }
            public double? Overbought { get; set; }
            public double? Oversold { get; set; }
        }

        public class CustomSeries
        {
            public string PanelName { get; set; }
            public string SeriesName { get; set; }
            public string Title { get; set; }
            public SKColor Color { get; set; }
            public List<double> Values { get; set; } = new List<double>();
            public PanelType PanelType { get; set; }
            public PlotType Style { get; set; } = PlotType.Line;
            public ShapeType Shape { get; set; } = ShapeType.Circle;
            public double? MinValue { get; set; }
            public double? MaxValue { get; set; }
            public float Thickness { get; set; } = 1.0f;
            public double? BaseLine { get; set; }
            public double? Overbought { get; set; }
            public double? Oversold { get; set; }
            public bool IsCustom { get; set; }
            public SKColor? ColorUp { get; set; }
            public SKColor? ColorDown { get; set; }
            public List<SKColor> OutputColors { get; set; } = new List<SKColor>();
            public List<double> TrendValues { get; set; }
            public bool IsLeftAxis { get; set; }
        }

        public struct SubChartInfo
        {
            public string PanelName;
            public SKRect Rect;
            public SKRect PriceRect;
        }

        private class LegendItem
        {
            public string PanelName { get; set; }
            public SKRect HitRect { get; set; }
            public string Text { get; set; }
            public SKColor Color { get; set; }
            public bool IsOverlay { get; set; }
        }

        public class SignalMarker
        {
            public DateTime Time { get; set; }
            public SignalType Type { get; set; }
            public double Price { get; set; }
            public string Note { get; set; }
        }

        public enum SignalType { Buy, Sell, ExitBuy, ExitSell }

        public class BreakoutBox
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public double Top { get; set; }
            public double Bottom { get; set; }
            public long UpVolume { get; set; }
            public long DownVolume { get; set; }
            public bool IsActive { get; set; }
            public bool IsBullishBreakout { get; set; }
            public bool IsBearishBreakout { get; set; }
        }

        public class HighlightZone
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public SKColor Color { get; set; }
            public string Note { get; set; }
        }
        #endregion

        #region Fields
        private SKControl skControl;
        private ChartTheme CurrentTheme = ChartTheme.CreateDarkTheme();
        public List<OHLCV> Data = new List<OHLCV>();
        public List<BreakoutBox> Boxes = new List<BreakoutBox>();
        private List<HighlightZone> _highlightZones = new List<HighlightZone>();
        private List<SignalMarker> _signals = new List<SignalMarker>();
        private List<CustomSeries> _customSeriesList = new List<CustomSeries>();
        private List<SubChartInfo> SubCharts = new List<SubChartInfo>();
        private List<LegendItem> _legendItems = new List<LegendItem>();

        private SKRect MainRect;
        private SKRect PriceRect;
        private SKRect TimeRect;
        private SKRect _leftAxisRect;

        private float _candleWidth = 8.0f;
        private float _gap = 2.0f;
        private float _scrollOffset = -1;
        private bool _autoScroll = true;
        private string _selectedSeriesName = "";
        private bool _isUpdating = false;

        private bool _isAutoScaleY = true;
        private LeftAxisModeType _leftAxisMode = LeftAxisModeType.None;
        private float _manualMaxP = 0;
        private float _manualMinP = 0;

        private float _mouseX = -1;
        private float _mouseY = -1;
        private bool _isDraggingChart = false;
        private bool _isDraggingPrice = false;
        private float _lastMouseX = 0;
        private float _lastMouseY = 0;

        private const float AxisWidth = 70.0f;
        private const float LeftAxisWidth = 60.0f;
        private const float AxisHeight = 25.0f;
        private const string MAIN_PANEL_NAME = "Main";

        private Dictionary<string, float> _panelWeights = new Dictionary<string, float>();
        private List<SplitterInfo> _splitterRects = new List<SplitterInfo>();
        private bool _isDraggingSplitter = false;
        private SplitterInfo? _activeSplitter = null;

        private struct SplitterInfo
        {
            public SKRect Rect;
            public string PanelAbove;
            public string PanelBelow;
        }

        private string _stockCode = "";
        private string _stockName = "";
        private TimeFrame _currentTimeFrame = TimeFrame.Min1;
        private string _comparisonSeriesName = "";

        // Toggle features
        private bool _showCurrentPriceLine = true;
        private bool _showOpenChangeRate = false;
        private bool _showViLine = false;

        // 틱강도(TickIntensity) 추적
        private List<int> _tickCounts = new List<int>();   // 봉별 양봉 체결건수
        private int _maxDayTicks = 0;                       // 당일 최대 체결건수
        private float _lastRealtimePrice = 0;               // 마지막 체결가 (양봉 판정)
        private int _tickDivisor = 120;                     // 체결건수 나누기 값 (기본 120)
        private bool _showTickIntensity = false;             // 틱강도 지표 표시 여부
        #endregion

        #region Properties
        public string StockCode { get => _stockCode; set => _stockCode = value; }
        public string StockName { get => _stockName; set => _stockName = value; }
        public TimeFrame CurrentTimeFrame { get => _currentTimeFrame; set => _currentTimeFrame = value; }
        public float CandleWidth { get => _candleWidth; set { _candleWidth = Math.Max(1.0f, Math.Min(50.0f, value)); RefreshChart(); } }
        public bool ShowDaySeparators { get; set; } = true;
        public double YesterdayClose { get; set; }
        public double TodayOpen { get; set; }
        public string StrategyName { get; set; }
        public string StrategyMode { get; set; }

        public ContextMenuStrip ChartMenu { get; private set; }
        #endregion

        public FastChart()
        {
            skControl = new SKControl();
            skControl.Dock = DockStyle.Fill;
            this.Controls.Add(skControl);

            InitializeContextMenu();

            skControl.PaintSurface += OnPaintSurface;
            skControl.Resize += (s, e) => { UpdateLayout(); RefreshChart(); };
            skControl.MouseDown += SKControl_MouseDown;
            skControl.MouseMove += SKControl_MouseMove;
            skControl.MouseUp += (s, e) => { _isDraggingChart = _isDraggingPrice = _isDraggingSplitter = false; _activeSplitter = null; };
            skControl.MouseWheel += OnWheel;
            skControl.DoubleClick += SKControl_DoubleClick;
            skControl.MouseLeave += (s, e) => { _mouseX = _mouseY = -1; RefreshChart(); };
            
            this.KeyDown += FastChart_KeyDown;
        }

        private void InitializeContextMenu()
        {
            ChartMenu = new ContextMenuStrip();
            var addMenu = new ToolStripMenuItem("지표삽입");
            addMenu.DropDownItems.Add("이동평균 (MA)", null, (s, e) => IndicatorAddRequested?.Invoke(this, "MA"));
            addMenu.DropDownItems.Add("SuperTrend", null, (s, e) => IndicatorAddRequested?.Invoke(this, "SuperTrend"));
            addMenu.DropDownItems.Add("RSI", null, (s, e) => IndicatorAddRequested?.Invoke(this, "RSI"));
            addMenu.DropDownItems.Add("MACD", null, (s, e) => IndicatorAddRequested?.Invoke(this, "MACD"));
            addMenu.DropDownItems.Add(new ToolStripSeparator());

            // 틱강도 지표 (나누기 값 서브메뉴)
            var tickMenu = new ToolStripMenuItem("틱강도 (TickIntensity)");
            foreach (int div in new[] { 5, 15, 30, 60, 120 })
            {
                int d = div;
                var item = new ToolStripMenuItem($"÷{d}", null, (s2, e2) => {
                    _tickDivisor = d;
                    _showTickIntensity = true;
                    BuildTickIntensitySeries();
                });
                if (d == 120) item.Checked = true;
                tickMenu.DropDownItems.Add(item);
            }
            addMenu.DropDownItems.Add(tickMenu);
            ChartMenu.Items.Add(addMenu);

            // 비교종목 삽입 (토글 방식)
            var compMenu = new ToolStripMenuItem("비교종목 삽입");
            var miKospi = new ToolStripMenuItem("지수비교 (KOSPI)");
            miKospi.Click += (s, e) => {
                var existing = _customSeriesList.FirstOrDefault(x => x.SeriesName == "COMP_KOSPI");
                if (existing != null)
                {
                    _customSeriesList.Remove(existing);
                    ((ToolStripMenuItem)s).Checked = false;
                    UpdateLayout(); RefreshChart();
                }
                else
                {
                    ((ToolStripMenuItem)s).Checked = true;
                    ComparisonSymbolRequested?.Invoke(this, "KOSPI");
                }
            };
            compMenu.DropDownItems.Add(miKospi);
            compMenu.DropDownItems.Add("직접 입력...", null, (s, e) => {
                ComparisonSymbolRequested?.Invoke(this, "DIALOG");
            });
            compMenu.DropDownItems.Add(new ToolStripSeparator());
            compMenu.DropDownItems.Add("비교종목 제거", null, (s, e) => {
                _customSeriesList.RemoveAll(x => x.SeriesName.StartsWith("COMP_"));
                UpdateLayout(); RefreshChart();
            });
            ChartMenu.Items.Add(compMenu);

            ChartMenu.Items.Add(new ToolStripSeparator());

            // 현재가 점선 토글
            var miPriceLine = new ToolStripMenuItem("현재가 라인 표시");
            miPriceLine.Checked = _showCurrentPriceLine;
            miPriceLine.Click += (s, e) => { _showCurrentPriceLine = !_showCurrentPriceLine; ((ToolStripMenuItem)s).Checked = _showCurrentPriceLine; RefreshChart(); };
            ChartMenu.Items.Add(miPriceLine);

            // 시가대비 상승률 좌축 토글
            var miOpenRate = new ToolStripMenuItem("시가대비 상승률 (좌축)");
            miOpenRate.Checked = _showOpenChangeRate;
            miOpenRate.Click += (s, e) => {
                _showOpenChangeRate = !_showOpenChangeRate;
                ((ToolStripMenuItem)s).Checked = _showOpenChangeRate;
                if (!_showOpenChangeRate && _leftAxisMode == LeftAxisModeType.VsTodayOpen)
                    _leftAxisMode = LeftAxisModeType.None;
                else if (_showOpenChangeRate)
                    _leftAxisMode = LeftAxisModeType.VsTodayOpen;
                UpdateLayout();
                RefreshChart();
            };
            ChartMenu.Items.Add(miOpenRate);

            // VI 예상선 토글
            var miViLine = new ToolStripMenuItem("VI 예상선 (시가±10%)");
            miViLine.Checked = _showViLine;
            miViLine.Click += (s, e) => { _showViLine = !_showViLine; ((ToolStripMenuItem)s).Checked = _showViLine; RefreshChart(); };
            ChartMenu.Items.Add(miViLine);

            ChartMenu.Items.Add(new ToolStripSeparator());
            ChartMenu.Items.Add("지표 모두 삭제", null, (s, e) => { _customSeriesList.Clear(); RefreshChart(); });

            skControl.ContextMenuStrip = ChartMenu;
        }

        private void FastChart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && !string.IsNullOrEmpty(_selectedSeriesName))
            {
                var target = _customSeriesList.FirstOrDefault(x => x.SeriesName == _selectedSeriesName);
                if (target != null)
                {
                    _customSeriesList.Remove(target);
                    // Cleanup weights if no more series in this panel
                    if (target.PanelType == PanelType.Bottom)
                    {
                        if (!_customSeriesList.Any(s => s.PanelName == target.PanelName))
                        {
                            _panelWeights.Remove(target.PanelName);
                        }
                    }
                    IndicatorDeleted?.Invoke(this, _selectedSeriesName);
                    _selectedSeriesName = "";
                    UpdateLayout();
                    RefreshChart();
                }
            }
        }


        public void BeginUpdate() => _isUpdating = true;
        public void EndUpdate() { _isUpdating = false; RefreshChart(); }
        public void RefreshChart() { if (!_isUpdating) skControl.Invalidate(); }

        private void UpdateLayout()
        {
            if (skControl.Width <= 0 || skControl.Height <= 0) return;

            float w = skControl.Width;
            float h = skControl.Height;
            float leftMargin = (_leftAxisMode != LeftAxisModeType.None) ? LeftAxisWidth : 0.0f;
            float totalAvailableHeight = h - AxisHeight;

            SubCharts.Clear();
            _splitterRects.Clear();

            var bottomPanels = _customSeriesList.Where(cs => cs.PanelType == PanelType.Bottom)
                                                .Select(cs => cs.PanelName)
                                                .Distinct().ToList();

            if (!_panelWeights.ContainsKey(MAIN_PANEL_NAME)) _panelWeights[MAIN_PANEL_NAME] = 3.0f;
            foreach (var p in bottomPanels) if (!_panelWeights.ContainsKey(p)) _panelWeights[p] = 1.2f;

            if (bottomPanels.Count > 0)
            {
                float totalWeight = _panelWeights[MAIN_PANEL_NAME] + bottomPanels.Sum(p => _panelWeights[p]);
                float unitHeight = totalAvailableHeight / totalWeight;

                float mainHeight = unitHeight * _panelWeights[MAIN_PANEL_NAME];
                MainRect = new SKRect(leftMargin, 0, w - AxisWidth, mainHeight);
                PriceRect = new SKRect(w - AxisWidth, 0, w, mainHeight);
                if (_leftAxisMode != LeftAxisModeType.None) _leftAxisRect = new SKRect(0, 0, leftMargin, mainHeight);

                float currentY = mainHeight;
                string prevPanel = MAIN_PANEL_NAME;
                foreach (var p in bottomPanels)
                {
                    _splitterRects.Add(new SplitterInfo { Rect = new SKRect(0, currentY - 3, w, currentY + 3), PanelAbove = prevPanel, PanelBelow = p });
                    float pHeight = unitHeight * _panelWeights[p];
                    SubCharts.Add(new SubChartInfo { PanelName = p, Rect = new SKRect(leftMargin, currentY, w - AxisWidth, currentY + pHeight), PriceRect = new SKRect(w - AxisWidth, currentY, w, currentY + pHeight) });
                    currentY += pHeight;
                    prevPanel = p;
                }
            }
            else
            {
                MainRect = new SKRect(leftMargin, 0, w - AxisWidth, totalAvailableHeight);
                PriceRect = new SKRect(w - AxisWidth, 0, w, totalAvailableHeight);
                if (_leftAxisMode != LeftAxisModeType.None) _leftAxisRect = new SKRect(0, 0, leftMargin, totalAvailableHeight);
            }
            TimeRect = new SKRect(leftMargin, h - AxisHeight, w - AxisWidth, h);
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(CurrentTheme.Background);

            if (Data.Count == 0) return;

            int startIndex = Math.Max(0, (int)Math.Floor(_scrollOffset));
            int visibleBars = (int)(MainRect.Width / (_candleWidth + _gap)) + 2;
            int endIndex = Math.Min(Data.Count - 1, startIndex + visibleBars);

            float maxP = float.MinValue, minP = float.MaxValue;
            for (int i = startIndex; i <= endIndex; i++) { maxP = Math.Max(maxP, Data[i].High); minP = Math.Min(minP, Data[i].Low); }
            float padding = (maxP - minP) * 0.05f;
            maxP += padding; minP -= padding;
            if (maxP == minP) maxP += 1.0f;

            if (_isAutoScaleY) { _manualMaxP = maxP; _manualMinP = minP; }
            else { maxP = _manualMaxP; minP = _manualMinP; }

            // Left Axis Range (Comparison)
            float maxL = float.MinValue, minL = float.MaxValue;
            var leftSeries = _customSeriesList.Where(s => s.IsLeftAxis).ToList();
            bool hasLeft = leftSeries.Count > 0;
            if (hasLeft)
            {
                foreach (var s in leftSeries)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (i < s.Values.Count && !double.IsNaN(s.Values[i]))
                        {
                            maxL = Math.Max(maxL, (float)s.Values[i]);
                            minL = Math.Min(minL, (float)s.Values[i]);
                        }
                    }
                }
                float padL = (maxL - minL) * 0.05f;
                maxL += padL; minL -= padL;
                if (maxL == minL) maxL += 1;
                if (_leftAxisMode == LeftAxisModeType.None) { _leftAxisMode = LeftAxisModeType.VsPrevClose; UpdateLayout(); }
            }
            else
            {
                // 시가대비 모드가 아닌 경우에만 좌축 해제
                if (_leftAxisMode != LeftAxisModeType.None && !_showOpenChangeRate)
                {
                    _leftAxisMode = LeftAxisModeType.None; UpdateLayout();
                }
            }

            canvas.Save();
            canvas.ClipRect(MainRect);
            DrawGrid(canvas, MainRect, maxP, minP);
            DrawCandles(canvas, startIndex, endIndex, maxP, minP);
            // Draw Right Axis Series
            foreach (var cs in _customSeriesList.Where(s => s.PanelType == PanelType.Overlay && !s.IsLeftAxis))
                DrawSeries(canvas, cs, startIndex, endIndex, maxP, minP, MainRect);

            // Draw Left Axis Series
            if (hasLeft)
            {
                foreach (var cs in leftSeries)
                    DrawSeries(canvas, cs, startIndex, endIndex, maxL, minL, MainRect);
            }
            canvas.Restore();

            // ── 현재가 흰색 점선 (마지막 캔들의 Close) ──
            if (_showCurrentPriceLine && Data.Count > 0)
            {
                float curPrice = Data[Data.Count - 1].Close;
                float curY = GetY(curPrice, maxP, minP, MainRect);
                if (curY >= MainRect.Top && curY <= MainRect.Bottom)
                {
                    using (var pLine = new SKPaint
                    {
                        Color = new SKColor(255, 255, 255, 180),
                        StrokeWidth = 1.0f,
                        PathEffect = SKPathEffect.CreateDash(new float[] { 6, 4 }, 0)
                    })
                    {
                        canvas.DrawLine(MainRect.Left, curY, MainRect.Right, curY, pLine);
                    }
                    // 가격 라벨
                    using (var pBg = new SKPaint { Color = new SKColor(60, 60, 80) })
                    using (var pTxt = new SKPaint { Color = SKColors.White, TextSize = 11, IsAntialias = true })
                    {
                        string txt = FormatPrice(curPrice);
                        float tw = pTxt.MeasureText(txt);
                        canvas.DrawRect(PriceRect.Left + 2, curY - 8, tw + 6, 16, pBg);
                        canvas.DrawText(txt, PriceRect.Left + 5, curY + 4, pTxt);
                    }
                }
            }

            // ── VI 예상선 (시가 대비 ±10%) ──
            if (_showViLine && TodayOpen > 0)
            {
                float viUp = (float)(TodayOpen * 1.10);
                float viDown = (float)(TodayOpen * 0.90);
                using (var pVi = new SKPaint
                {
                    Color = new SKColor(255, 60, 60, 180),
                    StrokeWidth = 1.0f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 8, 4 }, 0)
                })
                using (var pTxt = new SKPaint { Color = new SKColor(255, 80, 80), TextSize = 10, IsAntialias = true })
                {
                    float yUp = GetY(viUp, maxP, minP, MainRect);
                    float yDown = GetY(viDown, maxP, minP, MainRect);

                    if (yUp >= MainRect.Top && yUp <= MainRect.Bottom)
                    {
                        canvas.DrawLine(MainRect.Left, yUp, MainRect.Right, yUp, pVi);
                        canvas.DrawText($"VI↑ {FormatPrice(viUp)}", MainRect.Left + 5, yUp - 3, pTxt);
                    }
                    if (yDown >= MainRect.Top && yDown <= MainRect.Bottom)
                    {
                        canvas.DrawLine(MainRect.Left, yDown, MainRect.Right, yDown, pVi);
                        canvas.DrawText($"VI↓ {FormatPrice(viDown)}", MainRect.Left + 5, yDown + 12, pTxt);
                    }
                }
            }

            foreach (var sc in SubCharts) DrawSubChart(canvas, sc, startIndex, endIndex);

            DrawPriceAxis(canvas, MainRect, maxP, minP, PriceRect, false);

            // ── 시가대비 상승률 좌축 ──
            if (_showOpenChangeRate && TodayOpen > 0)
            {
                if (_leftAxisMode == LeftAxisModeType.VsTodayOpen)
                {
                    // 좌축에 % 표시 (maxP/minP 기준으로 시가 대비 변화율 계산)
                    using (var p = new SKPaint { Color = new SKColor(200, 200, 255), TextSize = 11, IsAntialias = true })
                    {
                        float range = maxP - minP;
                        float step = GetNiceStep(range, 5);
                        float startPrice = (float)Math.Ceiling(minP / step) * step;

                        for (float price = startPrice; price <= maxP; price += step)
                        {
                            float y = GetY(price, maxP, minP, MainRect);
                            double pct = (price - TodayOpen) / TodayOpen * 100.0;
                            string txt = pct.ToString("+0.0;-0.0;0.0") + "%";
                            float txtW = p.MeasureText(txt);
                            canvas.DrawText(txt, _leftAxisRect.Right - txtW - 5, y + 4, p);
                        }

                        // 0% 기준선 (시가)
                        float yOpen = GetY((float)TodayOpen, maxP, minP, MainRect);
                        if (yOpen >= MainRect.Top && yOpen <= MainRect.Bottom)
                        {
                            using (var pOpenLine = new SKPaint
                            {
                                Color = new SKColor(255, 255, 0, 100),
                                StrokeWidth = 0.8f,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0)
                            })
                            {
                                canvas.DrawLine(MainRect.Left, yOpen, MainRect.Right, yOpen, pOpenLine);
                            }
                        }
                    }
                }
            }
            else if (hasLeft)
            {
                DrawPriceAxis(canvas, MainRect, maxL, minL, _leftAxisRect, true);
            }

            DrawTimeAxis(canvas, startIndex, endIndex);
            if (_mouseX >= 0 && _mouseY >= 0) DrawCrosshair(canvas, maxP, minP, startIndex, endIndex);
            
            float legendY = 20;
            DrawStockInfo(canvas, ref legendY);
            DrawLegends(canvas, ref legendY);
        }

        private void DrawGrid(SKCanvas canvas, SKRect rect, float max, float min)
        {
            using (var p = new SKPaint { Color = CurrentTheme.GridLine, StrokeWidth = 0.5f })
            {
                float range = max - min;
                float step = GetNiceStep(range, 5);
                float startPrice = (float)Math.Ceiling(min / step) * step;

                for (float price = startPrice; price <= max; price += step)
                {
                    float y = GetY(price, max, min, rect);
                    canvas.DrawLine(rect.Left, y, rect.Right, y, p);
                }
            }
        }

        private float GetNiceStep(float range, int targetCount)
        {
            if (range <= 0) return 1;
            float rawStep = range / targetCount;
            float mag = (float)Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            float res = rawStep / mag;
            if (res < 1.5) res = 1;
            else if (res < 3) res = 2;
            else if (res < 7) res = 5;
            else res = 10;
            return res * mag;
        }

        private void DrawCandles(SKCanvas canvas, int start, int end, float maxP, float minP)
        {
            using (var pUp = new SKPaint { Color = CurrentTheme.CandleUp })
            using (var pDown = new SKPaint { Color = CurrentTheme.CandleDown })
            using (var pWick = new SKPaint { Color = CurrentTheme.Wick, StrokeWidth = 1 })
            {
                for (int i = start; i <= end; i++)
                {
                    var d = Data[i];
                    float x = GetX(i);
                    float yOpen = GetY(d.Open, maxP, minP, MainRect);
                    float yClose = GetY(d.Close, maxP, minP, MainRect);
                    float yHigh = GetY(d.High, maxP, minP, MainRect);
                    float yLow = GetY(d.Low, maxP, minP, MainRect);

                    canvas.DrawLine(x + _candleWidth / 2, yHigh, x + _candleWidth / 2, yLow, pWick);
                    if (d.Open <= d.Close) canvas.DrawRect(SKRect.Create(x, yClose, _candleWidth, Math.Max(1, yOpen - yClose)), pUp);
                    else canvas.DrawRect(SKRect.Create(x, yOpen, _candleWidth, Math.Max(1, yClose - yOpen)), pDown);
                }
            }
        }

        private void DrawSubChart(SKCanvas canvas, SubChartInfo sc, int start, int end)
        {
            var series = _customSeriesList.Where(s => s.PanelName == sc.PanelName).ToList();
            if (series.Count == 0) return;

            float maxV = float.MinValue, minV = float.MaxValue;
            foreach (var s in series)
                for (int i = start; i <= end; i++)
                    if (i < s.Values.Count && !double.IsNaN(s.Values[i]))
                    {
                        maxV = Math.Max(maxV, (float)s.Values[i]);
                        minV = Math.Min(minV, (float)s.Values[i]);
                    }
            
            if (maxV == float.MinValue) return;
            float pad = (maxV - minV) * 0.1f; maxV += pad; minV -= pad;
            if (maxV == minV) maxV += 1;

            canvas.Save();
            canvas.ClipRect(sc.Rect);
            DrawGrid(canvas, sc.Rect, maxV, minV);
            foreach (var s in series) DrawSeries(canvas, s, start, end, maxV, minV, sc.Rect);
            canvas.Restore();

            DrawPriceAxis(canvas, sc.Rect, maxV, minV, sc.PriceRect, false);
        }

        public void AddComparisonSeries(string title, List<CandleData> candles)
        {
            var series = new CustomSeries
            {
                SeriesName = "COMP_" + title,
                Title = title,
                Color = SKColors.Cyan,
                PanelType = PanelType.Overlay,
                IsLeftAxis = true,
                Style = PlotType.Line,
                Thickness = 2.0f
            };

            // Sync with main data dates
            foreach (var d in Data)
            {
                var match = candles.FirstOrDefault(c => c.DateTime == d.DateVal);
                if (match.DateTime == default)
                {
                    // Try matching by date only (for daily comparison on intraday chart)
                    match = candles.FirstOrDefault(c => c.DateTime.Date == d.DateVal.Date);
                }

                if (match.DateTime != default) series.Values.Add((double)match.Close);
                else series.Values.Add(double.NaN);
            }

            _customSeriesList.RemoveAll(s => s.SeriesName == series.SeriesName);
            _customSeriesList.Add(series);
            UpdateLayout();
            RefreshChart();
        }

        private void DrawSeries(SKCanvas canvas, CustomSeries s, int start, int end, float max, float min, SKRect rect)
        {
            using (var p = new SKPaint { Color = s.Color, StrokeWidth = s.Thickness, Style = SKPaintStyle.Stroke, IsAntialias = true })
            {
                if (s.Style == PlotType.Histogram)
                {
                    p.Style = SKPaintStyle.Fill;
                    float yBase = GetY((float)(s.BaseLine ?? 0), max, min, rect);
                    for (int i = start; i <= end; i++)
                    {
                        if (i >= s.Values.Count || double.IsNaN(s.Values[i])) continue;
                        float x = GetX(i);
                        float y = GetY((float)s.Values[i], max, min, rect);
                        
                        if (i < s.OutputColors.Count) p.Color = s.OutputColors[i];
                        else p.Color = s.Color;

                        canvas.DrawRect(SKRect.Create(x, Math.Min(y, yBase), _candleWidth, Math.Abs(y - yBase)), p);
                    }
                }
                else
                {
                    for (int i = start + 1; i <= end; i++)
                    {
                        if (i >= s.Values.Count || double.IsNaN(s.Values[i]) || double.IsNaN(s.Values[i - 1])) continue;
                        
                        float x1 = GetX(i - 1) + _candleWidth / 2;
                        float y1 = GetY((float)s.Values[i - 1], max, min, rect);
                        float x2 = GetX(i) + _candleWidth / 2;
                        float y2 = GetY((float)s.Values[i], max, min, rect);

                        if (i < s.OutputColors.Count) p.Color = s.OutputColors[i];
                        else p.Color = s.Color;

                        canvas.DrawLine(x1, y1, x2, y2, p);
                    }
                }
            }
        }

        private void DrawPriceAxis(SKCanvas canvas, SKRect chartRect, float max, float min, SKRect axisRect, bool isLeft)
        {
            using (var p = new SKPaint { Color = CurrentTheme.Text, TextSize = 11, IsAntialias = true })
            {
                float range = max - min;
                float step = GetNiceStep(range, 5);
                float startPrice = (float)Math.Ceiling(min / step) * step;

                for (float price = startPrice; price <= max; price += step)
                {
                    float y = GetY(price, max, min, chartRect);
                    string txt = FormatPrice(price);
                    if (isLeft)
                    {
                        float txtW = p.MeasureText(txt);
                        canvas.DrawText(txt, axisRect.Right - txtW - 5, y + 4, p);
                    }
                    else
                    {
                        canvas.DrawText(txt, axisRect.Left + 5, y + 4, p);
                    }
                }
            }
        }

        private void DrawTimeAxis(SKCanvas canvas, int start, int end)
        {
            using (var p = new SKPaint { Color = CurrentTheme.Text, TextSize = 11, IsAntialias = true, TextAlign = SKTextAlign.Center })
            {
                int step = Math.Max(1, (end - start) / 5);
                for (int i = start; i <= end; i += step)
                {
                    float x = GetX(i);
                    canvas.DrawText(Data[i].DateVal.ToString("HH:mm"), x, TimeRect.Top + 15, p);
                }
            }
        }

        private void DrawCrosshair(SKCanvas canvas, float max, float min, int start, int end)
        {
            using (var pLine = new SKPaint { Color = CurrentTheme.Crosshair, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0) })
            using (var pBg = new SKPaint { Color = CurrentTheme.CrosshairLabelBg })
            using (var pTxt = new SKPaint { Color = CurrentTheme.CrosshairLabelText, TextSize = 11, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Malgun Gothic") })
            {
                canvas.DrawLine(_mouseX, 0, _mouseX, skControl.Height - AxisHeight, pLine);
                canvas.DrawLine(0, _mouseY, skControl.Width, _mouseY, pLine);

                float val = 0;
                SKRect activeRect = MainRect;
                SKRect activePriceRect = PriceRect;
                bool found = false;

                if (MainRect.Contains(_mouseX, _mouseY) || PriceRect.Contains(_mouseX, _mouseY))
                {
                    val = min + (1.0f - (_mouseY - MainRect.Top) / MainRect.Height) * (max - min);
                    found = true;
                }
                else
                {
                    foreach (var sc in SubCharts)
                    {
                        if (sc.Rect.Contains(_mouseX, _mouseY) || sc.PriceRect.Contains(_mouseX, _mouseY))
                        {
                            activeRect = sc.Rect;
                            activePriceRect = sc.PriceRect;
                            var series = _customSeriesList.Where(s => s.PanelName == sc.PanelName).ToList();
                            float sMax = float.MinValue, sMin = float.MaxValue;
                            foreach (var s in series)
                                for (int i = start; i <= end; i++)
                                    if (i < s.Values.Count && !double.IsNaN(s.Values[i]))
                                    {
                                        sMax = Math.Max(sMax, (float)s.Values[i]);
                                        sMin = Math.Min(sMin, (float)s.Values[i]);
                                    }
                            float pad = (sMax - sMin) * 0.1f; sMax += pad; sMin -= pad;
                            if (sMax == sMin) sMax += 1;
                            val = sMin + (1.0f - (_mouseY - sc.Rect.Top) / sc.Rect.Height) * (sMax - sMin);
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    string label = (val >= 1000) ? val.ToString("N0") : val.ToString("N2");
                    float labelY = _mouseY + 4;
                    canvas.DrawRect(SKRect.Create(activePriceRect.Left, _mouseY - 10, AxisWidth, 20), pBg);
                    canvas.DrawText(label, activePriceRect.Left + AxisWidth / 2, labelY, pTxt);
                }

                // --- 시간 라벨 추가 ---
                int idx = (int)Math.Round(_scrollOffset + (_mouseX - MainRect.Left) / (_candleWidth + _gap));
                if (idx >= 0 && idx < Data.Count)
                {
                    string timeStr = Data[idx].DateVal.ToString("MM/dd HH:mm");
                    float tW = pTxt.MeasureText(timeStr);
                    canvas.DrawRect(SKRect.Create(_mouseX - tW / 2 - 5, TimeRect.Top, tW + 10, AxisHeight), pBg);
                    canvas.DrawText(timeStr, _mouseX, TimeRect.Top + 16, pTxt);

                    CrosshairMoved?.Invoke(this, new CrosshairEventArgs { Index = idx, Time = Data[idx].DateVal, Price = val });
                }
            }
        }

        private void DrawStockInfo(SKCanvas canvas, ref float y)
        {
            using (var p = new SKPaint { Color = CurrentTheme.Text, TextSize = 12, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Malgun Gothic") })
            {
                canvas.DrawText($"{_stockCode} {_stockName}", 10, y, p); y += 18;
            }
        }

        private void DrawLegends(SKCanvas canvas, ref float y)
        {
            _legendItems.Clear();
            int currIdx = -1;
            if (_mouseX >= 0)
            {
                currIdx = (int)Math.Round(_scrollOffset + (_mouseX - MainRect.Left) / (_candleWidth + _gap));
            }
            else if (Data.Count > 0)
            {
                currIdx = Data.Count - 1;
            }

            using (var p = new SKPaint { TextSize = 11, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") })
            {
                // 1. Overlay Indicators (Main Panel)
                foreach (var s in _customSeriesList.Where(s => s.PanelType == PanelType.Overlay))
                {
                    DrawLegendItem(canvas, s, ref y, 10, currIdx, p);







                }
                // 2. SubChart Indicators
                foreach (var sc in SubCharts)
                {
                    float scY = sc.Rect.Top + 15;
                    var series = _customSeriesList.Where(s => s.PanelName == sc.PanelName).ToList();
                    foreach (var s in series)
                    {
                        DrawLegendItem(canvas, s, ref scY, 10, currIdx, p);
                    }
                }
            }
        }

        private void DrawLegendItem(SKCanvas canvas, CustomSeries s, ref float y, float drawX, int currIdx, SKPaint p)
        {
            bool isSelected = s.SeriesName == _selectedSeriesName;
            p.Color = isSelected ? SKColors.White : s.Color;
            p.FakeBoldText = isSelected;

            double val = (currIdx >= 0 && currIdx < s.Values.Count) ? s.Values[currIdx] : double.NaN;
            string valStr = double.IsNaN(val) ? "n/a" : val.ToString("N2");
            string txt = $"{s.Title}: {valStr}";

            float txtW = p.MeasureText(txt);
            SKRect hit = new SKRect(drawX, y - 10, drawX + txtW + 20, y + 5);

            if (isSelected)
            {
                using (var bg = new SKPaint { Color = s.Color.WithAlpha(60) })
                    canvas.DrawRect(hit, bg);
            }

            canvas.DrawText(txt, drawX, y, p);

            _legendItems.Add(new LegendItem { 
                PanelName = s.SeriesName, 
                HitRect = hit, 
                Text = txt, 
                Color = s.Color, 
                IsOverlay = (s.PanelType == PanelType.Overlay) 
            });

            y += 18;
        }


        private float GetX(int i) => MainRect.Left + (i - _scrollOffset) * (_candleWidth + _gap);
        private float GetY(float val, float max, float min, SKRect rect) => rect.Top + rect.Height * (1.0f - (val - min) / (max - min));
        private string FormatPrice(float p) => p.ToString("N0");

        #region Interaction
        private void SKControl_MouseDown(object sender, MouseEventArgs e)
        {
            _lastMouseX = e.X; _lastMouseY = e.Y;
            this.Focus();

            var splitter = _splitterRects.FirstOrDefault(s => s.Rect.Contains(e.X, e.Y));
            if (splitter.Rect.Width > 0)
            {
                _isDraggingSplitter = true;
                _activeSplitter = splitter;
                return;
            }

            // 레전드 클릭 감지
            var hitLegend = _legendItems.FirstOrDefault(li => li.HitRect.Contains(e.X, e.Y));
            if (hitLegend != null)
            {
                _selectedSeriesName = hitLegend.PanelName;
                RefreshChart();
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (PriceRect.Contains(e.X, e.Y)) { _isDraggingPrice = true; _isAutoScaleY = false; }
                else _isDraggingChart = true;
            }
        }


        private void SKControl_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseX = e.X; _mouseY = e.Y;

            if (_isDraggingSplitter && _activeSplitter != null)
            {
                float dy = e.Y - _lastMouseY;
                float totalH = skControl.Height - AxisHeight;
                float weightSum = _panelWeights.Values.Sum();
                float weightDelta = (dy / totalH) * weightSum;

                _panelWeights[_activeSplitter.Value.PanelAbove] += weightDelta;
                _panelWeights[_activeSplitter.Value.PanelBelow] -= weightDelta;

                if (_panelWeights[_activeSplitter.Value.PanelAbove] < 0.2f) _panelWeights[_activeSplitter.Value.PanelAbove] = 0.2f;
                if (_panelWeights[_activeSplitter.Value.PanelBelow] < 0.2f) _panelWeights[_activeSplitter.Value.PanelBelow] = 0.2f;

                _lastMouseY = e.Y;
                UpdateLayout();
                RefreshChart();
                return;
            }

            if (_isDraggingChart)
            {
                float dx = e.X - _lastMouseX;
                _scrollOffset -= dx / (_candleWidth + _gap);
                _lastMouseX = e.X; RefreshChart();
            }
            else if (_isDraggingPrice)
            {
                float dy = e.Y - _lastMouseY;
                float range = _manualMaxP - _manualMinP;
                _manualMaxP += dy * (range / 100);
                _manualMinP -= dy * (range / 100);
                _lastMouseY = e.Y; RefreshChart();
            }
            else
            {
                if (_splitterRects.Any(s => s.Rect.Contains(e.X, e.Y)))
                    skControl.Cursor = Cursors.HSplit;
                else
                    skControl.Cursor = Cursors.Default;

                RefreshChart();
            }
        }

        private void OnWheel(object sender, MouseEventArgs e)
        {
            float zoom = (e.Delta > 0) ? 1.2f : 0.8f;
            CandleWidth *= zoom;
        }

        private void SKControl_DoubleClick(object sender, EventArgs e) 
        {
            var me = (MouseEventArgs)e;
            var hitLegend = _legendItems.FirstOrDefault(li => li.HitRect.Contains(me.X, me.Y));
            if (hitLegend != null)
            {
                IndicatorSettingsRequested?.Invoke(this, hitLegend.PanelName);
                return;
            }

            _isAutoScaleY = true; 
            RefreshChart(); 
        }

        #endregion

        public void LoadStockData(string code, string name, List<CandleData> candles)
        {
            _stockCode = code; _stockName = name;
            Data = candles.Select(c => new OHLCV { DateVal = c.DateTime, Open = (float)c.Open, High = (float)c.High, Low = (float)c.Low, Close = (float)c.Close, Volume = (long)c.Volume }).ToList();

            // 당일 시가 / 전일 종가 자동 감지
            if (Data.Count > 0)
            {
                var today = DateTime.Today;
                var todayBars = Data.Where(d => d.DateVal.Date == today).ToList();
                if (todayBars.Count > 0)
                {
                    TodayOpen = todayBars[0].Open;
                    // 전일 마지막 봉을 찾아 종가를 설정
                    var prevDayBars = Data.Where(d => d.DateVal.Date < today).ToList();
                    if (prevDayBars.Count > 0)
                        YesterdayClose = prevDayBars[prevDayBars.Count - 1].Close;
                }
                else
                {
                    // 전체가 과거 데이터인 경우 마지막 날이 기준
                    TodayOpen = Data[Data.Count - 1].Open;
                }
            }

            // 틱강도 초기화: 과거 데이터의 Volume을 체결건수 초기값으로 사용
            _tickCounts = Data.Select(d => (int)d.Volume).ToList();
            _maxDayTicks = _tickCounts.Count > 0 ? _tickCounts.Max() : 0;
            _lastRealtimePrice = Data.Count > 0 ? Data[Data.Count - 1].Close : 0;

            if (_autoScroll) ScrollToEnd();
            RefreshChart();
        }

        public CustomSeries GetSeries(string seriesName) => _customSeriesList.FirstOrDefault(s => s.SeriesName == seriesName);

        public void AddSeries(CustomSeries series)
        {
            if (string.IsNullOrEmpty(series.SeriesName)) series.SeriesName = Guid.NewGuid().ToString();
            _customSeriesList.RemoveAll(x => x.SeriesName == series.SeriesName);
            _customSeriesList.Add(series);
            UpdateLayout();
            RefreshChart();
        }

        public void ClearIndicators()
        {
            _customSeriesList.Clear();
            UpdateLayout();
            RefreshChart();
        }

        /// <summary>
        /// 실시간 시세(MarketData)로 차트 마지막 캔들 갱신 또는 새 캔들 추가.
        /// 분봉 기준: 현재 시각의 분이 바뀌면 새 캔들 추가, 아니면 마지막 캔들 OHLCV 갱신.
        /// </summary>
        public void UpdateRealtime(float price, long volume, DateTime time)
        {
            if (Data.Count == 0) return;

            var last = Data[Data.Count - 1];
            bool isNewBar = false;

            // 현재 분봉 시작 시각 계산
            var barTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);

            if (barTime > last.DateVal)
            {
                // 새로운 분봉 시작 → 이전 봉의 체결건수를 최대값에 반영
                isNewBar = true;
                if (_tickCounts.Count > 0)
                {
                    int prevTicks = _tickCounts[_tickCounts.Count - 1];
                    if (prevTicks > _maxDayTicks) _maxDayTicks = prevTicks;
                }
                _tickCounts.Add(0); // 새 봉의 체결건수 초기화

                Data.Add(new OHLCV
                {
                    DateVal = barTime,
                    Open = price,
                    High = price,
                    Low = price,
                    Close = price,
                    Volume = volume
                });
                if (_autoScroll) ScrollToEnd();
            }
            else
            {
                // 같은 분봉 → 마지막 캔들 갱신
                Data[Data.Count - 1] = new OHLCV
                {
                    DateVal = last.DateVal,
                    Open = last.Open,
                    High = Math.Max(last.High, price),
                    Low = Math.Min(last.Low, price),
                    Close = price,
                    Volume = last.Volume + volume
                };
            }

            // 양봉 체결건수 누적 (가격 상승 방향)
            if (price >= _lastRealtimePrice && volume > 0 && _tickCounts.Count > 0)
            {
                _tickCounts[_tickCounts.Count - 1] += (int)volume;
            }
            _lastRealtimePrice = price;

            // 틱강도 시리즈 실시간 갱신
            if (_showTickIntensity)
            {
                UpdateTickIntensitySeries(isNewBar);
            }

            // 지표 시리즈 실시간 업데이트
            foreach (var cs in _customSeriesList)
            {
                if (cs.SeriesName.StartsWith("COMP_")) continue;
                if (cs.SeriesName == "TICK_CNT" || cs.SeriesName == "TICK_RAT") continue; // 틱강도는 별도 처리

                try
                {
                    Services.IndicatorCalculation.UpdateSeriesRealtime(cs, Data, isNewBar);
                }
                catch { /* 무시 */ }
            }

            RefreshChart();
        }

        /// <summary>
        /// 틱강도(TickIntensity) 시리즈를 생성/갱신하여 차트에 표시.
        /// 체결건수(÷divisor) = 히스토그램, 틱비율(%) = 라인.
        /// </summary>
        public void BuildTickIntensitySeries()
        {
            // _tickCounts를 Data 길이에 맞게 동기화
            while (_tickCounts.Count < Data.Count) _tickCounts.Add(0);

            // 체결건수 히스토그램 시리즈
            var cntSeries = new CustomSeries
            {
                SeriesName = "TICK_CNT",
                Title = $"체결건수(÷{_tickDivisor})",
                Color = SKColors.DodgerBlue,
                PanelType = PanelType.Bottom,
                PanelName = "TickIntensity",
                Style = PlotType.Histogram,
                BaseLine = 0
            };

            // 틱비율 라인 시리즈
            var ratSeries = new CustomSeries
            {
                SeriesName = "TICK_RAT",
                Title = "틱비율(%)",
                Color = SKColors.Orange,
                PanelType = PanelType.Bottom,
                PanelName = "TickIntensity",
                Style = PlotType.Line,
                Thickness = 1.5f
            };

            // 값 계산
            var cntValues = new List<double>();
            var ratValues = new List<double>();
            var cntColors = new List<SKColor>();
            int runningMax = 0;

            for (int i = 0; i < _tickCounts.Count; i++)
            {
                int tc = _tickCounts[i];
                double divided = (double)tc / _tickDivisor;
                cntValues.Add(divided);

                // 색상: 양봉/음봉 판별
                if (i < Data.Count && Data[i].Close >= Data[i].Open)
                    cntColors.Add(SKColors.DodgerBlue);
                else
                    cntColors.Add(SKColors.Tomato);

                if (tc > runningMax) runningMax = tc;
                double ratio = runningMax > 0 ? (double)tc / runningMax * 100.0 : 0;
                ratValues.Add(ratio);
            }

            cntSeries.Values = cntValues;
            cntSeries.OutputColors = cntColors;
            ratSeries.Values = ratValues;

            _customSeriesList.RemoveAll(s => s.SeriesName == "TICK_CNT" || s.SeriesName == "TICK_RAT");
            _customSeriesList.Add(cntSeries);
            _customSeriesList.Add(ratSeries);
            UpdateLayout();
            RefreshChart();
        }

        private void UpdateTickIntensitySeries(bool isNewBar)
        {
            var cntSeries = _customSeriesList.FirstOrDefault(s => s.SeriesName == "TICK_CNT");
            var ratSeries = _customSeriesList.FirstOrDefault(s => s.SeriesName == "TICK_RAT");
            if (cntSeries == null || ratSeries == null) return;

            int tc = _tickCounts.Count > 0 ? _tickCounts[_tickCounts.Count - 1] : 0;
            double divided = (double)tc / _tickDivisor;
            int maxT = Math.Max(_maxDayTicks, tc);
            double ratio = maxT > 0 ? (double)tc / maxT * 100.0 : 0;

            // 색상
            SKColor col = SKColors.DodgerBlue;
            if (Data.Count > 0 && Data[Data.Count - 1].Close < Data[Data.Count - 1].Open)
                col = SKColors.Tomato;

            if (isNewBar)
            {
                cntSeries.Values.Add(divided);
                if (cntSeries.OutputColors == null) cntSeries.OutputColors = new List<SKColor>();
                cntSeries.OutputColors.Add(col);
                ratSeries.Values.Add(ratio);
            }
            else
            {
                if (cntSeries.Values.Count > 0)
                {
                    cntSeries.Values[cntSeries.Values.Count - 1] = divided;
                    if (cntSeries.OutputColors != null && cntSeries.OutputColors.Count > 0)
                        cntSeries.OutputColors[cntSeries.OutputColors.Count - 1] = col;
                }
                if (ratSeries.Values.Count > 0)
                    ratSeries.Values[ratSeries.Values.Count - 1] = ratio;
            }
        }

        /// <summary>틱강도 지표 토글 (제거)</summary>
        public void RemoveTickIntensity()
        {
            _showTickIntensity = false;
            _customSeriesList.RemoveAll(s => s.SeriesName == "TICK_CNT" || s.SeriesName == "TICK_RAT");
            UpdateLayout();
            RefreshChart();
        }

        public void ScrollToEnd() { if (Data.Count == 0) return; float vis = MainRect.Width / (_candleWidth + _gap); _scrollOffset = Math.Max(0, Data.Count - vis + 5); }
    }
}
