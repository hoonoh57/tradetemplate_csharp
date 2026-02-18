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
        public event EventHandler IndicatorChanged;
        public event EventHandler<string> IndicatorDeleted;
        public event EventHandler<string> IndicatorSettingsRequested;
        public event EventHandler<CandleClickEventArgs> CandleDoubleClicked;
        public event EventHandler<string> LegendDoubleClicked;

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
            public List<double> TrendValues { get; set; }
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
        #endregion

        public FastChart()
        {
            skControl = new SKControl();
            skControl.Dock = DockStyle.Fill;
            this.Controls.Add(skControl);

            skControl.PaintSurface += OnPaintSurface;
            skControl.Resize += (s, e) => { UpdateLayout(); RefreshChart(); };
            skControl.MouseDown += SKControl_MouseDown;
            skControl.MouseMove += SKControl_MouseMove;
            skControl.MouseUp += (s, e) => { _isDraggingChart = _isDraggingPrice = _isDraggingSplitter = false; _activeSplitter = null; };
            skControl.MouseWheel += OnWheel;
            skControl.DoubleClick += SKControl_DoubleClick;
            skControl.MouseLeave += (s, e) => { _mouseX = _mouseY = -1; RefreshChart(); };
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

            canvas.Save();
            canvas.ClipRect(MainRect);
            DrawCandles(canvas, startIndex, endIndex, maxP, minP);
            foreach (var cs in _customSeriesList.Where(s => s.PanelType == PanelType.Overlay))
                DrawSeries(canvas, cs, startIndex, endIndex, maxP, minP, MainRect);
            canvas.Restore();

            foreach (var sc in SubCharts) DrawSubChart(canvas, sc, startIndex, endIndex);

            DrawPriceAxis(canvas, maxP, minP);
            DrawTimeAxis(canvas, startIndex, endIndex);
            if (_mouseX >= 0 && _mouseY >= 0) DrawCrosshair(canvas, maxP, minP, startIndex, endIndex);
            
            float legendY = 20;
            DrawStockInfo(canvas, ref legendY);
            DrawLegends(canvas, legendY);
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
            foreach (var s in series) DrawSeries(canvas, s, start, end, maxV, minV, sc.Rect);
            canvas.Restore();

            using (var p = new SKPaint { Color = CurrentTheme.Text, TextSize = 10 })
                canvas.DrawText(FormatPrice(maxV), sc.PriceRect.Left + 5, sc.Rect.Top + 10, p);
        }

        private void DrawSeries(SKCanvas canvas, CustomSeries s, int start, int end, float max, float min, SKRect rect)
        {
            using (var p = new SKPaint { Color = s.Color, StrokeWidth = s.Thickness, Style = SKPaintStyle.Stroke, IsAntialias = true })
            {
                if (s.Style == PlotType.Histogram)
                {
                    p.Style = SKPaintStyle.Fill;
                    float yBase = GetY(0, max, min, rect);
                    for (int i = start; i <= end; i++)
                    {
                        if (i >= s.Values.Count || double.IsNaN(s.Values[i])) continue;
                        float x = GetX(i);
                        float y = GetY((float)s.Values[i], max, min, rect);
                        canvas.DrawRect(SKRect.Create(x, Math.Min(y, yBase), _candleWidth, Math.Abs(y - yBase)), p);
                    }
                }
                else
                {
                    using (var path = new SKPath())
                    {
                        bool first = true;
                        for (int i = start; i <= end; i++)
                        {
                            if (i >= s.Values.Count || double.IsNaN(s.Values[i])) { first = true; continue; }
                            float x = GetX(i) + _candleWidth / 2;
                            float y = GetY((float)s.Values[i], max, min, rect);
                            if (first) { path.MoveTo(x, y); first = false; } else path.LineTo(x, y);
                        }
                        canvas.DrawPath(path, p);
                    }
                }
            }
        }

        private void DrawPriceAxis(SKCanvas canvas, float max, float min)
        {
            using (var p = new SKPaint { Color = CurrentTheme.Text, TextSize = 11, IsAntialias = true })
            {
                canvas.DrawText(FormatPrice(max), PriceRect.Left + 5, 15, p);
                canvas.DrawText(FormatPrice(min), PriceRect.Left + 5, MainRect.Bottom - 5, p);
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
            using (var p = new SKPaint { Color = CurrentTheme.Crosshair, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0) })
            {
                canvas.DrawLine(_mouseX, 0, _mouseX, skControl.Height, p);
                canvas.DrawLine(0, _mouseY, skControl.Width, _mouseY, p);
            }
        }

        private void DrawStockInfo(SKCanvas canvas, ref float y)
        {
            using (var p = new SKPaint { Color = CurrentTheme.Text, TextSize = 12, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Malgun Gothic") })
            {
                canvas.DrawText($"{_stockCode} {_stockName}", 10, y, p); y += 18;
            }
        }

        private void DrawLegends(SKCanvas canvas, float y)
        {
            _legendItems.Clear();
            using (var p = new SKPaint { TextSize = 11, IsAntialias = true })
            {
                foreach (var s in _customSeriesList.Where(cs => cs.PanelType == PanelType.Overlay))
                {
                    p.Color = s.Color;
                    string txt = s.Title;
                    canvas.DrawText(txt, 10, y, p);
                    _legendItems.Add(new LegendItem { PanelName = s.SeriesName, HitRect = new SKRect(10, y - 10, 10 + p.MeasureText(txt), y + 5), Text = txt, Color = s.Color, IsOverlay = true });
                    y += 15;
                }
            }
        }

        private float GetX(int i) => MainRect.Left + (i - _scrollOffset) * (_candleWidth + _gap);
        private float GetY(float val, float max, float min, SKRect rect) => rect.Top + rect.Height * (1.0f - (val - min) / (max - min));
        private string FormatPrice(float p) => p.ToString("N0");

        #region Interaction
        private void SKControl_MouseDown(object sender, MouseEventArgs e)
        {
            _lastMouseX = e.X; _lastMouseY = e.Y;
            if (e.Button == MouseButtons.Left)
            {
                if (PriceRect.Contains(e.X, e.Y)) { _isDraggingPrice = true; _isAutoScaleY = false; }
                else _isDraggingChart = true;
            }
        }

        private void SKControl_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseX = e.X; _mouseY = e.Y;
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
            else RefreshChart();
        }

        private void OnWheel(object sender, MouseEventArgs e)
        {
            float zoom = (e.Delta > 0) ? 1.2f : 0.8f;
            CandleWidth *= zoom;
        }

        private void SKControl_DoubleClick(object sender, EventArgs e) { _isAutoScaleY = true; RefreshChart(); }
        #endregion

        public void LoadStockData(string code, string name, List<CandleData> candles)
        {
            _stockCode = code; _stockName = name;
            Data = candles.Select(c => new OHLCV { DateVal = c.DateTime, Open = (float)c.Open, High = (float)c.High, Low = (float)c.Low, Close = (float)c.Close, Volume = (long)c.Volume }).ToList();
            if (_autoScroll) ScrollToEnd();
            RefreshChart();
        }

        public void ScrollToEnd() { if (Data.Count == 0) return; float vis = MainRect.Width / (_candleWidth + _gap); _scrollOffset = Math.Max(0, Data.Count - vis + 5); }
    }
}
