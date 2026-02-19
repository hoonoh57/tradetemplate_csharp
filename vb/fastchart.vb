Imports System.Math
Imports SkiaSharp

Imports System.Windows.Forms
Imports System.Linq
Imports System.Collections.Generic
'Imports Fast.Models
Imports SkiaSharp.Views.Desktop ' Added
'Imports Fast.Unit.Native
'Imports Fast.Unit.Engine ' For TradeRecord?
'Imports Fast.Unit.Strategy ' For Strategy constructs?
Imports System.Drawing
Imports Protocol ' [Added] for ColorTranslator
'Imports Managers

''' <summary>
''' SkiaSharp 기반 고성능 캔들차트 + TI2Wrapper 지표 통합
''' 서브차트: 다중 지표 지원 (하단에 순차적으로 쌓임)
''' </summary>
Public Class FastChart
    Inherits UserControl

    ' --- 이벤트 정의 ---
    Public Event CandleClicked(sender As Object, e As CandleClickEventArgs)
    Public Event CrosshairMoved(sender As Object, e As CrosshairEventArgs)
    Public Event ScaleChanged(sender As Object, e As EventArgs)
    Public Event IndicatorChanged(sender As Object, e As EventArgs)
    Public Event IndicatorDeleted(sender As Object, seriesName As String)
    Public Event IndicatorSettingsRequested(sender As Object, seriesName As String)

#Region "테마 설정"
    Public Class ChartTheme
        Public Property Background As SKColor
        Public Property Text As SKColor
        Public Property GridLine As SKColor
        Public Property CandleUp As SKColor
        Public Property CandleDown As SKColor
        Public Property Wick As SKColor
        Public Property Crosshair As SKColor
        Public Property CrosshairLabelBg As SKColor
        Public Property CrosshairLabelText As SKColor
        Public Property Border As SKColor
        Public Property VolumeUp As SKColor
        Public Property VolumeDown As SKColor

        ' 이동평균선 색상
        Public Property MA5 As SKColor
        Public Property MA10 As SKColor
        Public Property MA20 As SKColor
        Public Property MA60 As SKColor
        Public Property MA120 As SKColor

        ' 볼린저밴드 색상
        Public Property BBUpper As SKColor
        Public Property BBMiddle As SKColor
        Public Property BBLower As SKColor
        Public Property BBFill As SKColor

        ' SuperTrend 색상
        Public Property STUp As SKColor
        Public Property STDown As SKColor

        ' 서브차트 지표 색상
        Public Property RSILine As SKColor
        Public Property RSIOverbought As SKColor
        Public Property RSIOversold As SKColor

        Public Property MACDLine As SKColor
        Public Property MACDSignal As SKColor
        Public Property MACDHistUp As SKColor
        Public Property MACDHistDown As SKColor

        Public Property CCILine As SKColor
        Public Property StochK As SKColor
        Public Property StochD As SKColor

        Public Property ATRLine As SKColor
        Public Property ADXLine As SKColor
        Public Property PlusDI As SKColor
        Public Property MinusDI As SKColor

        Public Property JMALine As SKColor

        Public Shared Function CreateDarkTheme() As ChartTheme
            Return New ChartTheme With {
                .Background = SKColor.Parse("#131722"),
                .Text = SKColor.Parse("#d1d4dc"),
                .GridLine = SKColor.Parse("#363c4e"),
                .CandleUp = SKColor.Parse("#089981"),
                .CandleDown = SKColor.Parse("#f23645"),
                .Wick = SKColor.Parse("#787b86"),
                .Crosshair = SKColor.Parse("#9598a1"),
                .CrosshairLabelBg = SKColor.Parse("#4c525e"),
                .CrosshairLabelText = SKColors.White,
                .Border = SKColor.Parse("#2a2e39"),
                .VolumeUp = SKColor.Parse("#089981").WithAlpha(128),
                .VolumeDown = SKColor.Parse("#f23645").WithAlpha(128),
                .MA5 = SKColor.Parse("#f5c142"),
                .MA10 = SKColor.Parse("#42a5f5"),
                .MA20 = SKColor.Parse("#ab47bc"),
                .MA60 = SKColor.Parse("#26a69a"),
                .MA120 = SKColor.Parse("#ef5350"),
                .BBUpper = SKColor.Parse("#2962ff"),
                .BBMiddle = SKColor.Parse("#ff9800"),
                .BBLower = SKColor.Parse("#2962ff"),
                .BBFill = SKColor.Parse("#2962ff").WithAlpha(30),
                .STUp = SKColor.Parse("#FFFF00"), ' Yellow
                .STDown = SKColor.Parse("#800080"), ' Purple
                .RSILine = SKColor.Parse("#7e57c2"),
                .RSIOverbought = SKColor.Parse("#f44336").WithAlpha(100),
                .RSIOversold = SKColor.Parse("#4caf50").WithAlpha(100),
                .MACDLine = SKColor.Parse("#2196f3"),
                .MACDSignal = SKColor.Parse("#ff9800"),
                .MACDHistUp = SKColor.Parse("#089981"),
                .MACDHistDown = SKColor.Parse("#f23645"),
                .CCILine = SKColor.Parse("#00bcd4"),
                .StochK = SKColor.Parse("#2196f3"),
                .StochD = SKColor.Parse("#ff5722"),
                .ATRLine = SKColor.Parse("#9c27b0"),
                .ADXLine = SKColor.Parse("#ff9800"),
                .PlusDI = SKColor.Parse("#4caf50"),
                .MinusDI = SKColor.Parse("#f44336"),
                .JMALine = SKColor.Parse("#e91e63")
            }
        End Function

        Public Shared Function CreateLightTheme() As ChartTheme
            Return New ChartTheme With {
                .Background = SKColor.Parse("#ffffff"),
                .Text = SKColor.Parse("#333333"),
                .GridLine = SKColor.Parse("#e0e0e0"),
                .CandleUp = SKColor.Parse("#089981"),
                .CandleDown = SKColor.Parse("#f23645"),
                .Wick = SKColor.Parse("#999999"),
                .Crosshair = SKColor.Parse("#666666"),
                .CrosshairLabelBg = SKColor.Parse("#333333"),
                .CrosshairLabelText = SKColors.White,
                .Border = SKColor.Parse("#cccccc"),
                .VolumeUp = SKColor.Parse("#089981").WithAlpha(100),
                .VolumeDown = SKColor.Parse("#f23645").WithAlpha(100),
                .MA5 = SKColor.Parse("#d4a017"),
                .MA10 = SKColor.Parse("#1976d2"),
                .MA20 = SKColor.Parse("#7b1fa2"),
                .MA60 = SKColor.Parse("#00897b"),
                .MA120 = SKColor.Parse("#d32f2f"),
                .BBUpper = SKColor.Parse("#1565c0"),
                .BBMiddle = SKColor.Parse("#ef6c00"),
                .BBLower = SKColor.Parse("#1565c0"),
                .BBFill = SKColor.Parse("#1565c0").WithAlpha(20),
                .STUp = SKColor.Parse("#FFFF00"), ' Yellow
                .STDown = SKColor.Parse("#800080"), ' Purple
                .RSILine = SKColor.Parse("#5e35b1"),
                .RSIOverbought = SKColor.Parse("#e53935").WithAlpha(80),
                .RSIOversold = SKColor.Parse("#43a047").WithAlpha(80),
                .MACDLine = SKColor.Parse("#1565c0"),
                .MACDSignal = SKColor.Parse("#ef6c00"),
                .MACDHistUp = SKColor.Parse("#2e7d32"),
                .MACDHistDown = SKColor.Parse("#c62828"),
                .CCILine = SKColor.Parse("#00838f"),
                .StochK = SKColor.Parse("#1565c0"),
                .StochD = SKColor.Parse("#e64a19"),
                .ATRLine = SKColor.Parse("#6a1b9a"),
                .ADXLine = SKColor.Parse("#ef6c00"),
                .PlusDI = SKColor.Parse("#2e7d32"),
                .MinusDI = SKColor.Parse("#c62828"),
                .JMALine = SKColor.Parse("#ad1457")
            }
        End Function
    End Class
#End Region

#Region "데이터 구조체"
    Public Structure OHLCV
        Public Open As Single
        Public High As Single
        Public Low As Single
        Public Close As Single
        Public Volume As Long
        Public DateVal As DateTime
        Public TimeStr As String
    End Structure

    Public Enum TimeFrame
        Tick
        Min1
        Min3
        Min5
        Min10
        Min15
        Min30
        Min60
        Day
        Week
        Month
    End Enum

    ' [REMOVED: IndicatorType Enum to enforce decoupling]


    ' Moved LeftAxisModeType to global scope to avoid ambiguity and access issues

    Public Class CandleClickEventArgs
        Inherits EventArgs
        Public Property Index As Integer
        Public Property Data As OHLCV
        Public Property MouseButton As MouseButtons
    End Class

    Public Class CrosshairEventArgs
        Inherits EventArgs
        Public Property Price As Single
        Public Property Time As DateTime
        Public Property Index As Integer
    End Class

    Public Enum PanelType ' [Added]
        Overlay
        Bottom
    End Enum

    Public Class CustomSeries ' [Added]
        Public Property PanelName As String
        Public Property SeriesName As String ' Key for lookup
        Public Property Title As String ' Display Name [Added]
        Public Property Color As SKColor
        Public Property Values As List(Of Double)
        Public Property PanelType As PanelType
        Public Property Style As PlotType = PlotType.Line ' [Added]
        Public Property Shape As ShapeType = ShapeType.Circle ' [Added] for Scatter
        Public Property MinValue As Double? = Nothing
        Public Property MaxValue As Double? = Nothing
        Public Property Thickness As Single = 1.0
        Public Property BaseLine As Double? = Nothing
        Public Property Overbought As Double? = Nothing
        Public Property Oversold As Double? = Nothing

        Public IsCustom As Boolean ' [Added]
        Public CustomPanelName As String ' [Added]
        Public Rect As SKRect
        Public PriceRect As SKRect
        Public Property ColorUp As SKColor? = Nothing ' [Added]
        Public Property ColorDown As SKColor? = Nothing ' [Added]
        Public Property TrendValues As List(Of Double) = Nothing ' [Added] 1: Up, -1: Down, 0: Neutral / Fallback
    End Class

    ' 서브차트 레이아웃 정보 (Generic)
    Public Structure SubChartInfo
        Public PanelName As String ' Identifier
        Public Rect As SKRect
        Public PriceRect As SKRect
    End Structure


    ' Legend 정보 (HitTest용)
    Private Class LegendItem
        Public Property PanelName As String
        Public Property HitRect As SKRect
        Public Property Text As String
        Public Property Color As SKColor
        Public Property IsOverlay As Boolean
    End Class

    ' [REMOVED: IndicatorData Structure to enforce Generic Data Model]

#End Region

#Region "필드"
    Private skControl As SKControl
    Private CurrentTheme As ChartTheme = ChartTheme.CreateDarkTheme()
    Public Data As New List(Of OHLCV)
    Public Property Boxes As New List(Of BreakoutBox)

    ' [REMOVED: Private Indicators As IndicatorData]

    ' 레이아웃 설정
    Private Const AxisWidth As Single = 70.0F
    Private Const LeftAxisWidth As Single = 60.0F
    Private Const AxisHeight As Single = 25.0F
    Private Const SubChartHeight As Single = 80.0F  ' 각 서브차트 높이
    Private Const SubChartMinHeight As Single = 60.0F

    Private MainRect As SKRect
    Private SubCharts As New List(Of SubChartInfo)
    Private PriceRect As SKRect
    Private TimeRect As SKRect

    ' Legend Hit Testing
    Private _legendItems As New List(Of LegendItem)

    ' Generic Series Storage - THIS IS THE SINGLE SOURCE OF TRADUTH FOR INDICATORS
    Private _customSeriesList As New List(Of CustomSeries)

    ' 차트 상태 (X축)
    Private _candleWidth As Single = 8.0F
    Private _gap As Single = 2.0F

    Private _isUpdating As Boolean = False ' [Added] Update suppression logic

    Public Sub BeginUpdate()
        _isUpdating = True
    End Sub

    Public Sub EndUpdate()
        _isUpdating = False
        RefreshChart()
    End Sub

    Private _scrollOffset As Single = -1
    Private _selectedSeriesName As String = ""
    Private _autoScroll As Boolean = True

    ' Y축 스케일링
    Private _isAutoScaleY As Boolean = True
    Private _leftAxisMode As LeftAxisModeType = LeftAxisModeType.None
    Private _leftAxisRect As SKRect
    Private _manualMaxP As Single = 0
    Private _manualMinP As Single = 0

    ' 마우스 상태
    Private _mouseX As Single = -1
    Private _mouseY As Single = -1
    Private _isDraggingChart As Boolean = False
    Private _isDraggingPrice As Boolean = False
    Private _lastMouseX As Single = 0
    Private _lastMouseY As Single = 0
    Private _isDraggingSplitter As Boolean = False
    Private _activeSplitter As SplitterInfo? = Nothing
    Private _splitterRects As New List(Of SplitterInfo)
    Private _panelWeights As New Dictionary(Of String, Single)

    Private Structure SplitterInfo
        Public Rect As SKRect
        Public PanelAbove As String
        Public PanelBelow As String
    End Structure

    ' Injected Managers
    Private _stockManager As StockManager
    Private _candleManager As CandleManager
    Private _indicatorManager As IndicatorManager

    ' Dynamic Charting Engine
    Private Const MAIN_PANEL_NAME As String = "Main"
    Private _activePanels As New List(Of String) ' List of active panel names

    ' 종목 정보
    Private _stockCode As String = ""
    Private _stockName As String = ""
    Private _currentTimeFrame As TimeFrame = TimeFrame.Min1

    ' [REMOVED: All _showSMA, _showEMA, _showRSI flags]
    ' [REMOVED: All _bbPeriod, _rsiPeriod parameters]

    Private _showCrosshair As Boolean = True
    Private _showDaySeparators As Boolean = True
#End Region

#Region "속성"
    Public Property StockCode As String
        Get
            Return _stockCode
        End Get
        Set(value As String)
            _stockCode = value
        End Set
    End Property

    Public Property StockName As String
        Get
            Return _stockName
        End Get
        Set(value As String)
            _stockName = value
        End Set
    End Property

    Public Property CurrentTimeFrame As TimeFrame
        Get
            Return _currentTimeFrame
        End Get
        Set(value As TimeFrame)
            _currentTimeFrame = value
        End Set
    End Property

    Public Property CandleWidth As Single
        Get
            Return _candleWidth
        End Get
        Set(value As Single)
            _candleWidth = Math.Max(1.0F, Math.Min(50.0F, value))
            RefreshChart()
        End Set
    End Property

    Public Property VisibleCount As Integer
        Get
            Dim w = Me.Width - 100 ' Approx Chart Area
            If w <= 0 Then w = 1000
            If _candleWidth <= 0 Then Return 100
            Return CInt(w / _candleWidth)
        End Get
        Set(value As Integer)
            If value < 10 Then value = 10
            If value > 5000 Then value = 5000

            Dim w = Me.Width - 100 ' Approx Chart Area
            If w <= 0 Then w = 1000

            _candleWidth = w / value
            RefreshChart()
        End Set
    End Property

    Public ReadOnly Property VisibleStartIndex As Integer
        Get
            Return Math.Max(0, CInt(Math.Floor(_scrollOffset)))
        End Get
    End Property

    Public ReadOnly Property VisibleEndIndex As Integer
        Get
            Dim visibleBars = CInt(MainRect.Width / (_candleWidth + _gap)) + 2
            Return Math.Min(Data.Count - 1, VisibleStartIndex + visibleBars)
        End Get
    End Property

    Public Sub Recalculate()
        ' No-op or trigger generic refresh
        RefreshChart()
    End Sub

    ''' <summary>
    ''' Returns the panel name at the specified Y-coordinate.
    ''' </summary>
    Public Function GetPanelAt(y As Single) As String
        ' Check Main Chart
        If y >= MainRect.Top AndAlso y <= MainRect.Bottom Then
            Return MAIN_PANEL_NAME
        End If

        ' Check SubCharts
        For Each sc In SubCharts
            If y >= sc.Rect.Top AndAlso y <= sc.Rect.Bottom Then
                Return sc.PanelName
            End If
        Next

        Return MAIN_PANEL_NAME ' Default
    End Function
#End Region

#Region "생성자"
    Public Sub New()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)

        skControl = New SKControl()
        skControl.Dock = DockStyle.Fill
        Me.Controls.Add(skControl)

        ' Default Volume or user preference can be set here or via Configure
        ' _activePanels.Add("Volume") ' If we implement generic volume

        AddHandler skControl.PaintSurface, AddressOf OnPaintSurface
        AddHandler skControl.Resize, AddressOf SKControl_Resize
        AddHandler skControl.MouseDown, AddressOf SKControl_MouseDown
        AddHandler skControl.MouseMove, AddressOf SKControl_MouseMove
        AddHandler skControl.MouseUp, AddressOf SKControl_MouseUp
        AddHandler skControl.MouseWheel, AddressOf OnWheel
        AddHandler skControl.DoubleClick, AddressOf SKControl_DoubleClick
        AddHandler skControl.MouseLeave, AddressOf SKControl_MouseLeave

        ' [Added] Key Handling for Delete
        AddHandler skControl.PreviewKeyDown, Sub(s, ek)
                                                 If ek.KeyCode = Keys.Delete AndAlso Not String.IsNullOrEmpty(_selectedSeriesName) Then
                                                     RaiseEvent IndicatorDeleted(Me, _selectedSeriesName)
                                                     _selectedSeriesName = ""
                                                     RefreshChart()
                                                 End If
                                             End Sub
    End Sub
#End Region

#Region "서브차트 관리"
    ''' <summary>
    ''' 서브차트 지표 추가
    ''' </summary>
    ' [REMOVED: AddSubChart, RemoveSubChart, ToggleSubChart using IndicatorType]

    ''' <summary>
    ''' Configures the chart panels/series based on Strategy Metadata.
    ''' This replaces the hardcoded "ShowRSI", "ShowMA" flags.
    ''' </summary>
    Public Sub Configure(configs As Dictionary(Of String, PlotConfig))
        _customSeriesList.Clear()
        ' _activePanels.Clear() ' Managed by Layout

        ' Parse Configs -> CustomSeries
        For Each kvp In configs
            Dim cfg = kvp.Value
            Dim panelName = If(String.IsNullOrEmpty(cfg.PanelName), MAIN_PANEL_NAME, cfg.PanelName)
            Dim pType = If(panelName = MAIN_PANEL_NAME, PanelType.Overlay, PanelType.Bottom)

            ' FIX: Use Key as Identifier (SeriesName), Name as Title (Display)
            Dim sKey = kvp.Key
            Dim sTitle = If(Not String.IsNullOrEmpty(cfg.Title), cfg.Title, If(String.IsNullOrEmpty(cfg.Name), kvp.Key, cfg.Name))

            Dim cs As New CustomSeries With {
                .SeriesName = sKey,
                .Title = sTitle,
                .PanelName = panelName,
                .PanelType = pType,
                .Color = New SKColor(cfg.Color.R, cfg.Color.G, cfg.Color.B, cfg.Color.A),
                .ColorUp = If(cfg.ColorUp.HasValue, New SKColor(cfg.ColorUp.Value.R, cfg.ColorUp.Value.G, cfg.ColorUp.Value.B, cfg.ColorUp.Value.A), CType(Nothing, SKColor?)),
                .ColorDown = If(cfg.ColorDown.HasValue, New SKColor(cfg.ColorDown.Value.R, cfg.ColorDown.Value.G, cfg.ColorDown.Value.B, cfg.ColorDown.Value.A), CType(Nothing, SKColor?)),
                .Style = cfg.Type,
                .Values = New List(Of Double)(),
                .Thickness = cfg.Thickness,
                .BaseLine = cfg.BaseLine,
                .Overbought = cfg.Overbought,
                .Oversold = cfg.Oversold
            }
            _customSeriesList.Add(cs)
        Next

        UpdateLayout()
        RefreshChart()
    End Sub

    ''' <summary>
    ''' Feeds data into the configured series.
    ''' Call this inside the Simulation Loop or Realtime Update.
    ''' Ensure EVERY series gets a value (NaN if missing) to maintain sync with Data.Count
    ''' </summary>
    Public Sub StreamData(values As Dictionary(Of String, Double))
        ' Iterate over ALL custom series to ensure synchronization
        For Each series In _customSeriesList
            If values.ContainsKey(series.SeriesName) Then
                series.Values.Add(values(series.SeriesName))
            Else
                ' If value missing for this tick, add NaN to keep list length aligned with Candles
                series.Values.Add(Double.NaN)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Sets the entire data series for a specific panel/indicator.
    ''' Useful for bulk loading historical indicators.
    ''' </summary>
    Public Sub SetSeriesData(seriesName As String, data As List(Of Double))
        Dim series = _customSeriesList.FirstOrDefault(Function(s) s.SeriesName = seriesName)
        If series IsNot Nothing Then
            series.Values = data
        End If
    End Sub

    ''' <summary>
    ''' Sets directional/trend data for coloring.
    ''' </summary>
    Public Sub SetSeriesTrendData(seriesName As String, data As List(Of Double))
        Dim series = _customSeriesList.FirstOrDefault(Function(s) s.SeriesName = seriesName)
        If series IsNot Nothing Then
            series.TrendValues = data
        End If
    End Sub

    ' [REMOVED: GetOrCreatePanel, RecalculatePanelLayout, HasSubChart, ClearSubCharts]
#End Region

#Region "공개 메서드"
    ''' <summary>
    ''' StockData 객체로부터 차트 데이터 로드 (외부 데이터 주입)
    ''' </summary>
    ' Reference Prices for Left Axis
    Public Property YesterdayClose As Double = 0
    Public Property TodayOpen As Double = 0

    ' Guideline Properties
    Public Property ShowBuyReserve As Boolean = False
    Public Property ShowStopLoss As Boolean = False
    Public Property ShowVILines As Boolean = True

    Public Property ViPrice1 As Double = 0
    Public Property ViPrice2 As Double = 0
    Private _firstCandleMid As Double = 0

    Public Property LeftAxisMode As LeftAxisModeType
        Get
            Return _leftAxisMode
        End Get
        Set(value As LeftAxisModeType)
            If _leftAxisMode <> value Then
                _leftAxisMode = value
                UpdateLayout()
                skControl.Invalidate()
            End If
        End Set
    End Property

    ' Strategy Info
    Public Property StrategyName As String = ""
    Public Property StrategyMode As String = ""

    Public Property ShowDaySeparators As Boolean
        Get
            Return _showDaySeparators
        End Get
        Set(value As Boolean)
            _showDaySeparators = value
            RefreshChart()
        End Set
    End Property

    Public Sub LoadStockData(stock As Stock)
        If stock Is Nothing Then Return

        ' Set Base Prices
        Me.YesterdayClose = stock.YesterdayClose
        If Me.YesterdayClose > 0 Then
            Me.ViPrice1 = Me.YesterdayClose * 1.1
            Me.ViPrice2 = Me.YesterdayClose * 1.2
        End If

        LoadStockData(stock.Code, stock.Name, stock.Candles)
    End Sub

    Public Sub LoadStockData(code As String, name As String, candles As List(Of Candle))
        Me.StockCode = code
        Me.StockName = name
        ' Me.YesterdayClose = candles.Last().Close ' Optional: set base price

        ' 1. 캔들 데이터 변환
        Data.Clear()
        If candles IsNot Nothing Then
            For Each c In candles
                Data.Add(New OHLCV With {
                    .DateVal = c.Time,
                    .TimeStr = c.Time.ToString("HH:mm"),
                    .Open = CSng(c.Open),
                    .High = CSng(c.High),
                    .Low = CSng(c.Low),
                    .Close = CSng(c.Close),
                    .Volume = CLng(c.Volume)
                })
            Next
        End If

        ' Calculate First Candle Mid
        _firstCandleMid = 0
        If Data.Count > 0 Then
            Dim lastDate = Data.Last().DateVal.Date
            Dim firstCandle = Data.FirstOrDefault(Function(x) x.DateVal.Date = lastDate)
            If firstCandle.Close > 0 Then
                _firstCandleMid = (firstCandle.Open + firstCandle.Close) / 2.0
                Me.TodayOpen = firstCandle.Open
            End If
        End If

        ' 2. 지표 데이터 초기화 및 계산 [REMOVED]
        ' Strategies managed via CustomSeries.

        If _scrollOffset < 0 OrElse _autoScroll Then
            ScrollToEnd()
        End If

        _signals.Clear()
        RefreshChart()
    End Sub

    Public Sub UpdateRealtimeStock(stock As Stock, Optional candlesOverride As List(Of Candle) = Nothing)
        If stock Is Nothing OrElse stock.Code <> _stockCode Then Return

        ' Use provided list or default to stock.Candles
        Dim srcList = If(candlesOverride, stock.Candles)
        If srcList Is Nothing OrElse srcList.Count = 0 Then Return

        Dim dstList = Data

        Dim srcCount = srcList.Count
        Dim dstCount = dstList.Count
        Dim diff = srcCount - dstCount

        ' If data stream reset or gap too large, force full reload
        If diff < 0 OrElse diff > 200 Then
            LoadStockData(stock.Code, stock.Name, srcList)
            Return
        End If

        ' Resize Indicator Arrays if Growing [REMOVED]

        ' Append/Update Candles and Recalculate
        ' FIX: Start from dstCount - 1 to update the last bar's current OHLC price
        Dim startIdx = Math.Max(0, dstCount - 1)
        For i = startIdx To srcCount - 1
            Dim c = srcList(i)
            Dim ohlcv As New OHLCV With {
                    .DateVal = c.Time,
                    .TimeStr = c.Time.ToString("HH:mm"),
                    .Open = CSng(c.Open),
                    .High = CSng(c.High),
                    .Low = CSng(c.Low),
                    .Close = CSng(c.Close),
                    .Volume = CLng(c.Volume)
            }
            If i < dstList.Count Then dstList(i) = ohlcv Else dstList.Add(ohlcv)
        Next

        ' RecalculateAllIndicators Removed

        ' Sync Strategy Signals
        SyncSignals(stock)

        ' Auto-Scroll if we were at the end
        If _autoScroll Then
            ScrollToEnd()
        End If

        RefreshChart()
    End Sub

    Public Sub UpdateSignals(stock As Stock)
        SyncSignals(stock)
    End Sub

    Private Sub SyncSignals(stock As Stock)
        If stock Is Nothing Then Return

        _signals.Clear()
        If stock.Signals Is Nothing Then Return

        ' Lock if necessary, though this is usually UI thread
        Dim cmds = stock.Signals.ToList()

        For Each cmd In cmds
            Dim st As SignalType
            Select Case cmd.Type
                Case OrderType.Buy : st = SignalType.Buy
                Case OrderType.Sell : st = SignalType.Sell
                Case Else : Continue For
            End Select

            _signals.Add(New SignalMarker With {
                .Time = cmd.Timestamp,
                .Type = st,
                .Price = CDbl(cmd.Price),
                .Note = cmd.Comment
            })
        Next
    End Sub



    Private Sub CopyListToArray(src As List(Of Double), dst() As Double)
        Dim count = Math.Min(src.Count, dst.Length)
        For i = 0 To count - 1
            dst(i) = src(i)
        Next
        ' 나머지는 NaN 처리? 리스트가 짧다면 앞부분은 0 또는 NaN?
        ' 보통 리스트와 배열 길이가 같거나 리스트가 더 김 (오래된 데이터).
        ' 간단히 0부터 복사.
    End Sub

    ''' <summary>
    ''' OHLCV 데이터 로드
    ''' </summary>
    Public Sub LoadData(newData As List(Of OHLCV))
        Data.Clear()
        If newData IsNot Nothing AndAlso newData.Count > 0 Then
            Data.AddRange(newData)
            ScrollToEnd()
        End If
        RefreshChart()
    End Sub

    ''' <summary>
    ''' 단일 캔들 추가 (실시간 업데이트용)
    ''' </summary>
    Public Sub AddCandle(candle As OHLCV)
        Data.Add(candle)
        RefreshChart()
    End Sub

    ''' <summary>
    ''' 마지막 캔들 업데이트 (실시간 틱 업데이트용)
    ''' </summary>
    Public Sub UpdateLastCandle(candle As OHLCV)
        If Data.Count > 0 Then
            Data(Data.Count - 1) = candle
            RefreshChart()
        End If
    End Sub

    ''' <summary>
    ''' 마지막 거래일의 데이터를 화면에 꽉 차게 표시 (이전 context 포함)
    ''' </summary>
    Public Sub FocusOnLastDay(trailingCount As Integer)
        If Data.Count = 0 Then Return

        ' 1. 마지막 캔들의 일자 파악
        Dim lastDate = Data.Last().DateVal.Date

        ' 2. 해당 일자의 시작 인덱스 찾기
        Dim firstIdxOfDay = Data.FindIndex(Function(x) x.DateVal.Date = lastDate)
        If firstIdxOfDay = -1 Then firstIdxOfDay = Math.Max(0, Data.Count - 100)

        ' 3. 시작 위치 결정 (여유분 포함)
        Dim startIdx = Math.Max(0, firstIdxOfDay - trailingCount)
        Dim endIdx = Data.Count - 1
        Dim countToShow = (endIdx - startIdx) + 10 ' 우측 여백 포함

        ' 4. 레이아웃에 맞춰 캔들 너비 자동 조절
        Dim chartWidth = If(MainRect.Width > 0, MainRect.Width, Me.Width - AxisWidth - LeftAxisWidth)
        If chartWidth <= 0 Then chartWidth = 800
        
        _candleWidth = CSng(chartWidth / countToShow) - _gap
        _candleWidth = Math.Max(1.0F, Math.Min(50.0F, _candleWidth))
        
        ' 5. 스크롤 위치 고정
        _scrollOffset = CSng(startIdx)
        _autoScroll = False
        
        ' 6. Y축 스케일링 초기화 (자동)
        _isAutoScaleY = True
        
        RefreshChart()
    End Sub


    ''' <summary>
    ''' 차트 끝으로 스크롤
    ''' </summary>
    Public Sub ScrollToEnd()
        Dim w = If(MainRect.Width > 0, MainRect.Width, Me.Width - AxisWidth - LeftAxisWidth)
        If w <= 0 Then w = 500

        Dim visibleBars = CInt(w / (_candleWidth + _gap))
        If visibleBars <= 0 Then visibleBars = 100

        ' Ensure we scroll to the very end
        If Data.Count > 0 Then
            _scrollOffset = Math.Max(0, Data.Count - visibleBars + 8)
            ' Force immediate repaint request
            RefreshChart()
        End If
    End Sub


    ''' <summary>
    ''' 차트 처음으로 스크롤
    ''' </summary>
    Public Sub ScrollToStart()
        _scrollOffset = 0
        _autoScroll = False
        RefreshChart()
    End Sub

    ''' <summary>
    ''' 특정 날짜로 스크롤
    ''' </summary>
    Public Sub ScrollToDate(targetDate As DateTime)
        For i As Integer = 0 To Data.Count - 1
            If Data(i).DateVal >= targetDate Then
                _scrollOffset = Math.Max(0, i - 10)
                RefreshChart()
                Exit For
            End If
        Next
    End Sub

    ''' <summary>
    ''' 테마 변경
    ''' </summary>
    Public Sub SetTheme(theme As ChartTheme)
        CurrentTheme = theme
        RefreshChart()
    End Sub

    ''' <summary>
    ''' 차트 새로고침
    ''' </summary>
    Public Sub RefreshChart()
        If _isUpdating Then Return
        skControl?.Invalidate()
    End Sub

    ''' <summary>
    ''' 데이터 초기화
    ''' </summary>
    Public Sub ClearData()
        Data.Clear()
        _scrollOffset = 0
        RefreshChart()
    End Sub

    Public Sub ZoomIn()
        CandleWidth = _candleWidth * 1.2F
    End Sub

    Public Sub ZoomOut()
        CandleWidth = _candleWidth / 1.2F
    End Sub

    Public Sub ResetYScale()
        _isAutoScaleY = True
        RefreshChart()
    End Sub

    Public Function CaptureToImage() As Bitmap
        Dim bmp As New Bitmap(skControl.Width, skControl.Height)
        skControl.DrawToBitmap(bmp, New Rectangle(0, 0, bmp.Width, bmp.Height))
        Return bmp
    End Function

    Public Function GetDataAt(index As Integer) As OHLCV?
        If index >= 0 AndAlso index < Data.Count Then
            Return Data(index)
        End If
        Return Nothing
    End Function

    ' --- [Added] New Methods for Strategy Manager ---

    ' Data Series Management
    Public ReadOnly Property CustomSeriesList As List(Of CustomSeries)
        Get
            Return _customSeriesList
        End Get
    End Property

    Public Sub Clear()
        Data.Clear()
        _customSeriesList.Clear()
        ' _activePanels.Clear()
        _signals.Clear()
        _scrollOffset = 0
        UpdateLayout()
        RefreshChart()
    End Sub

    Public Property Candles As List(Of Candle)
        Get
            Return Nothing ' Not implemented for read
        End Get
        Set(value As List(Of Candle))
            LoadStockData("SIM", "Simulation", value)
        End Set
    End Property

    Public Sub AddPanelSeries(panelName As String, seriesName As String, data As List(Of Double), color As System.Drawing.Color, type As PanelType)
        Dim newItem As New CustomSeries With {
            .PanelName = panelName,
            .SeriesName = seriesName,
            .Values = data,
            .Color = New SKColor(color.R, color.G, color.B, color.A),
            .PanelType = type
        }
        _customSeriesList.Add(newItem)
        UpdateLayout()
        RefreshChart()
    End Sub

    Public Sub Redraw()
        RefreshChart()
    End Sub

    Public Sub SetYAxisRange(panelName As String, min As Double, max As Double)
        ' Find custom series or panel and set fixed range hint
        For Each s In _customSeriesList
            If s.PanelName = panelName Then
                s.MinValue = min
                s.MaxValue = max
            End If
        Next
    End Sub

#End Region

#Region "지표 계산"
    ' [REMOVED] All internal calculation logic. 
    ' Chart is now a pure renderer. 
    ' Strategies must pre-calculate and feed data via AddPanelSeries.
#End Region


#Region "레이아웃"
    ' --- Signals ---
    Public Class SignalMarker
        Public Property Time As DateTime
        Public Property Type As SignalType ' Buy/Sell
        Public Property Price As Double
        Public Property Index As Integer ' Calculated on Draw
        Public Property Note As String
    End Class

    Public Enum SignalType
        Buy
        Sell
        ExitBuy
        ExitSell
    End Enum

    ' --- Highlight Zones ---
    Public Class HighlightZone
        Public Property StartTime As DateTime
        Public Property EndTime As DateTime
        Public Property Color As SKColor
        Public Property Note As String
    End Class

    Private _highlightZones As New List(Of HighlightZone)
    Public ReadOnly Property HighlightZones As List(Of HighlightZone)
        Get
            Return _highlightZones
        End Get
    End Property

    Private _signals As New List(Of SignalMarker)
    Public ReadOnly Property Signals As List(Of SignalMarker)
        Get
            Return _signals
        End Get
    End Property

    Public Sub SetSignals(trades As List(Of TradeRecord))
        _signals.Clear()
        If trades Is Nothing Then Return

        For Each t In trades
            ' Entry
            _signals.Add(New SignalMarker With {.Time = t.EntryTime, .Type = If(t.Side = "Buy", SignalType.Buy, SignalType.Sell), .Price = t.EntryPrice, .Note = "Entry"})
            ' Exit
            If t.Exittime > DateTime.MinValue Then
                _signals.Add(New SignalMarker With {.Time = t.Exittime, .Type = If(t.Side = "Buy", SignalType.ExitBuy, SignalType.ExitSell), .Price = t.ExitPrice, .Note = "Exit"})
            End If
        Next
        RefreshChart()
    End Sub

    Public Function GetSnapshot() As System.Drawing.Bitmap
        Dim width = skControl.Width
        Dim height = skControl.Height
        Dim bmp As New System.Drawing.Bitmap(width, height)

        ' Draw to Bitmap
        Using surface = SKSurface.Create(New SKImageInfo(width, height))
            Dim canvas = surface.Canvas

            ' Re-use OnPaint logic (We need to synthesize an event args or refactor OnPaint logic to a shared function)
            ' For now, manually trigger a repaint on this canvas? No, easiest is to extract main draw logic.
            ' Simulating Render:
            Dim e As New SKPaintSurfaceEventArgs(surface, New SKImageInfo(width, height))
            OnPaintSurface(Me, e)

            ' Convert to Bitmap
            Using image = surface.Snapshot()
                Using data = image.Encode(SKEncodedImageFormat.Png, 100)
                    Using stream = data.AsStream()
                        bmp = New System.Drawing.Bitmap(stream)
                    End Using
                End Using
            End Using
        End Using

        Return bmp
    End Function

    Private Sub DrawHighlightZones(canvas As SKCanvas, startIndex As Integer, endIndex As Integer)
        If _highlightZones Is Nothing OrElse _highlightZones.Count = 0 Then Return

        Using paint As New SKPaint With {.Style = SKPaintStyle.Fill, .IsAntialias = True},
              textPaint As New SKPaint With {
                  .Color = SKColors.Gray, 
                  .TextSize = 10, 
                  .IsAntialias = True,
                  .Typeface = SKTypeface.FromFamilyName("Malgun Gothic")
              }
            
            For Each zone In _highlightZones
                Dim sIdx = Data.FindIndex(Function(c) c.DateVal >= zone.StartTime)
                Dim eIdx = Data.FindIndex(Function(c) c.DateVal >= zone.EndTime)
                If sIdx = -1 Then sIdx = 0
                If eIdx = -1 Then eIdx = Data.Count - 1
                
                If eIdx >= startIndex AndAlso sIdx <= endIndex Then
                    Dim xStart = GetX(Math.Max(sIdx, startIndex))
                    Dim xEnd = GetX(Math.Min(eIdx, endIndex)) + _candleWidth
                    
                    Dim rect = New SKRect(xStart, MainRect.Top, xEnd, MainRect.Bottom)
                    paint.Color = zone.Color
                    canvas.DrawRect(rect, paint)
                    
                    If Not String.IsNullOrEmpty(zone.Note) AndAlso sIdx >= startIndex Then
                        canvas.DrawText(zone.Note, xStart + 2, MainRect.Bottom - 5, textPaint)
                    End If
                End If
            Next
        End Using
    End Sub

    Private Sub DrawStrategyMarkers(canvas As SKCanvas, startIndex As Integer, endIndex As Integer, maxP As Single, minP As Single)
        If _signals Is Nothing OrElse _signals.Count = 0 Then Return

        Using paintBuy As New SKPaint With {.Color = SKColor.Parse("#00C853"), .Style = SKPaintStyle.Fill, .IsAntialias = True},
              paintSell As New SKPaint With {.Color = SKColor.Parse("#D50000"), .Style = SKPaintStyle.Fill, .IsAntialias = True},
              textPaint As New SKPaint With {
                  .Color = SKColors.White, 
                  .TextSize = 10, 
                  .IsAntialias = True, 
                  .TextAlign = SKTextAlign.Center,
                  .Typeface = SKTypeface.FromFamilyName("Malgun Gothic", SKFontStyle.Bold)
              },
              bgPaint As New SKPaint With {.Color = SKColors.Black.WithAlpha(150), .IsAntialias = True}
              
            For Each sig In _signals
                Dim idx = Data.FindIndex(Function(c) c.DateVal = sig.Time)
                If idx >= startIndex AndAlso idx <= endIndex Then
                    Dim x = GetX(idx) + _candleWidth / 2
                    Dim y = GetY(CSng(sig.Price), maxP, minP, MainRect)

                    If sig.Type = SignalType.Buy Then
                        ' Premium Buy Marker: Circle + Triangle
                        canvas.DrawCircle(x, y + 26, 9, bgPaint)
                        
                        Dim path As New SKPath()
                        path.MoveTo(x, y + 12)
                        path.LineTo(x - 6, y + 20)
                        path.LineTo(x + 6, y + 20)
                        path.Close()
                        canvas.DrawPath(path, paintBuy)
                        
                        Dim note = If(String.IsNullOrEmpty(sig.Note), "B", sig.Note)
                        canvas.DrawText(note, x, y + 36, textPaint)
                    ElseIf sig.Type = SignalType.Sell Then
                        ' Premium Sell Marker
                        canvas.DrawCircle(x, y - 26, 9, bgPaint)
                        
                        Dim path As New SKPath()
                        path.MoveTo(x, y - 12)
                        path.LineTo(x - 6, y - 20)
                        path.LineTo(x + 6, y - 20)
                        path.Close()
                        canvas.DrawPath(path, paintSell)
                        
                        Dim note = If(String.IsNullOrEmpty(sig.Note), "S", sig.Note)
                        canvas.DrawText(note, x, y - 36, textPaint)
                    ElseIf sig.Type = SignalType.ExitBuy OrElse sig.Type = SignalType.ExitSell Then
                        ' Sleek X Mark
                        Using strokePaint As New SKPaint With {.Color = SKColors.Yellow, .StrokeWidth = 2, .IsAntialias = True}
                            canvas.DrawLine(x - 5, y - 5, x + 5, y + 5, strokePaint)
                            canvas.DrawLine(x + 5, y - 5, x - 5, y + 5, strokePaint)
                        End Using
                    End If
                End If
            Next
        End Using
    End Sub

    Private Sub UpdateLayout()
        If skControl Is Nothing Then Return

        Dim w = skControl.Width
        Dim h = skControl.Height
        Dim leftMargin = If(_leftAxisMode <> LeftAxisModeType.None, LeftAxisWidth, 0.0F)

        ' Available height excluding time axis
        Dim totalAvailableHeight = h - AxisHeight
        If totalAvailableHeight < 50 Then totalAvailableHeight = 50 

        SubCharts.Clear()
        _splitterRects.Clear()

        ' Filter bottom panels (CustomSeries only now)
        Dim bottomPanels = _customSeriesList.Where(Function(cs) cs.PanelType = PanelType.Bottom) _
                                            .Select(Function(cs) cs.PanelName) _
                                            .Distinct() _
                                            .ToList()

        ' Ensure Weights exist for all active panels
        If Not _panelWeights.ContainsKey(MAIN_PANEL_NAME) Then _panelWeights(MAIN_PANEL_NAME) = 3.0F
        For Each pName In bottomPanels
            If Not _panelWeights.ContainsKey(pName) Then _panelWeights(pName) = 1.2F
        Next

        ' Remove weights for panels no longer active
        Dim activeKeys = New List(Of String) From {MAIN_PANEL_NAME}
        activeKeys.AddRange(bottomPanels)
        Dim keysToRemove = _panelWeights.Keys.Where(Function(k) Not activeKeys.Contains(k)).ToList()
        For Each k In keysToRemove
            _panelWeights.Remove(k)
        Next

        If bottomPanels.Count > 0 Then
            ' [Proportional Weight-Based Layout]
            Dim totalWeight As Single = 0
            For Each pName In activeKeys
                totalWeight += _panelWeights(pName)
            Next

            Dim unitHeight = totalAvailableHeight / totalWeight
            
            ' 1. Main Chart Area
            Dim mainHeight = unitHeight * _panelWeights(MAIN_PANEL_NAME)
            MainRect = New SKRect(leftMargin, 0, w - AxisWidth, mainHeight)
            PriceRect = New SKRect(w - AxisWidth, 0, w, mainHeight)
            If _leftAxisMode <> LeftAxisModeType.None Then
                _leftAxisRect = New SKRect(0, 0, leftMargin, mainHeight)
            End If

            ' 2. Sub Chart Areas and Splitters
            Dim currentY As Single = mainHeight
            Dim prevPanel = MAIN_PANEL_NAME

            For Each pName In bottomPanels
                ' Splitter between prevPanel and pName
                Dim splitRect = New SKRect(0, currentY - 3, w, currentY + 3)
                _splitterRects.Add(New SplitterInfo With {.Rect = splitRect, .PanelAbove = prevPanel, .PanelBelow = pName})

                Dim pHeight = unitHeight * _panelWeights(pName)
                Dim subInfo As New SubChartInfo()
                subInfo.PanelName = pName
                subInfo.Rect = New SKRect(leftMargin, currentY, w - AxisWidth, currentY + pHeight)
                subInfo.PriceRect = New SKRect(w - AxisWidth, currentY, w, currentY + pHeight)
                SubCharts.Add(subInfo)

                currentY += pHeight
                prevPanel = pName
            Next
        Else
            ' No sub-charts
            MainRect = New SKRect(leftMargin, 0, w - AxisWidth, totalAvailableHeight)
            PriceRect = New SKRect(w - AxisWidth, 0, w, totalAvailableHeight)
            If _leftAxisMode <> LeftAxisModeType.None Then
                _leftAxisRect = New SKRect(0, 0, leftMargin, totalAvailableHeight)
            End If
        End If
        Dim timeAxisLeft = If(_leftAxisMode <> LeftAxisModeType.None, LeftAxisWidth, 0.0F)
        TimeRect = New SKRect(timeAxisLeft, h - AxisHeight, w - AxisWidth, h)
    End Sub

#End Region

#Region "그리기"
    Private Sub OnPaintSurface(sender As Object, e As SKPaintSurfaceEventArgs)
        Dim canvas = e.Surface.Canvas
        canvas.Clear(CurrentTheme.Background)

        If Data.Count = 0 Then
            DrawNoDataMessage(canvas)
            Return
        End If

        Dim startIndex = VisibleStartIndex
        Dim endIndex = VisibleEndIndex

        If startIndex >= endIndex Then Return

        ' Y축 스케일 계산
        Dim autoMaxP As Single = Single.MinValue
        Dim autoMinP As Single = Single.MaxValue

        For i = startIndex To endIndex
            autoMaxP = Math.Max(autoMaxP, Data(i).High)
            autoMinP = Math.Min(autoMinP, Data(i).Low)
        Next

        Dim padding = (autoMaxP - autoMinP) * 0.05F
        autoMaxP += padding
        autoMinP -= padding
        If autoMaxP = autoMinP Then autoMaxP += 1.0F

        Dim finalMaxP, finalMinP As Single
        If _isAutoScaleY Then
            finalMaxP = autoMaxP
            finalMinP = autoMinP
            _manualMaxP = autoMaxP
            _manualMinP = autoMinP
        Else
            finalMaxP = _manualMaxP
            finalMinP = _manualMinP
        End If

        ' 1. 메인 차트 및 오버레이 그리기 (Clipping 적용)
        canvas.Save()
        canvas.ClipRect(MainRect)
        Try
            ' 1.0 Setup Zones (Background Highlight)
            DrawHighlightZones(canvas, startIndex, endIndex)

            ' 1.1 메인 캔들
            DrawMainChart(canvas, startIndex, endIndex, finalMaxP, finalMinP)

            ' 1.2 Boxes (Smart Money Breakout)
            If Boxes IsNot Nothing AndAlso Boxes.Count > 0 Then
                Using paintBox As New SKPaint With {.Style = SKPaintStyle.Fill}
                    For Each b In Boxes
                        If Not b.IsActive AndAlso Not b.IsBullishBreakout AndAlso Not b.IsBearishBreakout Then Continue For
                        If b.EndIndex < startIndex Or b.StartIndex > endIndex Then Continue For

                        Dim x1 = GetX(Math.Max(b.StartIndex, startIndex))
                        Dim x2 = GetX(Math.Min(b.EndIndex, endIndex))
                        Dim width = x2 - x1
                        If width < 1 Then width = CandleWidth

                        Dim yTop = GetY(CSng(b.Top), finalMaxP, finalMinP, MainRect)
                        Dim yBottom = GetY(CSng(b.Bottom), finalMaxP, finalMinP, MainRect)
                        Dim h = Math.Abs(yBottom - yTop)

                        If b.UpVolume > b.DownVolume Then
                            paintBox.Color = SKColors.Green.WithAlpha(50)
                        Else
                            paintBox.Color = SKColors.Red.WithAlpha(50)
                        End If

                        If b.IsBullishBreakout Then paintBox.Color = CurrentTheme.CandleUp.WithAlpha(40)
                        If b.IsBearishBreakout Then paintBox.Color = CurrentTheme.CandleDown.WithAlpha(40)

                        canvas.DrawRect(SKRect.Create(x1, Math.Min(yTop, yBottom), width, h), paintBox)

                        Using paintBorder As New SKPaint With {.Style = SKPaintStyle.Stroke, .StrokeWidth = 1, .Color = paintBox.Color.WithAlpha(200)}
                            canvas.DrawRect(SKRect.Create(x1, Math.Min(yTop, yBottom), width, h), paintBorder)
                        End Using

                        If b.IsBullishBreakout Then
                            Dim x = GetX(b.EndIndex) + CandleWidth / 2
                            Dim y = GetY(CSng(b.Top), finalMaxP, finalMinP, MainRect)
                            Using p As New SKPaint With {.Style = SKPaintStyle.Fill, .Color = SKColors.Red, .IsAntialias = True}
                                canvas.DrawCircle(x, y, 4, p)
                            End Using
                        ElseIf b.IsBearishBreakout Then
                            Dim x = GetX(b.EndIndex) + CandleWidth / 2
                            Dim y = GetY(CSng(b.Bottom), finalMaxP, finalMinP, MainRect)
                            Using p As New SKPaint With {.Style = SKPaintStyle.Fill, .Color = SKColors.Green, .IsAntialias = True}
                                canvas.DrawCircle(x, y, 4, p)
                            End Using
                        End If
                    Next
                End Using
            End If

            ' 1.3 Custom Overlays
            For Each cs In _customSeriesList
                If cs.PanelType = PanelType.Overlay Then
                    DrawCustomSeries(canvas, cs, startIndex, endIndex, finalMaxP, finalMinP, MainRect)
                End If
            Next

            ' 1.4 전략 신호
            DrawStrategyMarkers(canvas, startIndex, endIndex, finalMaxP, finalMinP)
        Finally
            canvas.Restore()
        End Try

        ' 3. 서브차트들 그리기
        For Each subChart In SubCharts
            DrawCustomSubChart(canvas, startIndex, endIndex, subChart)
        Next

        ' 4. 가격축 그리기
        DrawPriceAxis(canvas, finalMaxP, finalMinP)

        ' 5.1 좌측 Y축 그리기 (등락률)
        If _leftAxisMode <> LeftAxisModeType.None Then
            DrawLeftAxis(canvas, finalMaxP, finalMinP)
        End If

        ' 5.2 가이드라인 그리기 (VI, 추천매수, 손절)
        If _ShowBuyReserve Or _ShowStopLoss Or _ShowVILines Then
            DrawGuidelines(canvas, finalMaxP, finalMinP)
        End If

        ' 5. 시간축 그리기
        DrawTimeAxis(canvas, startIndex, endIndex)

        ' 6. 십자선 그리기
        If _showCrosshair AndAlso _mouseX >= 0 AndAlso _mouseY >= 0 Then
            DrawCrosshair(canvas, finalMaxP, finalMinP)
        End If

        ' 7. 종목 정보 표시
        Dim legendStartY = DrawStockInfo(canvas)

        ' 8. Legend 그리기 (지표명 + 파라미터)
        DrawLegends(canvas, legendStartY)
    End Sub

    Private Sub DrawNoDataMessage(canvas As SKCanvas)
        Using textPaint As New SKPaint With {
            .Color = CurrentTheme.Text,
            .TextSize = 16,
            .IsAntialias = True,
            .TextAlign = SKTextAlign.Center,
            .Typeface = SKTypeface.FromFamilyName("Malgun Gothic") ' Fix for Korean Text
        }
            canvas.DrawText("데이터가 없습니다", skControl.Width / 2, skControl.Height / 2, textPaint)
        End Using
    End Sub

    Private Sub DrawMainChart(canvas As SKCanvas, startIndex As Integer, endIndex As Integer, maxP As Single, minP As Single)
        canvas.Save()
        canvas.ClipRect(MainRect)

        Using paintUp As New SKPaint With {.Color = CurrentTheme.CandleUp, .Style = SKPaintStyle.Fill, .IsAntialias = False},
              paintDown As New SKPaint With {.Color = CurrentTheme.CandleDown, .Style = SKPaintStyle.Fill, .IsAntialias = False},
              paintWick As New SKPaint With {.Color = CurrentTheme.Wick, .StrokeWidth = 1, .IsAntialias = False},
              gridPaint As New SKPaint With {.Color = CurrentTheme.GridLine, .StrokeWidth = 1}

            ' 수평 그리드
            ' [FIX: Removed _showGridH field] Defaulting to False or implementation strategy? 
            ' If grids are needed, re-add generic generic properties or Config.
            ' For now, skipping to resolve compilation.
            ' If _showGridH Then
            '    ...
            ' End If

            ' 수직 그리드 (Vertical Grid)
            ' [FIX: Removed _showGridV field]
            ' If _showGridV Then
            '    ...
            ' End If

            ' 일자 변경선 (Day Separators) - Dynamic
            If _showDaySeparators AndAlso _currentTimeFrame < TimeFrame.Day Then
                Using daySepPaint As New SKPaint With {.Color = CurrentTheme.GridLine, .StrokeWidth = 1, .PathEffect = SKPathEffect.CreateDash({5, 5}, 0)}
                    Dim prevDate As DateTime = DateTime.MinValue
                    If startIndex > 0 Then prevDate = Data(startIndex - 1).DateVal.Date

                    ' Limit density: if too many days visible, skip?
                    ' Simple logic: Draw all day changes. 
                    ' If zoomed out heavily (e.g. 1 min chart showing 1 month), lines will be dense.
                    ' Check pixel distance between days?
                    ' We just draw them. If user wants to hide, they toggle.
                    ' "Dynamic" requirement: "Prevent too many lines".
                    ' We can check visible day count.
                    ' Count days in view first? No, pure iteration.

                    ' Optimization: Check if we have > 50 day changes in view.

                    For i = startIndex To endIndex
                        Dim currentDate = Data(i).DateVal.Date
                        If i > startIndex AndAlso currentDate <> prevDate Then
                            ' New Day
                            Dim x = GetX(i) ' Start of candle i
                            ' Draw line at specific X (e.g. left of candle)
                            canvas.DrawLine(x, MainRect.Top, x, MainRect.Bottom, daySepPaint)
                        End If
                        prevDate = currentDate
                    Next
                End Using
            End If

            ' 캔들 그리기
            For i = startIndex To endIndex
                Dim item = Data(i)
                Dim x = GetX(i)
                Dim yOpen = GetY(item.Open, maxP, minP, MainRect)
                Dim yClose = GetY(item.Close, maxP, minP, MainRect)
                Dim yHigh = GetY(item.High, maxP, minP, MainRect)
                Dim yLow = GetY(item.Low, maxP, minP, MainRect)

                canvas.DrawLine(x + _candleWidth / 2, yHigh, x + _candleWidth / 2, yLow, paintWick)

                Dim rect As SKRect
                If item.Open <= item.Close Then
                    rect = SKRect.Create(x, yClose, _candleWidth, Math.Max(1.0F, yOpen - yClose))
                    canvas.DrawRect(rect, paintUp)
                Else
                    rect = SKRect.Create(x, yOpen, _candleWidth, Math.Max(1.0F, yClose - yOpen))
                    canvas.DrawRect(rect, paintDown)
                End If
            Next
        End Using

        canvas.Restore()
    End Sub

    Private Sub DrawOverlayIndicators(canvas As SKCanvas, startIndex As Integer, endIndex As Integer, maxP As Single, minP As Single)
        ' [REMOVED]
    End Sub

    Private Sub DrawIndicatorLine(canvas As SKCanvas, values() As Double, startIndex As Integer, endIndex As Integer,
                                   maxP As Single, minP As Single, color As SKColor, strokeWidth As Single, rect As SKRect)
        ' [REMOVED] Can be replaced by DrawCustomLink helper if needed or just use generic logical elsewhere
    End Sub


    ' [REMOVED: GetIndicatorDisplayName]

    Private Sub DrawLegends(canvas As SKCanvas, startY As Single)
        _legendItems.Clear()

        Using paint As New SKPaint With {.TextSize = 12, .IsAntialias = True}
            ' Determine Start X based on Left Axis
            Dim startX As Single = 10
            If _leftAxisMode <> LeftAxisModeType.None Then
                startX = _leftAxisRect.Right + 10
            End If

            Dim y As Single = startY

            ' --- Overlay Legends (Left Side, Stacked Below Stock Info) ---
            For Each cs In _customSeriesList
                If cs.PanelType = PanelType.Overlay Then
                    Dim cursorIdx = If(_mouseX >= 0, GetIndexAtX(_mouseX), cs.Values.Count - 1)
                    Dim val As Double = Double.NaN
                    Dim prevVal As Double = Double.NaN

                    If cursorIdx >= 0 AndAlso cursorIdx < cs.Values.Count Then
                        val = cs.Values(cursorIdx)
                        If cursorIdx > 0 Then prevVal = cs.Values(cursorIdx - 1)
                    End If

                    ' [CRITICAL] Bold if selected
                    paint.FakeBoldText = (cs.SeriesName = _selectedSeriesName)

                    ' Prepare Text
                    paint.Color = cs.Color
                    Dim textToDraw = cs.Title
                    Dim valStr = ""
                    Dim chgStr = ""
                    Dim chgColor = SKColors.Gray

                    If Not Double.IsNaN(val) Then
                        valStr = val.ToString("F2")
                        If Not Double.IsNaN(prevVal) AndAlso prevVal <> 0 Then
                            Dim pct = (val - prevVal) / prevVal * 100.0
                            chgStr = $"({pct:F2}%)"
                            If pct > 0 Then
                                chgStr = $"(+{pct:F2}%)"
                                chgColor = SKColors.Red
                            ElseIf pct < 0 Then
                                chgColor = SKColor.Parse("#00FF00")
                            End If
                        Else
                            chgStr = "(0.00%)"
                        End If
                        textToDraw &= $" : {valStr} {chgStr}"
                    End If

                    ' Measure Text
                    Dim textWidth = paint.MeasureText(textToDraw)

                    ' Draw Background - Left Aligned
                    Dim bgRect = SKRect.Create(startX, y - 12, textWidth + 10, 16)
                    Using bgPaint As New SKPaint With {.Color = SKColors.Black.WithAlpha(150), .Style = SKPaintStyle.Fill}
                        canvas.DrawRect(bgRect, bgPaint)
                    End Using

                    ' Draw Text
                    Dim curX = startX + 5

                    ' Draw Title
                    paint.Color = cs.Color
                    canvas.DrawText(cs.Title & " : ", curX, y, paint)
                    curX += paint.MeasureText(cs.Title & " : ")

                    ' Draw Value
                    canvas.DrawText(valStr, curX, y, paint)
                    curX += paint.MeasureText(valStr & " ")

                    ' Draw Change
                    If Not String.IsNullOrEmpty(chgStr) Then
                        Dim oldC = paint.Color
                        paint.Color = chgColor
                        canvas.DrawText(chgStr, curX, y, paint)
                        paint.Color = oldC
                    End If

                    _legendItems.Add(New LegendItem With {
                         .PanelName = cs.SeriesName,
                         .HitRect = bgRect,
                         .Text = textToDraw,
                         .Color = cs.Color,
                         .IsOverlay = True
                     })

                    y += 18
                End If
            Next

                ' --- SubChart Legends (Left Side, Keep as is but add bg) ---
                For Each subChart In SubCharts
                    ' Find Series in this Panel (Case-Insensitive)
                    Dim series = _customSeriesList.Where(Function(s) String.Equals(s.PanelName, subChart.PanelName, StringComparison.OrdinalIgnoreCase)).ToList()

                Dim drawX = subChart.Rect.Left + 5
                Dim drawY = subChart.Rect.Top + 15

                For Each s In series
                    ' [CRITICAL] Bold if selected
                    paint.FakeBoldText = (s.SeriesName = _selectedSeriesName)

                    Dim text = If(s.Title, s.SeriesName)
                    If String.IsNullOrEmpty(text) Then text = "Unknown"

                    Dim cursorIdx = If(_mouseX >= 0, GetIndexAtX(_mouseX), s.Values.Count - 1)
                    If cursorIdx >= 0 AndAlso cursorIdx < s.Values.Count Then
                        Dim val = s.Values(cursorIdx)
                        If Not Double.IsNaN(val) Then text &= $" : {val:F2}"
                    End If

                    Dim textSize = paint.MeasureText(text)

                    ' Background for SubCharts too? Yes for consistency
                    Dim bgRect = SKRect.Create(drawX, drawY - 12, textSize + 6, 16)
                    Using bgPaint As New SKPaint With {.Color = SKColors.Black.WithAlpha(100), .Style = SKPaintStyle.Fill}
                        canvas.DrawRect(bgRect, bgPaint)
                    End Using

                    paint.Color = s.Color
                    canvas.DrawText(text, drawX + 3, drawY, paint)

                    _legendItems.Add(New LegendItem With {
                         .PanelName = s.SeriesName,
                         .HitRect = bgRect,
                         .Text = text,
                         .Color = s.Color,
                         .IsOverlay = False
                     })

                    ' Reset bold
                    paint.FakeBoldText = False

                    ' Increment Y for next item in THIS panel
                    drawY += 18
                Next
            Next
        End Using
    End Sub


    Public Event LegendDoubleClicked(sender As Object, seriesName As String)

    ' ...

    Public Event CandleDoubleClicked(sender As Object, e As CandleClickEventArgs)

    Private Sub SKControl_DoubleClick(sender As Object, e As EventArgs)
        Dim mousePos = skControl.PointToClient(Cursor.Position)
        Dim mx = mousePos.X
        Dim my = mousePos.Y

        ' 1. Check Legend Interaction
        For Each item In _legendItems
            If item.HitRect.Contains(mx, my) Then
                Debug.WriteLine($"Double clicked legend: {item.Text} (Name: {item.PanelName})")
                RaiseEvent LegendDoubleClicked(Me, item.PanelName)
                RaiseEvent IndicatorSettingsRequested(Me, item.PanelName)
                Return
            End If
        Next

        ' 2. Check Modifiers - Trigger Analysis
        If Control.ModifierKeys = Keys.Alt Then
            Dim idx = GetIndexAtX(mx)
            If idx >= 0 AndAlso idx < Data.Count Then
                RaiseEvent CandleDoubleClicked(Me, New CandleClickEventArgs With {
                    .Index = idx,
                    .Data = Data(idx),
                    .MouseButton = MouseButtons.Left
                })
            End If
            Return
        End If

        ResetYScale()
    End Sub


    ' [REMOVED: DrawDMISubChart, DrawSubChartLineInRect which used legacy Indicators]

    Private Function GetYForSubChart(value As Double, maxVal As Double, minVal As Double, rect As SKRect) As Single
        If maxVal = minVal Then Return rect.Top + rect.Height / 2
        Return rect.Top + CSng(rect.Height * (1.0 - (value - minVal) / (maxVal - minVal)))
    End Function

    Private Sub DrawSubChartPriceAxis(canvas As SKCanvas, subChart As SubChartInfo)
        Using axisLinePaint As New SKPaint With {.Color = CurrentTheme.Border, .StrokeWidth = 1}
            canvas.DrawLine(subChart.PriceRect.Left, subChart.Rect.Top, subChart.PriceRect.Left, subChart.Rect.Bottom, axisLinePaint)
        End Using
    End Sub

    Private Sub DrawLeftAxis(canvas As SKCanvas, maxP As Single, minP As Single)
        If _leftAxisMode = LeftAxisModeType.None Then Return

        Dim basePrice As Double = If(_leftAxisMode = LeftAxisModeType.VsPrevClose, YesterdayClose, TodayOpen)
        If basePrice <= 0 Then Return ' Avoid DivByZero

        Using axisLinePaint As New SKPaint With {.Color = CurrentTheme.Border, .StrokeWidth = 1},
              textPaint As New SKPaint With {.Color = CurrentTheme.Text, .TextSize = 11, .IsAntialias = True, .TextAlign = SKTextAlign.Right}

            canvas.DrawLine(_leftAxisRect.Right, 0, _leftAxisRect.Right, _leftAxisRect.Bottom, axisLinePaint)

            Dim desiredTickCount As Integer = Math.Max(2, CInt(MainRect.Height / 50))
            Dim yInterval = GetNiceInterval(maxP - minP, desiredTickCount)
            Dim yVal As Single = CSng(Math.Ceiling(minP / yInterval) * yInterval)

            While yVal <= maxP
                Dim yPos = GetY(yVal, maxP, minP, MainRect)
                If yPos >= -20 And yPos <= MainRect.Height + 20 Then
                    canvas.DrawLine(_leftAxisRect.Right - 4, yPos, _leftAxisRect.Right, yPos, axisLinePaint)

                    ' Calculate % Change
                    Dim pct = (yVal - basePrice) / basePrice * 100.0
                    Dim label = $"{pct:F2}%"

                    ' Colorize label based on + / -
                    If pct > 0 Then textPaint.Color = CurrentTheme.CandleUp
                    If pct < 0 Then textPaint.Color = CurrentTheme.CandleDown
                    If pct = 0 Then textPaint.Color = CurrentTheme.Text

                    canvas.DrawText(label, _leftAxisRect.Right - 6, yPos + 4, textPaint)
                End If
                yVal += CSng(yInterval)
            End While

            ' Draw Header
            textPaint.Color = CurrentTheme.Text
            textPaint.TextAlign = SKTextAlign.Center
            'Dim header = If(_leftAxisMode = LeftAxisMode.VsPrevClose, "vs Prev", "vs Open")
            'canvas.DrawText(header, _leftAxisRect.Width / 2, 15, textPaint)
        End Using
    End Sub

    Private Sub DrawGuidelines(canvas As SKCanvas, maxP As Single, minP As Single)
        Using textPaint As New SKPaint With {.TextSize = 10, .IsAntialias = True, .TextAlign = SKTextAlign.Right},
              linePaint As New SKPaint With {.StrokeWidth = 1, .PathEffect = SKPathEffect.CreateDash({2, 2}, 0)}

            Dim drawLine = Sub(price As Double, label As String, color As SKColor)
                               If price <= 0 Then Return
                               Dim y = GetY(CSng(price), maxP, minP, MainRect)
                               If y >= MainRect.Top AndAlso y <= MainRect.Bottom Then
                                   linePaint.Color = color
                                   textPaint.Color = color
                                   canvas.DrawLine(MainRect.Left, y, MainRect.Right, y, linePaint)
                                   canvas.DrawText(label, MainRect.Right - 5, y - 5, textPaint)
                               End If
                           End Sub

            If _ShowBuyReserve AndAlso _firstCandleMid > 0 Then
                Dim price = _firstCandleMid * 1.05
                drawLine(price, $"Res(+5%) {FormatPrice(price)}", SKColors.Red)
            End If

            If _ShowStopLoss AndAlso _firstCandleMid > 0 Then
                Dim price = _firstCandleMid * 0.97
                drawLine(price, $"Stop(-3%) {FormatPrice(price)}", SKColors.LimeGreen)
            End If

            If _ShowVILines Then
                ' 1. Static VI (당일 시가 대비 10%, 20%)
                ' [FIX] TodayOpen이 0일 경우 로드된 데이터로부터 추출 시도
                If TodayOpen <= 0 AndAlso Data.Count > 0 Then
                     Dim lastD = Data.Last().DateVal.Date
                     Dim firstC = Data.FirstOrDefault(Function(x) x.DateVal.Date = lastD)
                     If firstC.Open > 0 Then TodayOpen = firstC.Open
                End If

                If TodayOpen > 0 Then
                    Dim staticVi1 = TodayOpen * 1.1
                    Dim staticVi2 = TodayOpen * 1.2
                    drawLine(staticVi1, $"Static VI (+10%) {FormatPrice(staticVi1)}", SKColors.Orange)
                    drawLine(staticVi2, $"Static VI (+20%) {FormatPrice(staticVi2)}", SKColors.OrangeRed)
                    
                    ' 하락 VI
                    Dim staticViDown = TodayOpen * 0.9
                    drawLine(staticViDown, $"Static VI (-10%) {FormatPrice(staticViDown)}", SKColors.DeepSkyBlue)
                End If

                ' 2. Dynamic VI (직전 대비 급등락 리스크 구간)
                ' 실시간 데이터가 있을 때 마지막 캔들 기준으로 표시
                If Data.Count > 0 Then
                    ' 전략 엔진에서 넘겨받은 ViPrice1, ViPrice2가 있다면 그것을 우선 사용
                    ' (BindingStockData 등에서 설정됨)
                    If ViPrice1 > 0 Then
                        drawLine(ViPrice1, $"VI Target (Up) {FormatPrice(ViPrice1)}", SKColors.Gold)
                    End If
                    If ViPrice2 > 0 Then
                        drawLine(ViPrice2, $"VI Target (Dn) {FormatPrice(ViPrice2)}", SKColors.PeachPuff)
                    End If
                End If
            End If
        End Using
    End Sub

    Private Sub DrawPriceAxis(canvas As SKCanvas, maxP As Single, minP As Single)
        Using axisLinePaint As New SKPaint With {.Color = CurrentTheme.Border, .StrokeWidth = 1},
              textPaint As New SKPaint With {.Color = CurrentTheme.Text, .TextSize = 11, .IsAntialias = True, .TextAlign = SKTextAlign.Left}

            canvas.DrawLine(PriceRect.Left, 0, PriceRect.Left, PriceRect.Bottom, axisLinePaint)

            Dim desiredTickCount As Integer = Math.Max(2, CInt(MainRect.Height / 50))
            Dim yInterval = GetNiceInterval(maxP - minP, desiredTickCount)
            Dim yVal As Single = CSng(Math.Ceiling(minP / yInterval) * yInterval)

            While yVal <= maxP
                Dim yPos = GetY(yVal, maxP, minP, MainRect)
                If yPos >= -20 And yPos <= MainRect.Height + 20 Then
                    canvas.DrawLine(PriceRect.Left, yPos, PriceRect.Left + 4, yPos, axisLinePaint)
                    canvas.DrawText(FormatPrice(yVal), PriceRect.Left + 6, yPos + 4, textPaint)
                End If
                yVal += CSng(yInterval)
            End While

            If Not _isAutoScaleY Then
                Using indicatorPaint As New SKPaint With {.Color = SKColors.Orange, .TextSize = 10, .IsAntialias = True}
                    canvas.DrawText("M", PriceRect.Left + 5, MainRect.Bottom - 5, indicatorPaint)
                End Using
            End If
        End Using
    End Sub

    Private Sub DrawCustomSubChart(canvas As SKCanvas, startIndex As Integer, endIndex As Integer, subChart As SubChartInfo)
        ' 1. Find Series (Case-Insensitive)
        Dim seriesList = _customSeriesList.Where(Function(s) String.Equals(s.PanelName, subChart.PanelName, StringComparison.OrdinalIgnoreCase)).ToList()
        If seriesList.Count = 0 Then Return

        ' 2. Calculate Range
        Dim maxVal As Double = Double.MinValue
        Dim minVal As Double = Double.MaxValue
        Dim fixedRange As Boolean = False

        ' Check for fixed range first (from first series)
        If seriesList(0).MinValue.HasValue AndAlso seriesList(0).MaxValue.HasValue Then
            minVal = seriesList(0).MinValue.Value
            maxVal = seriesList(0).MaxValue.Value
            fixedRange = True
        Else
            For Each s In seriesList
                For i = startIndex To endIndex
                    If i < s.Values.Count Then
                        Dim v = s.Values(i)
                        If Not Double.IsNaN(v) Then
                            maxVal = Math.Max(maxVal, v)
                            minVal = Math.Min(minVal, v)
                        End If
                    End If
                Next

                ' [NEW] Ensure range includes Levels
                If s.Overbought.HasValue Then
                    maxVal = Math.Max(maxVal, s.Overbought.Value)
                    minVal = Math.Min(minVal, s.Overbought.Value)
                End If
                If s.Oversold.HasValue Then
                    maxVal = Math.Max(maxVal, s.Oversold.Value)
                    minVal = Math.Min(minVal, s.Oversold.Value)
                End If
                If s.BaseLine.HasValue Then
                    maxVal = Math.Max(maxVal, s.BaseLine.Value)
                    minVal = Math.Min(minVal, s.BaseLine.Value)
                End If
            Next
        End If

        ' Default range if empty or invalid
        If maxVal = Double.MinValue OrElse maxVal <= minVal Then
            maxVal = 100
            minVal = 0
        ElseIf Not fixedRange Then
            ' Add padding
            Dim rng = maxVal - minVal
            If rng = 0 Then rng = 1
            maxVal += rng * 0.1
            minVal -= rng * 0.1
        End If

        ' 3. Draw Axis
        DrawSubChartPriceAxis(canvas, subChart)

        ' Try to find first series with levels
        Dim levelSeries = seriesList.FirstOrDefault(Function(x) x.Overbought.HasValue OrElse x.Oversold.HasValue OrElse x.BaseLine.HasValue)

        ' Draw Axis Labels
        Using textPaint As New SKPaint With {.Color = CurrentTheme.Text, .TextSize = 9, .IsAntialias = True, .TextAlign = SKTextAlign.Left, .Typeface = SKTypeface.FromFamilyName("Malgun Gothic")}
            Dim labelsList As New List(Of Double) From {maxVal, minVal}
            If levelSeries IsNot Nothing Then
                If levelSeries.Overbought.HasValue Then labelsList.Add(levelSeries.Overbought.Value)
                If levelSeries.Oversold.HasValue Then labelsList.Add(levelSeries.Oversold.Value)
                If levelSeries.BaseLine.HasValue Then labelsList.Add(levelSeries.BaseLine.Value)
            End If
            
            ' Add Mid if not too many
            If labelsList.Count < 4 Then labelsList.Add((maxVal + minVal) / 2)

            For Each lblVal In labelsList.Distinct().OrderByDescending(Function(x) x)
                Dim y = GetYForSubChart(lblVal, maxVal, minVal, subChart.Rect)
                ' Constrain Y padding
                If y < subChart.Rect.Top + 8 Then y = subChart.Rect.Top + 8
                If y > subChart.Rect.Bottom - 2 Then y = subChart.Rect.Bottom - 2

                Dim labelText = If(Math.Abs(lblVal) < 10, lblVal.ToString("N2"), lblVal.ToString("N0"))

                canvas.DrawText(labelText, subChart.PriceRect.Left + 2, y + 3, textPaint)
            Next
        End Using

        ' Draw Grid / Zero line?
        Using linePaint As New SKPaint With {.Color = CurrentTheme.GridLine, .StrokeWidth = 1}
            ' If range crosses 0, draw zero line
            If minVal < 0 And maxVal > 0 Then
                Dim y0 = GetYForSubChart(0, maxVal, minVal, subChart.Rect)
                canvas.DrawLine(subChart.Rect.Left, y0, subChart.Rect.Right, y0, linePaint)
            End If
        End Using

        ' 4. Draw Series (Clipping 적용)
        canvas.Save()
        canvas.ClipRect(subChart.Rect)
        Try
            ' [NEW] Draw Levels (BaseLine, Overbought, Oversold)
            Using levelPaint As New SKPaint With {.Style = SKPaintStyle.Stroke, .StrokeWidth = 1.0F, .IsAntialias = True, .PathEffect = SKPathEffect.CreateDash({4.0F, 4.0F}, 0)}
                ' Try to find first series with levels
                Dim sLevel = seriesList.FirstOrDefault(Function(x) x.Overbought.HasValue OrElse x.Oversold.HasValue OrElse x.BaseLine.HasValue)
                If sLevel IsNot Nothing Then
                    ' Draw Overbought/Oversold Zone Fill
                    If sLevel.Overbought.HasValue AndAlso sLevel.Oversold.HasValue Then
                        Dim yOB = GetYForSubChart(sLevel.Overbought.Value, maxVal, minVal, subChart.Rect)
                        Dim yOS = GetYForSubChart(sLevel.Oversold.Value, maxVal, minVal, subChart.Rect)
                        Using fillPaint As New SKPaint With {.Style = SKPaintStyle.Fill, .Color = sLevel.Color.WithAlpha(30)}
                             canvas.DrawRect(SKRect.Create(subChart.Rect.Left, Math.Min(yOB, yOS), subChart.Rect.Width, Math.Abs(yOB - yOS)), fillPaint)
                        End Using
                    End If

                    ' Draw Overbought Line
                    If sLevel.Overbought.HasValue Then
                        Dim yOB = GetYForSubChart(sLevel.Overbought.Value, maxVal, minVal, subChart.Rect)
                        levelPaint.Color = SKColor.Parse("#f44336").WithAlpha(150) ' Reddish
                        canvas.DrawLine(subChart.Rect.Left, yOB, subChart.Rect.Right, yOB, levelPaint)
                    End If

                    ' Draw Oversold Line
                    If sLevel.Oversold.HasValue Then
                        Dim yOS = GetYForSubChart(sLevel.Oversold.Value, maxVal, minVal, subChart.Rect)
                        levelPaint.Color = SKColor.Parse("#4caf50").WithAlpha(150) ' Greenish
                        canvas.DrawLine(subChart.Rect.Left, yOS, subChart.Rect.Right, yOS, levelPaint)
                    End If

                    ' Draw BaseLine
                    If sLevel.BaseLine.HasValue Then
                         Dim yBL = GetYForSubChart(sLevel.BaseLine.Value, maxVal, minVal, subChart.Rect)
                         levelPaint.Color = SKColors.Gray.WithAlpha(150)
                         canvas.DrawLine(subChart.Rect.Left, yBL, subChart.Rect.Right, yBL, levelPaint)
                    End If
                End If
            End Using

            For Each s In seriesList
                If s.Style = PlotType.Histogram Then
                    Using paint As New SKPaint With {.Color = s.Color, .Style = SKPaintStyle.Fill, .IsAntialias = True}
                        Dim y0 = GetYForSubChart(0, maxVal, minVal, subChart.Rect)
                        For i = startIndex To endIndex
                            If i >= s.Values.Count Then Exit For
                            Dim v = s.Values(i)
                            If Double.IsNaN(v) Then Continue For

                            Dim x = GetX(i)
                            Dim y = GetYForSubChart(v, maxVal, minVal, subChart.Rect)

                            ' Draw Bar from 0 to Y
                            Dim barRect As SKRect
                            If v >= 0 Then
                                barRect = SKRect.Create(x, y, _candleWidth, Math.Abs(y0 - y))
                            Else
                                barRect = SKRect.Create(x, y0, _candleWidth, Math.Abs(y - y0))
                            End If
                            canvas.DrawRect(barRect, paint)
                        Next
                    End Using
                ElseIf s.Style = PlotType.StepLine Then
                    ' Step Line Style
                    Using path As New SKPath(),
                          paint As New SKPaint With {.Color = s.Color, .Style = SKPaintStyle.Stroke, .StrokeWidth = s.Thickness, .IsAntialias = True}

                        Dim started As Boolean = False
                        Dim prevY As Single = 0

                        For i = startIndex To endIndex
                            If i >= s.Values.Count Then Exit For
                            Dim v = s.Values(i)
                            If Double.IsNaN(v) Then
                                started = False
                                Continue For
                            End If

                            Dim x = GetX(i) + _candleWidth / 2
                            Dim y = GetYForSubChart(v, maxVal, minVal, subChart.Rect)

                            If Not started Then
                                path.MoveTo(x, y)
                                started = True
                            Else
                                path.LineTo(x, prevY)
                                path.LineTo(x, y)
                            End If
                            prevY = y
                        Next
                        canvas.DrawPath(path, paint)
                    End Using
                Else
                    ' Default: Line Style
                    Using path As New SKPath(),
                          paint As New SKPaint With {.Color = s.Color, .Style = SKPaintStyle.Stroke, .StrokeWidth = s.Thickness, .IsAntialias = True}

                        Dim started As Boolean = False
                        For i = startIndex To endIndex
                            If i >= s.Values.Count Then Exit For
                            Dim v = s.Values(i)
                            If Double.IsNaN(v) Then
                                started = False
                                Continue For
                            End If

                            Dim x = GetX(i) + _candleWidth / 2
                            Dim y = GetYForSubChart(v, maxVal, minVal, subChart.Rect)

                            If Not started Then
                                path.MoveTo(x, y)
                                started = True
                            Else
                                path.LineTo(x, y)
                            End If
                        Next
                        canvas.DrawPath(path, paint)
                    End Using
                End If
            Next
        Finally
            canvas.Restore()
        End Try
    End Sub

    Private Sub DrawCustomSeries(canvas As SKCanvas, series As CustomSeries, startIndex As Integer, endIndex As Integer, maxP As Single, minP As Single, rect As SKRect)
        ' Draw Levels (BaseLine, Overbought, Oversold)
        Using levelPaint As New SKPaint With {.Style = SKPaintStyle.Stroke, .StrokeWidth = 1.0F, .IsAntialias = True, .PathEffect = SKPathEffect.CreateDash({5, 5}, 0)}
            If series.BaseLine.HasValue Then
                levelPaint.Color = SKColors.Gray.WithAlpha(150)
                Dim y = GetY(CSng(series.BaseLine.Value), maxP, minP, rect)
                If y >= rect.Top AndAlso y <= rect.Bottom Then
                    canvas.DrawLine(rect.Left, y, rect.Right, y, levelPaint)
                End If
            End If
            If series.Overbought.HasValue Then
                levelPaint.Color = SKColors.Red.WithAlpha(150)
                Dim y = GetY(CSng(series.Overbought.Value), maxP, minP, rect)
                If y >= rect.Top AndAlso y <= rect.Bottom Then
                    canvas.DrawLine(rect.Left, y, rect.Right, y, levelPaint)
                End If
            End If
            If series.Oversold.HasValue Then
                levelPaint.Color = SKColors.Green.WithAlpha(150)
                Dim y = GetY(CSng(series.Oversold.Value), maxP, minP, rect)
                If y >= rect.Top AndAlso y <= rect.Bottom Then
                    canvas.DrawLine(rect.Left, y, rect.Right, y, levelPaint)
                End If
            End If
        End Using

        ' Use Style
        Select Case series.Style
            Case PlotType.Histogram
                Using paint As New SKPaint With {.Color = series.Color, .Style = SKPaintStyle.Fill, .IsAntialias = True}
                    For i = startIndex To endIndex
                        If i >= series.Values.Count Then Exit For
                        Dim v = series.Values(i)
                        If Double.IsNaN(v) Then Continue For

                        Dim x = GetX(i)
                        Dim yZero = GetY(0, maxP, minP, rect) ' Assuming 0 baseline for histogram overlay? Or minP?
                        ' Overlay Histogram relies on scale. Usually for Volume or specialized indicators.
                        ' If Price Overlay, 0 might be far off. 
                        ' Use yFrom and yTo.
                        ' For Price Overlay Histogram, maybe relative to something?
                        ' Defaulting to: Bar from MinP? Or Bar from 0 if visible?
                        ' Let's assume generic Histogram from Bottom (MinP) or 0 if within range.
                        Dim y = GetY(CSng(v), maxP, minP, rect)
                        Dim barHeight = Math.Abs(y - yZero) ' This logic is tricky for Price Overlay.
                        ' Fallback: Draw Circle/Dot for Point? 
                        ' Let's stick to Line if unsure, or trivial implementation.
                        Dim r = SKRect.Create(x, y, _candleWidth, 2)
                        canvas.DrawRect(r, paint) ' Just a dash for now if overlay histogram is weird
                    Next
                End Using
            Case PlotType.Scatter
                Using paint As New SKPaint With {.Color = series.Color, .Style = SKPaintStyle.Fill, .IsAntialias = True}
                    For i = startIndex To endIndex
                        If i >= series.Values.Count Then Exit For
                        Dim v = series.Values(i)
                        If Double.IsNaN(v) Then Continue For

                        Dim x = GetX(i) + _candleWidth / 2
                        Dim y = GetY(CSng(v), maxP, minP, rect)
                        Dim radius = 4.0F

                        ' Select Shape
                        Select Case series.Shape
                            Case ShapeType.Circle
                                canvas.DrawCircle(x, y, radius, paint)
                            Case ShapeType.Square
                                canvas.DrawRect(SKRect.Create(x - radius, y - radius, radius * 2, radius * 2), paint)
                            Case ShapeType.TriangleUp
                                Dim path As New SKPath()
                                path.MoveTo(x, y - radius)
                                path.LineTo(x + radius, y + radius)
                                path.LineTo(x - radius, y + radius)
                                path.Close()
                                canvas.DrawPath(path, paint)
                            Case ShapeType.TriangleDown
                                Dim path As New SKPath()
                                path.MoveTo(x, y + radius)
                                path.LineTo(x + radius, y - radius)
                                path.LineTo(x - radius, y - radius)
                                path.Close()
                                canvas.DrawPath(path, paint)
                            Case ShapeType.Diamond
                                Dim path As New SKPath()
                                path.MoveTo(x, y - radius * 1.5F)
                                path.LineTo(x + radius, y)
                                path.LineTo(x, y + radius * 1.5F)
                                path.LineTo(x - radius, y)
                                path.Close()
                                canvas.DrawPath(path, paint)
                            Case ShapeType.Cross
                                paint.Style = SKPaintStyle.Stroke
                                paint.StrokeWidth = 2
                                canvas.DrawLine(x - radius, y - radius, x + radius, y + radius, paint)
                                canvas.DrawLine(x + radius, y - radius, x - radius, y + radius, paint)
                            Case Else
                                canvas.DrawCircle(x, y, radius, paint)
                        End Select
                    Next
                End Using
            Case PlotType.StepLine
                Using path As New SKPath(),
                      paint As New SKPaint With {.Color = series.Color, .Style = SKPaintStyle.Stroke, .StrokeWidth = 1.5F, .IsAntialias = True}

                    Dim started As Boolean = False
                    Dim prevX As Single = 0
                    Dim prevY As Single = 0

                    For i = startIndex To endIndex
                        If i >= series.Values.Count Then Exit For
                        Dim v = series.Values(i)
                        If Double.IsNaN(v) Then
                            started = False
                            Continue For
                        End If

                        Dim x = GetX(i) + _candleWidth / 2
                        Dim y = GetY(CSng(v), maxP, minP, rect)

                        If Not started Then
                            path.MoveTo(x, y)
                            started = True
                        Else
                            ' Step Logic: Horizontal to new X, then Vertical to new Y
                            path.LineTo(x, prevY) ' Extend previous level to current bar
                            path.LineTo(x, y)     ' Step to new level
                        End If
                        prevX = x
                        prevY = y
                    Next
                    canvas.DrawPath(path, paint)
                End Using

            Case PlotType.Line
                If series.ColorUp.HasValue AndAlso series.ColorDown.HasValue Then
                    ' Trend Coloring Mode
                    Using paint As New SKPaint With {.Style = SKPaintStyle.Stroke, .StrokeWidth = series.Thickness, .IsAntialias = True}
                        Dim prevX As Single = -1
                        Dim prevY As Single = -1
                        Dim hasPrev As Boolean = False

                        ' We need to iterate carefully to handle NaNs and Gaps
                        For i = startIndex To endIndex
                            If i >= series.Values.Count Then Exit For
                            Dim v = series.Values(i)
                            If Double.IsNaN(v) Then
                                hasPrev = False
                                Continue For
                            End If

                            Dim x = GetX(i) + _candleWidth / 2
                            Dim y = GetY(CSng(v), maxP, minP, rect)

                             If hasPrev Then
                                 ' Determine color based on TrendValues if available, otherwise fallback to slope
                                 Dim isTrendUp As Boolean = False
                                 Dim isTrendDown As Boolean = False

                                 If series.TrendValues IsNot Nothing AndAlso i < series.TrendValues.Count Then
                                     Dim trend = series.TrendValues(i)
                                     isTrendUp = (trend > 0)
                                     isTrendDown = (trend < 0)
                                 Else
                                     ' Fallback: slope logic
                                     Dim prevV = series.Values(i - 1)
                                     isTrendUp = (v > prevV)
                                     isTrendDown = (v < prevV)
                                 End If

                                 If isTrendUp Then
                                     paint.Color = series.ColorUp.Value
                                 ElseIf isTrendDown Then
                                     paint.Color = series.ColorDown.Value
                                 Else
                                     paint.Color = series.Color
                                 End If

                                 canvas.DrawLine(prevX, prevY, x, y, paint)
                             End If

                            prevX = x
                            prevY = y
                            hasPrev = True
                        Next
                    End Using
                Else
                    ' Default Single Color Mode
                    Using path As New SKPath(),
                          paint As New SKPaint With {.Color = series.Color, .Style = SKPaintStyle.Stroke, .StrokeWidth = series.Thickness, .IsAntialias = True}

                        Dim started As Boolean = False
                        For i = startIndex To endIndex
                            If i >= series.Values.Count Then Exit For
                            Dim v = series.Values(i)
                            If Double.IsNaN(v) Then
                                started = False
                                Continue For
                            End If

                            Dim x = GetX(i) + _candleWidth / 2
                            Dim y = GetY(CSng(v), maxP, minP, rect)

                            If Not started Then
                                path.MoveTo(x, y)
                                started = True
                            Else
                                path.LineTo(x, y)
                            End If
                        Next
                        canvas.DrawPath(path, paint)
                    End Using
                End If
        End Select
    End Sub



    Private Sub DrawTimeAxis(canvas As SKCanvas, startIndex As Integer, endIndex As Integer)
        If startIndex >= Data.Count Or endIndex < 0 Then Return

        Using textPaint As New SKPaint With {.Color = CurrentTheme.Text, .TextSize = 11, .IsAntialias = True, .TextAlign = SKTextAlign.Center},
              linePaint As New SKPaint With {.Color = CurrentTheme.Border, .StrokeWidth = 1}

            canvas.DrawLine(0, TimeRect.Top, TimeRect.Right, TimeRect.Top, linePaint)

            Dim maxLabels As Integer = Math.Max(2, CInt(TimeRect.Width / 80))
            Dim totalBars = endIndex - startIndex + 1
            Dim stepBars = Math.Max(1, totalBars \ maxLabels)

            Dim lastLabelX As Single = -999

            For i As Integer = startIndex To endIndex
                If (i - startIndex) Mod stepBars <> 0 AndAlso i <> startIndex Then Continue For

                Dim x = GetX(i) + _candleWidth / 2
                If x < 0 Or x > TimeRect.Right Then Continue For

                Dim labelStr = Data(i).DateVal.ToString("MM/dd HH:mm")
                Dim textWidth = textPaint.MeasureText(labelStr)

                If x > lastLabelX + textWidth * 1.2 Then
                    canvas.DrawLine(x, TimeRect.Top, x, TimeRect.Top + 4, linePaint)
                    canvas.DrawText(labelStr, x, TimeRect.Top + 16, textPaint)
                    lastLabelX = x + textWidth / 2
                End If
            Next
        End Using
    End Sub

    Private Sub DrawCrosshair(canvas As SKCanvas, maxP As Single, minP As Single)
        ' price calculation: mouse is relative to control. y must be relative to pane.
        Dim activeSubChart As SubChartInfo = Nothing
        Dim isInSubChart As Boolean = False

        For Each s In SubCharts
            ' Check if in specific SubChart Rect (Chart Area or Axis Area)
            ' Note: Rect includes chart area. PriceRect includes Axis. 
            ' We usually draw crosshair across whole width.
            ' But standard crosshair logic usually checks vertical position.
            If _mouseY >= s.Rect.Top AndAlso _mouseY <= s.Rect.Bottom Then
                activeSubChart = s
                isInSubChart = True
                Exit For
            End If
        Next
        Dim isInMainChart = Not isInSubChart AndAlso MainRect.Contains(_mouseX, _mouseY)

        ' Allow crosshair anywhere in the vertical stack
        Dim chartBottom = If(SubCharts.Count > 0, SubCharts.Last().Rect.Bottom, MainRect.Bottom)
        If _mouseY < 10 OrElse _mouseY > chartBottom Then Return

        Using crossPen As New SKPaint With {.Color = CurrentTheme.Crosshair, .PathEffect = SKPathEffect.CreateDash({4, 4}, 0), .StrokeWidth = 1},
              defaultLabelBg As New SKPaint With {.Color = CurrentTheme.CrosshairLabelBg, .Style = SKPaintStyle.Fill},
              labelTx As New SKPaint With {.Color = CurrentTheme.CrosshairLabelText, .TextSize = 11, .IsAntialias = True}

            canvas.DrawLine(_mouseX, 0, _mouseX, chartBottom, crossPen)
            canvas.DrawLine(MainRect.Left, _mouseY, MainRect.Right, _mouseY, crossPen)

            ' Price / Indicator Label
            Dim labelText As String = ""
            Dim labelColor As SKColor = CurrentTheme.CrosshairLabelBg

            If isInMainChart Then
                Dim priceAtMouse = GetPriceFromY(_mouseY, maxP, minP, MainRect)
                labelText = FormatPrice(priceAtMouse)
                labelColor = CurrentTheme.CrosshairLabelBg

                ' Draw Label
                DrawAxisLabel(canvas, _mouseY, labelText, labelColor, labelTx, PriceRect.Left)

            ElseIf isInSubChart Then
                ' Calculate Value from Y
                Dim s = activeSubChart
                Dim minVal As Double = 0
                Dim maxVal As Double = 0

                Dim range = GetVisibleMinMax(s.PanelName)
                maxVal = range.Item1
                minVal = range.Item2

                If maxVal <> minVal Then
                    Dim val = GetPriceFromY(_mouseY, CSng(maxVal), CSng(minVal), s.Rect)
                    labelText = val.ToString("F2")

                    ' Attempt to get color of first series in this panel
                    Dim series = _customSeriesList.FirstOrDefault(Function(cs) cs.PanelName = s.PanelName)
                    If series IsNot Nothing Then
                        labelColor = series.Color
                    Else
                        labelColor = CurrentTheme.CrosshairLabelBg
                    End If

                    DrawAxisLabel(canvas, _mouseY, labelText, labelColor, labelTx, s.PriceRect.Left)
                End If
            End If

            ' Time Label
            Dim indexAtMouse = GetIndexFromX(_mouseX)
            If indexAtMouse >= 0 AndAlso indexAtMouse < Data.Count Then
                Dim timeData = Data(indexAtMouse)
                Dim timeStr = timeData.DateVal.ToString("MM/dd HH:mm")
                
                labelTx.TextAlign = SKTextAlign.Center
                labelTx.Typeface = SKTypeface.FromFamilyName("Malgun Gothic")
                
                Dim tTextW = labelTx.MeasureText(timeStr)
                Dim tLabelRect = SKRect.Create(_mouseX - tTextW / 2 - 5, TimeRect.Top, tTextW + 10, AxisHeight - 2)

                Using timeBg As New SKPaint With {.Color = CurrentTheme.CrosshairLabelBg, .Style = SKPaintStyle.Fill}
                    canvas.DrawRect(tLabelRect, timeBg)
                End Using
                
                canvas.DrawText(timeStr, _mouseX, TimeRect.Top + 16, labelTx)

                RaiseEvent CrosshairMoved(Me, New CrosshairEventArgs With {
                    .Price = If(isInMainChart, GetPriceFromY(_mouseY, maxP, minP, MainRect), 0),
                    .Time = timeData.DateVal,
                    .Index = indexAtMouse
                })
            End If
        End Using
    End Sub

    Private Sub DrawAxisLabel(canvas As SKCanvas, y As Single, text As String, bgColor As SKColor, textPaint As SKPaint, xPos As Single)
        Dim textW = textPaint.MeasureText(text)
        Dim labelRect = SKRect.Create(xPos, y - 10, textW + 10, 20)

        Using bgPaint As New SKPaint With {.Color = bgColor, .Style = SKPaintStyle.Fill}
            canvas.DrawRect(labelRect, bgPaint)
        End Using

        ' Auto-Contrast Text Color (Luminance check)
        Dim brightness = (0.299 * bgColor.Red + 0.587 * bgColor.Green + 0.114 * bgColor.Blue)
        Dim contrastColor = If(brightness > 128, SKColors.Black, SKColors.White)

        Dim originalColor = textPaint.Color
        textPaint.Color = contrastColor
        canvas.DrawText(text, xPos + 5, y + 4, textPaint)
        textPaint.Color = originalColor
    End Sub

    ' Helper to get Min/Max for visible range (re-calculation needed for Crosshair accuracy)
    ' This relies on _startIndex and _endIndex which must be stored during Draw
    Private _cachedStartIndex As Integer
    Private _cachedEndIndex As Integer

    Private Function GetVisibleMinMax(panelName As String) As Tuple(Of Double, Double)
        Dim minVal As Double = Double.MaxValue
        Dim maxVal As Double = Double.MinValue

        Dim seriesList = _customSeriesList.Where(Function(s) s.PanelName = panelName).ToList()
        If seriesList.Count = 0 Then Return Tuple.Create(100.0, 0.0)

        ' Priority: Fixed Range
        If seriesList(0).MinValue.HasValue AndAlso seriesList(0).MaxValue.HasValue Then
            Return Tuple.Create(seriesList(0).MaxValue.Value, seriesList(0).MinValue.Value)
        End If

        ' Scan Visible
        Dim found As Boolean = False
        For Each s In seriesList
            ' Use cached range indices from last draw if available, or full
            Dim startI = If(_cachedStartIndex > 0, _cachedStartIndex, 0)
            Dim endI = If(_cachedEndIndex > 0, _cachedEndIndex, s.Values.Count - 1)

            For i = startI To endI
                If i < s.Values.Count Then
                    Dim v = s.Values(i)
                    If Not Double.IsNaN(v) Then
                        maxVal = Math.Max(maxVal, v)
                        minVal = Math.Min(minVal, v)
                        found = True
                    End If
                End If
            Next
        Next

        If Not found Then Return Tuple.Create(100.0, 0.0)

        ' Add padding
        Dim rng = maxVal - minVal
        If rng = 0 Then rng = 1
        Return Tuple.Create(maxVal + rng * 0.05, minVal - rng * 0.05)
    End Function


    Private Function DrawStockInfo(canvas As SKCanvas) As Single
        ' [Fix] Use Malgun Gothic for Hangul support
        Dim currentY As Single = 20
        Dim startX As Single = 10
        If _leftAxisMode <> LeftAxisModeType.None Then
            startX = _leftAxisRect.Right + 10
        End If

        Using typeface As SKTypeface = SKTypeface.FromFamilyName("Malgun Gothic")
            Using textPaint As New SKPaint With {.Color = CurrentTheme.Text, .TextSize = 12, .IsAntialias = True, .Typeface = typeface}

                ' 1. Stock Name Code
                Dim info = $"{_stockCode} {_stockName}"
                If Not String.IsNullOrEmpty(info.Trim()) Then
                    canvas.DrawText(info, startX, currentY, textPaint)
                    currentY += 18 ' Next Line
                End If

                ' 2. OHLCV
                Dim idx = GetIndexFromX(_mouseX)
                If idx >= 0 AndAlso idx < Data.Count Then
                    Dim d = Data(idx)
                    Dim prevIdx = idx - 1
                    Dim prevD As OHLCV = Nothing
                    If prevIdx >= 0 Then prevD = Data(prevIdx)

                    Dim cx As Single = startX
                    Dim cy As Single = currentY

                    ' Use DrawValueWithChange for each
                    cx = DrawValueWithChange(canvas, cx, cy, textPaint, "O", d.Open, If(prevIdx >= 0, prevD.Open, Double.NaN), FormatPrice(d.Open))
                    cx = DrawValueWithChange(canvas, cx, cy, textPaint, "H", d.High, If(prevIdx >= 0, prevD.High, Double.NaN), FormatPrice(d.High))
                    cx = DrawValueWithChange(canvas, cx, cy, textPaint, "L", d.Low, If(prevIdx >= 0, prevD.Low, Double.NaN), FormatPrice(d.Low))
                    cx = DrawValueWithChange(canvas, cx, cy, textPaint, "C", d.Close, If(prevIdx >= 0, prevD.Close, Double.NaN), FormatPrice(d.Close))

                    canvas.DrawText($"V: {d.Volume:N0}", cx, cy, textPaint)
                    currentY += 18 ' Next Line
                End If

                ' 3. Strategy Info
                If Not String.IsNullOrEmpty(StrategyName) Then
                    textPaint.Color = SKColors.Orange
                    canvas.DrawText($"[{StrategyMode}] {StrategyName}", startX, currentY, textPaint)
                    textPaint.Color = CurrentTheme.Text ' Restore
                    currentY += 18
                End If
            End Using
        End Using

        Return currentY
    End Function


#End Region

#Region "헬퍼 함수"
    Private Function DrawValueWithChange(canvas As SKCanvas, x As Single, y As Single, paint As SKPaint,
                                         label As String, val As Double, prevVal As Double, valStr As String) As Single

        Dim mainText = $"{label}: {valStr}"
        canvas.DrawText(mainText, x, y, paint)
        Dim w = paint.MeasureText(mainText)
        x += w + 5

        If Not Double.IsNaN(prevVal) Then
            Dim pct = 0.0
            If prevVal <> 0 Then pct = (val - prevVal) / prevVal * 100.0

            Dim changeStr = $"({pct:F2}%)"
            If pct > 0 Then changeStr = $"(+{pct:F2}%)"

            Dim oldColor = paint.Color
            If pct > 0 Then
                paint.Color = SKColors.Red
            ElseIf pct < 0 Then
                paint.Color = SKColor.Parse("#00FF00") ' Lime
            Else
                paint.Color = SKColors.Gray
            End If

            canvas.DrawText(changeStr, x, y, paint)
            w = paint.MeasureText(changeStr)
            x += w + 10

            paint.Color = oldColor ' Restore
        End If

        Return x
    End Function

    Private Function GetNiceInterval(range As Single, targetTickCount As Integer) As Single
        If targetTickCount <= 0 Then targetTickCount = 1
        Dim roughStep As Single = range / targetTickCount
        If roughStep <= 0 Then roughStep = 1.0F

        Dim power As Single = CSng(Math.Pow(10, Math.Floor(Math.Log10(roughStep))))
        Dim normalizedStep As Single = roughStep / power
        Dim niceStep As Single

        If normalizedStep < 1.5 Then
            niceStep = 1
        ElseIf normalizedStep < 3.5 Then
            niceStep = 2
        ElseIf normalizedStep < 7.5 Then
            niceStep = 5
        Else
            niceStep = 10
        End If

        Return niceStep * power
    End Function

    Private Function GetY(price As Single, maxP As Single, minP As Single, rect As SKRect) As Single
        If maxP = minP Then Return rect.Top + rect.Height / 2
        Return rect.Top + rect.Height * (1.0F - (price - minP) / (maxP - minP))
    End Function

    Private Function GetX(index As Integer) As Single
        Return MainRect.Left + CSng((index - _scrollOffset) * (_candleWidth + _gap))
    End Function

    Private Function GetIndexFromX(x As Single) As Integer
        If _candleWidth + _gap <= 0 Then Return -1
        Return CInt(Math.Round(_scrollOffset + (x - MainRect.Left) / (_candleWidth + _gap)))
    End Function

    Private Function GetPriceFromY(y As Single, maxP As Single, minP As Single, rect As SKRect) As Single
        If rect.Height <= 0 Then Return 0
        Dim yRel = y - rect.Top
        Return maxP - (yRel / rect.Height) * (maxP - minP)
    End Function

    Private Function FormatPrice(price As Single) As String
        If price >= 100000 Then
            Return price.ToString("N0")
        ElseIf price >= 1000 Then
            Return price.ToString("N0")
        ElseIf price >= 100 Then
            Return price.ToString("N1")
        Else
            Return price.ToString("N2")
        End If
    End Function
#End Region

#Region "이벤트 핸들러"
    Private Sub SKControl_Resize(sender As Object, e As EventArgs)
        UpdateLayout()
        RefreshChart()
    End Sub

    Private Sub SKControl_MouseDown(sender As Object, e As MouseEventArgs)
        ' Check Legend Hit FIRST
        Dim hitLegend = _legendItems.FirstOrDefault(Function(li) li.HitRect.Contains(e.X, e.Y))
        If hitLegend IsNot Nothing Then
            _selectedSeriesName = hitLegend.PanelName
            RefreshChart()
            Return ' Don't start dragging if clicking legend
        Else
            _selectedSeriesName = ""
            RefreshChart()
        End If

        _lastMouseX = e.X
        _lastMouseY = e.Y

        ' Check Splitter Hit
        Dim hitSplitter = _splitterRects.Cast(Of SplitterInfo?).FirstOrDefault(Function(si) si.Value.Rect.Contains(e.X, e.Y))
        If hitSplitter.HasValue Then
            _isDraggingSplitter = True
            _activeSplitter = hitSplitter.Value
            Return
        End If

        ' Propagate to Parent (UserControl) so ContextMenu works
        MyBase.OnMouseDown(e)

        If e.Button = MouseButtons.Left Then
            If PriceRect.Contains(e.X, e.Y) Then
                _isDraggingPrice = True
                _isAutoScaleY = False
            ElseIf MainRect.Contains(e.X, e.Y) OrElse SubCharts.Any(Function(s) s.Rect.Contains(e.X, e.Y)) Then
                _isDraggingChart = True
            End If
        ElseIf e.Button = MouseButtons.Right Then
            Dim idx = GetIndexFromX(e.X)
            If idx >= 0 AndAlso idx < Data.Count Then
                RaiseEvent CandleClicked(Me, New CandleClickEventArgs With {
                    .Index = idx,
                    .Data = Data(idx),
                    .MouseButton = e.Button
                })
            End If
        End If
    End Sub

    Private Sub SKControl_MouseMove(sender As Object, e As MouseEventArgs)
        _mouseX = e.X
        _mouseY = e.Y

        If _isDraggingPrice Then
            Dim dy = e.Y - _lastMouseY
            Dim sensitivity As Single = 0.008F

            Dim range = _manualMaxP - _manualMinP
            Dim center = (_manualMaxP + _manualMinP) / 2
            Dim scaleFactor As Single = 1.0F + (dy * sensitivity)

            Dim newHalfRange = (range * scaleFactor) / 2
            _manualMaxP = center + newHalfRange
            _manualMinP = center - newHalfRange

            _lastMouseY = e.Y
            RaiseEvent ScaleChanged(Me, EventArgs.Empty)
            RefreshChart()

        ElseIf _isDraggingChart Then
            ' Horizontal Scroll
            Dim dx = e.X - _lastMouseX
            _scrollOffset -= dx / (_candleWidth + _gap)
            _scrollOffset = Math.Max(0, Math.Min(Math.Max(0, Data.Count - 10), _scrollOffset))
            _lastMouseX = e.X

            ' Vertical Scroll
            Dim dy = e.Y - _lastMouseY
            If Math.Abs(dy) > 1 Then
                If _isAutoScaleY Then _isAutoScaleY = False
                Dim range = _manualMaxP - _manualMinP
                If range > 0 Then
                    Dim deltaP = (dy / MainRect.Height) * range
                    _manualMaxP += deltaP
                    _manualMinP += deltaP
                End If
                _lastMouseY = e.Y
            End If
            RefreshChart()

        ElseIf _isDraggingSplitter AndAlso _activeSplitter.HasValue Then
            Dim dy = e.Y - _lastMouseY
            If dy <> 0 Then
                Dim h = skControl.Height - AxisHeight
                If h > 0 Then
                    ' The sum of weights corresponds to h. 
                    ' weightDelta = (dy / h) * totalWeight
                    Dim totalW As Single = 0
                    For Each wVal In _panelWeights.Values : totalW += wVal : Next
                    
                    Dim weightDelta = (dy / h) * totalW
                    Dim pAbove = _activeSplitter.Value.PanelAbove
                    Dim pBelow = _activeSplitter.Value.PanelBelow
                    
                    Dim newWAbove = _panelWeights(pAbove) + weightDelta
                    Dim newWBelow = _panelWeights(pBelow) - weightDelta
                    
                    If newWAbove > 0.3F AndAlso newWBelow > 0.3F Then
                        _panelWeights(pAbove) = newWAbove
                        _panelWeights(pBelow) = newWBelow
                        UpdateLayout()
                    End If
                End If
                _lastMouseY = e.Y
                RefreshChart()
            End If
        Else
            ' Cursor logic
            Dim isOverSplitter = _splitterRects.Any(Function(si) si.Rect.Contains(e.X, e.Y))
            If isOverSplitter Then
                skControl.Cursor = Cursors.HSplit
            ElseIf PriceRect.Contains(e.X, e.Y) Or _isDraggingPrice Then
                skControl.Cursor = Cursors.SizeNS
            Else
                skControl.Cursor = Cursors.Cross
            End If
            RefreshChart()
        End If
    End Sub

    Private Sub SKControl_MouseUp(sender As Object, e As MouseEventArgs)
        _isDraggingChart = False
        _isDraggingPrice = False
        _isDraggingSplitter = False
        _activeSplitter = Nothing
    End Sub

    Private Sub SKControl_MouseLeave(sender As Object, e As EventArgs)
        _mouseX = -1
        _mouseY = -1
        RefreshChart()
    End Sub


    Private Function GetIndexAtX(x As Single) As Integer
        If _candleWidth + _gap <= 0 Then Return -1
        Return CInt(Math.Floor(_scrollOffset + (x - MainRect.Left) / (_candleWidth + _gap)))
    End Function

    Private Sub OnWheel(sender As Object, e As MouseEventArgs)
        Dim zoomFactor As Single = If(e.Delta > 0, 1.15F, 0.87F)
        Dim newWidth = Math.Max(2.0F, Math.Min(50.0F, _candleWidth * zoomFactor))
        Dim mouseIndex = _scrollOffset + (e.X - MainRect.Left) / (_candleWidth + _gap)
        _candleWidth = newWidth
        _scrollOffset = CSng(mouseIndex - (e.X - MainRect.Left) / (_candleWidth + _gap))
        _scrollOffset = Math.Max(0, Math.Min(Math.Max(0, Data.Count - 5), _scrollOffset))
        RefreshChart()
    End Sub
#End Region

    Private Sub DrawSubChart(canvas As SKCanvas, startIndex As Integer, endIndex As Integer, subInfo As SubChartInfo)
        DrawCustomSubChart(canvas, startIndex, endIndex, subInfo)
    End Sub


    Private Sub DrawLineIndicatorSimple(canvas As SKCanvas, values() As Double, startIndex As Integer, endIndex As Integer, rect As SKRect, color As SKColor, title As String)
        If values Is Nothing Then Return

        ' min/max 계산
        Dim maxVal As Double = Double.MinValue
        Dim minVal As Double = Double.MaxValue
        For i = startIndex To endIndex
            If i < values.Length AndAlso Not Double.IsNaN(values(i)) Then
                maxVal = Math.Max(maxVal, values(i))
                minVal = Math.Min(minVal, values(i))
            End If
        Next

        If maxVal = Double.MinValue Then Return ' 데이터 없음

        Dim padding = (maxVal - minVal) * 0.1
        maxVal += padding
        minVal -= padding
        If maxVal = minVal Then maxVal += 1

        ' 라인 그리기
        Using path As New SKPath(),
              paint As New SKPaint With {.Color = color, .Style = SKPaintStyle.Stroke, .StrokeWidth = 1.5F, .IsAntialias = True},
              textPaint As New SKPaint With {.Color = CurrentTheme.Text, .TextSize = 10, .IsAntialias = True}

            Dim started As Boolean = False
            For i = startIndex To endIndex
                If i >= values.Length Then Exit For
                Dim val = values(i)
                If Double.IsNaN(val) Then
                    started = False
                    Continue For
                End If

                Dim x = GetX(i) + _candleWidth / 2
                Dim y = GetY(CSng(val), CSng(maxVal), CSng(minVal), rect)

                If Not started Then
                    path.MoveTo(x, y)
                    started = True
                Else
                    path.LineTo(x, y)
                End If
            Next
            canvas.DrawPath(path, paint)

            ' 제목 표시
            canvas.DrawText(title, rect.Left + 5, rect.Top + 15, textPaint)
        End Using
    End Sub



    ''' <summary>
    ''' Strategy-driven Overlay Drawing.
    ''' Clears existing custom series and rebuilds them from the provided Configs and Data.
    ''' </summary>
    Public Sub DrawOverlays(configs As Dictionary(Of String, PlotConfig), dataMap As Dictionary(Of String, List(Of Double)), Optional trendMap As Dictionary(Of String, List(Of Double)) = Nothing)
        ' 1. Clear existing Custom Series lists
        _customSeriesList.Clear()

        ' 2. Rebuild Series from Configs
        For Each kvp In configs
            Dim key = kvp.Key
            Dim config = kvp.Value

            ' Check if we have data for this config
            If Not dataMap.ContainsKey(key) Then Continue For

            Dim seriesData = dataMap(key)

            ' Create CustomSeries
            Dim newSeries As New CustomSeries With {
                .SeriesName = key,
                .Title = If(Not String.IsNullOrEmpty(config.Title), config.Title, If(Not String.IsNullOrEmpty(config.Name), config.Name, key)),
                .PanelName = If(String.IsNullOrEmpty(config.PanelName), "Main", config.PanelName),
                .PanelType = If(String.Equals(config.PanelName, "Main", StringComparison.OrdinalIgnoreCase) OrElse String.IsNullOrEmpty(config.PanelName), PanelType.Overlay, PanelType.Bottom),
                .Color = If(config.Color.IsEmpty OrElse config.Color.A = 0, SKColors.White, New SKColor(config.Color.R, config.Color.G, config.Color.B, config.Color.A)),
                .Values = seriesData,
                .Style = config.Type,
                .IsCustom = True,
                .Thickness = If(config.Thickness <= 0, 1.5F, config.Thickness),
                .BaseLine = config.BaseLine,
                .Overbought = config.Overbought,
                .Oversold = config.Oversold
            }

            If config.ColorUp.HasValue Then
                newSeries.ColorUp = New SKColor(config.ColorUp.Value.R, config.ColorUp.Value.G, config.ColorUp.Value.B, config.ColorUp.Value.A)
            End If
            If config.ColorDown.HasValue Then
                newSeries.ColorDown = New SKColor(config.ColorDown.Value.R, config.ColorDown.Value.G, config.ColorDown.Value.B, config.ColorDown.Value.A)
            End If

            ' Apply Trend values if available
            If trendMap IsNot Nothing AndAlso trendMap.ContainsKey(key) Then
                newSeries.TrendValues = trendMap(key)
            End If

            _customSeriesList.Add(newSeries)
        Next

        ' 3. Calculate Layouts (Panels)
        UpdateLayout() ' Was RecalculateLayout()
        RefreshChart()
    End Sub

End Class

Public Enum LeftAxisModeType
    None
    VsPrevClose ' 전일 종가 대비
    VsTodayOpen ' 당일 시가 대비
End Enum
