using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common.Enums;
using App64.Forms;
using App64.Services;
using Bridge;
using Common.Models;
using WeifenLuo.WinFormsUI.Docking;

namespace App64
{
    public partial class MainForm : Form
    {
        // ── 서비스 (기존 유지) ──────────────────────
        private ConnectionService _conn;
        private MarketDataService _market;
        private OrderService _order;
        private CandleService _candle;

        // ── DockPanel ───────────────────────────────
        private DockPanel _dockPanel;

        // ── 싱글톤 자식 폼 ──────────────────────────
        private LogForm _logForm;
        private SettingsForm _settingsForm;
        private WatchForm _watchForm;
        private PortfolioForm _portfolioForm;

        // ── 툴바/상태바 컨트롤 ──────────────────────
        private MenuStrip _menuStrip;
        private ToolStrip _toolStrip;
        private ToolStripButton _btnConnect;
        private ToolStripButton _btnOrderTest;
        private ToolStripLabel _lblStatus;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblConnState;
        private ToolStripStatusLabel _lblKiwoomState;
        private ToolStripStatusLabel _lblCybosState;
        private ToolStripStatusLabel _lblMsgCount;
        private Timer _clockTimer;

        // ── 통계 ────────────────────────────────────
        private int _msgCount;

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
        }

        // ═══════════════════════════════════════════
        //  UI 구성 (코드 기반 – Designer 의존 최소화)
        // ═══════════════════════════════════════════
        private void SetupUI()
        {
            this.Text = "TradeTemplate — App64";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.IsMdiContainer = true;
            this.BackColor = Color.FromArgb(20, 20, 30);

            // ── MenuStrip ──
            _menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.FromArgb(200, 200, 220)
            };

            var mnuFile = new ToolStripMenuItem("파일(&F)");
            mnuFile.DropDownItems.Add("종료(&X)", null, (s, e) => this.Close());

            var mnuTrade = new ToolStripMenuItem("매매(&T)");
            mnuTrade.DropDownItems.Add("조건검색 실행(&Q)", null, (s, e) => ShowConditionSearch());
            mnuTrade.DropDownItems.Add("잔고 조회(&B)", null, async (s, e) => await RequestBalanceAsync());
            mnuTrade.DropDownItems.Add(new ToolStripSeparator());
            mnuTrade.DropDownItems.Add("주문 극한테스트(&T)", null, BtnOrderTest_Click);

            var mnuView = new ToolStripMenuItem("보기(&V)");
            mnuView.DropDownItems.Add("종목감시(&W)", null, (s, e) => ShowOrActivate(ref _watchForm));
            mnuView.DropDownItems.Add("포트폴리오(&P)", null, (s, e) => ShowOrActivate(ref _portfolioForm));
            mnuView.DropDownItems.Add("로그(&L)", null, (s, e) => ShowOrActivate(ref _logForm));
            mnuView.DropDownItems.Add("설정(&S)", null, (s, e) => ShowOrActivate(ref _settingsForm));
            mnuView.DropDownItems.Add(new ToolStripSeparator());
            mnuView.DropDownItems.Add("멀티차트(&M)", null, (s, e) => ShowMultiChart());
            mnuView.DropDownItems.Add(new ToolStripSeparator());
            mnuView.DropDownItems.Add("레이아웃 초기화(&R)", null, (s, e) => ResetLayout());

            var mnuServer = new ToolStripMenuItem("서버(&S)");
            mnuServer.DropDownItems.Add("연결(&C)", null, async (s, e) => await ConnectAsync());
            mnuServer.DropDownItems.Add("연결해제(&D)", null, (s, e) => Disconnect());

            _menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuTrade, mnuView, mnuServer });
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);

            // ── ToolStrip (시스템 시작/중지 제거, 업무 버튼만) ──
            _toolStrip = new ToolStrip
            {
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.White,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = new DarkToolStripRenderer()
            };

            _btnConnect = new ToolStripButton("연결")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.LimeGreen
            };
            _btnConnect.Click += async (s, e) => await ConnectAsync();

            var btnCondition = new ToolStripButton("조건검색")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.Cyan
            };
            btnCondition.Click += (s, e) => ShowConditionSearch();

            var btnBalance = new ToolStripButton("잔고조회")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.FromArgb(255, 180, 100)
            };
            btnBalance.Click += async (s, e) => await RequestBalanceAsync();

            _btnOrderTest = new ToolStripButton("주문테스트")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.FromArgb(80, 180, 255),
                Enabled = false
            };
            _btnOrderTest.Click += BtnOrderTest_Click;

            _lblStatus = new ToolStripLabel("대기중")
            {
                ForeColor = Color.Gray,
                Alignment = ToolStripItemAlignment.Right
            };

            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _btnConnect,
                new ToolStripSeparator(),
                btnCondition,
                btnBalance,
                new ToolStripSeparator(),
                _btnOrderTest,
                new ToolStripSeparator(),
                _lblStatus
            });
            this.Controls.Add(_toolStrip);

            // ── StatusStrip (동일) ──
            _statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(25, 25, 35),
                SizingGrip = false
            };
            _lblConnState = new ToolStripStatusLabel("● 끊김") { ForeColor = Color.Red };
            _lblKiwoomState = new ToolStripStatusLabel("K:--") { ForeColor = Color.Gray };
            _lblCybosState = new ToolStripStatusLabel("C:--") { ForeColor = Color.Gray };
            _lblMsgCount = new ToolStripStatusLabel("Msg: 0")
            {
                ForeColor = Color.FromArgb(150, 150, 170),
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            _statusStrip.Items.AddRange(new ToolStripItem[]
            {
                _lblConnState, _lblKiwoomState, _lblCybosState, _lblMsgCount
            });
            this.Controls.Add(_statusStrip);

            // ── DockPanel (동일) ──
            _dockPanel = new DockPanel
            {
                Dock = DockStyle.Fill,
                Theme = new VS2015DarkTheme(),
                DocumentStyle = DocumentStyle.DockingMdi,
                AllowEndUserDocking = true,
                AllowEndUserNestedDocking = true
            };
            this.Controls.Add(_dockPanel);
            _dockPanel.BringToFront();

            // ── Clock Timer ──
            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                _lblMsgCount.Text = $"Msg: {_msgCount} | {DateTime.Now:HH:mm:ss}";
            };
            _clockTimer.Start();
        }

        // ═══════════════════════════════════════════
        //  Form Load – 레이아웃 초기화 + 자동 연결
        // ═══════════════════════════════════════════
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ResetLayout();
            AppendLog("TradeTemplate App64 시작됨");
            await ConnectAsync();
        }

        // ═══════════════════════════════════════════
        //  레이아웃
        // ═══════════════════════════════════════════
        private void ResetLayout()
        {
            CloseAllForms();

            _logForm = new LogForm();
            _logForm.Show(_dockPanel, DockState.DockBottom);

            _watchForm = new WatchForm(this);
            _watchForm.Show(_dockPanel, DockState.DockLeft);

            _portfolioForm = new PortfolioForm();
            _portfolioForm.Show(_logForm.Pane, DockAlignment.Right, 0.5);

            _settingsForm = new SettingsForm();
            _settingsForm.Show(_dockPanel, DockState.DockRight);
        }

        private void CloseAllForms()
        {
            for (int i = _dockPanel.Contents.Count - 1; i >= 0; i--)
            {
                if (_dockPanel.Contents[i] is IDockContent content)
                    content.DockHandler.Close();
            }
            _logForm = null;
            _settingsForm = null;
            _watchForm = null;
            _portfolioForm = null;
        }

        // ═══════════════════════════════════════════
        //  연결
        // ═══════════════════════════════════════════
        private async Task ConnectAsync()
        {
            if (_conn != null && _conn.IsConnected)
            {
                AppendLog("이미 연결되어 있습니다");
                return;
            }

            _btnConnect.Enabled = false;
            _lblStatus.Text = "연결 중...";

            try
            {
                // 서비스 초기화
                _conn = new ConnectionService();
                _conn.OnLog += msg => AppendLog(msg);
                _conn.OnConnectionChanged += OnConnectionChanged;
                _conn.OnPushReceived += OnPushReceived;

                // MarketDataService – 실시간 시세 처리
                _market = new MarketDataService(_conn);
                _market.OnLog += msg => BeginInvoke((Action)(() => AppendLog(msg)));
                _market.OnMarketDataUpdated += md =>
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        _watchForm?.UpdateMarketData(md);

                        // 열려 있는 차트 폼에도 실시간 데이터 전달
                        foreach (var content in _dockPanel.Documents)
                        {
                            if (content is ChartForm cf && !cf.IsDisposed)
                                cf.OnMarketDataUpdated(md);
                            else if (content is MultiChartForm mcf && !mcf.IsDisposed)
                                mcf.OnMarketDataUpdated(md);
                        }
                    }));
                };

                _order = new OrderService(_conn);
                _candle = new CandleService(_conn);

                // 서버 프로세스 실행 확인
                EnsureServerRunning();

                AppendLog("서버 연결 시도...");
                await _conn.ConnectAsync();

                // 로그인 상태 확인
                var loginStatus = await _conn.CheckLoginAsync();
                bool kiwoomOk = loginStatus.Item1;
                bool cybosOk = loginStatus.Item2;

                UpdateKiwoomIndicator(kiwoomOk);
                UpdateCybosIndicator(cybosOk);

                _btnOrderTest.Enabled = kiwoomOk;
                _lblStatus.Text = "연결 완료";
                _btnConnect.Text = "연결됨";

                AppendLog($"로그인 상태 — 키움:{(kiwoomOk ? "OK" : "X")} Cybos:{(cybosOk ? "OK" : "X")}");
            }
            catch (Exception ex)
            {
                AppendLog($"연결 실패: {ex.Message}");
                _lblStatus.Text = "연결 실패";
                _btnConnect.Enabled = true;
                _btnConnect.Text = "연결";
            }
        }

        private void EnsureServerRunning()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("Server32");
                if (processes.Length == 0)
                {
                    AppendLog("Server32 프로세스가 감지되지 않았습니다. 실행을 시도합니다...");
                    
                    // 실행 경로 유추 (작업 디렉토리 기준)
                    string serverPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Server32.exe");
                    if (!System.IO.File.Exists(serverPath))
                    {
                        // 개발 환경용 대체 경로 (../Server32/bin/Debug/Server32.exe)
                        serverPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Server32", "bin", "x86", "Debug", "Server32.exe");
                    }

                    if (System.IO.File.Exists(serverPath))
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo(serverPath)
                        {
                            UseShellExecute = true,
                            Verb = "runas" // 관리자 권한 필요 (키움/Cybos)
                        };
                        System.Diagnostics.Process.Start(startInfo);
                        AppendLog("Server32 실행 명령 전송 완료 (관리자 권한 요청됨)");
                    }
                    else
                    {
                        AppendLog("[오류] Server32 실행 파일을 찾을 수 없습니다. 경로를 확인하세요.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[오류] 서버 자동 실행 실패: {ex.Message}");
            }
        }

        private void Disconnect()
        {
            _conn?.Disconnect();
            AppendLog("연결 해제됨");
        }

        // ═══════════════════════════════════════════
        //  파이프 Push 메시지 라우팅
        // ═══════════════════════════════════════════
        private void OnPushReceived(ushort msgType, uint seqNo, byte[] body)
        {
            _msgCount++;

            // MarketDataService가 RealtimePush(0x0022)를 자체 처리하므로
            // 여기서는 그 외 Push만 처리

            switch (msgType)
            {
                case MessageTypes.TradePush:
                    HandleTradePush(body);
                    break;

                case MessageTypes.BalancePush:
                    HandleBalancePush(body);
                    break;
                case MessageTypes.ConditionResult:
                    HandleConditionResult(body);
                    break;

                case MessageTypes.ConditionRealtime:
                    HandleConditionRealtime(body);
                    break;

                case MessageTypes.OrderTestResponse:
                    string orderLog = Encoding.UTF8.GetString(body);
                    AppendLog(orderLog);
                    break;

                case MessageTypes.SystemLogPush:
                    string remoteLog = Encoding.UTF8.GetString(body);
                    AppendLog("[Server] " + remoteLog);
                    break;
            }
        }

        private void HandleTradePush(byte[] body)
        {
            try
            {
                var trade = BinarySerializer.DeserializeTrade(body);
                AppendLog($"[체결] {trade}");
                // Phase 4: OrderManager.ProcessFill(trade) 호출 예정
            }
            catch (Exception ex)
            {
                AppendLog($"[체결 파싱 오류] {ex.Message}");
            }
        }

        private void HandleBalancePush(byte[] body)
        {
            try
            {
                var balance = BinarySerializer.DeserializeBalance(body);
                AppendLog($"[잔고] {balance}");
                // Phase 4: BalanceManager.Update(balance) 호출 예정
                this.BeginInvoke((Action)(() =>
                {
                    _portfolioForm?.UpdatePosition(balance);
                }));
            }
            catch (Exception ex)
            {
                AppendLog($"[잔고 파싱 오류] {ex.Message}");
            }
        }

        private void HandleConditionResult(byte[] body)
        {
            try
            {
                string payload = Encoding.UTF8.GetString(body);
                AppendLog($"[조건검색 결과] 수신 {payload.Length}자");

                // 키움 조건식은 포착 종목을 자동으로 실시간 등록하므로
                // 수동 구독 전에 미리 등록하여 중복 방지
                if (_market != null)
                {
                    var lines = payload.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var code = line.Split('|')[0].Trim();
                        if (!string.IsNullOrEmpty(code) && code.Length == 6)
                            _market.MarkAsConditionAutoSubscribed(code);
                    }
                }

                this.BeginInvoke((Action)(() =>
                {
                    _watchForm?.OnConditionResult(payload);
                }));
            }
            catch (Exception ex)
            {
                AppendLog($"[조건검색 오류] {ex.Message}");
            }
        }

        private void HandleConditionRealtime(byte[] body)
        {
            try
            {
                string payload = Encoding.UTF8.GetString(body);
                // "CODE|TYPE|NAME|IDX"
                AppendLog($"[실시간 조건] {payload}");

                // 편입(I) 종목도 키움 자동 실시간 등록
                var fields = payload.Split('|');
                if (fields.Length >= 2 && fields[1] == "I" && _market != null)
                {
                    _market.MarkAsConditionAutoSubscribed(fields[0].Trim());
                }

                this.BeginInvoke((Action)(() =>
                {
                    _watchForm?.OnConditionRealtime(payload);
                }));
            }
            catch (Exception ex)
            {
                AppendLog($"[실시간 조건 오류] {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════
        //  주문 극한테스트 (기존 기능 유지)
        // ═══════════════════════════════════════════
        private async void BtnOrderTest_Click(object sender, EventArgs e)
        {
            _btnOrderTest.Enabled = false;
            _btnOrderTest.Text = "테스트 실행중...";
            AppendLog("═══════ 주문 극한테스트 요청 → Server32 ═══════");

            try
            {
                var resp = await _conn.RequestAsync(
                    MessageTypes.OrderTestRequest, null, 120000);

                if (resp.respType == MessageTypes.OrderTestResponse)
                {
                    string result = Encoding.UTF8.GetString(resp.respBody);
                    AppendLog(result);
                    AppendLog("═══════ 주문 극한테스트 완료 ═══════");
                }
                else if (resp.respType == MessageTypes.ErrorResponse)
                {
                    string err = Encoding.UTF8.GetString(resp.respBody);
                    AppendLog("[ERR] " + err);
                }
            }
            catch (OperationCanceledException)
            {
                AppendLog("[TIMEOUT] 주문테스트 응답 타임아웃 (120초 초과)");
            }
            catch (Exception ex)
            {
                AppendLog("[ERR] 주문테스트 실패: " + ex.Message);
            }
            finally
            {
                _btnOrderTest.Enabled = _conn?.IsConnected ?? false;
                _btnOrderTest.Text = "주문 극한테스트";
            }
        }

        // ═══════════════════════════════════════════
        //  상태 표시
        // ═══════════════════════════════════════════
        private void OnConnectionChanged(bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(OnConnectionChanged), connected);
                return;
            }

            _lblConnState.Text = connected ? "● 연결됨" : "● 끊김";
            _lblConnState.ForeColor = connected ? Color.FromArgb(80, 250, 120) : Color.Red;

            if (!connected)
            {
                _lblStatus.Text = "연결 끊김";
                _btnConnect.Enabled = true;
                _btnConnect.Text = "연결";
                _btnOrderTest.Enabled = false;
                AppendLog("Server32 연결이 끊어졌습니다");
            }
        }

        private void UpdateKiwoomIndicator(bool ok)
        {
            _lblKiwoomState.Text = ok ? "K:OK" : "K:X";
            _lblKiwoomState.ForeColor = ok ? Color.FromArgb(80, 250, 120) : Color.Gray;
        }

        private void UpdateCybosIndicator(bool ok)
        {
            _lblCybosState.Text = ok ? "C:OK" : "C:X";
            _lblCybosState.ForeColor = ok ? Color.FromArgb(80, 200, 255) : Color.Gray;
        }

        // ═══════════════════════════════════════════
        //  차트 폼 (멀티 인스턴스 – Document 영역)
        // ═══════════════════════════════════════════
        public void ShowChart(string stockCode, string stockName)
        {
            foreach (var content in _dockPanel.Documents)
            {
                if (content is ChartForm existing && existing.StockCode == stockCode)
                {
                    existing.Activate();
                    return;
                }
            }

            var chart = new ChartForm(stockCode, stockName, _candle);
            chart.Show(_dockPanel, DockState.Document);

            // 데이터 비동기 로드
            _ = Task.Run(async () =>
            {
                try
                {
                    AppendLog($"[차트] 데이터 로드 시작: {stockName}({stockCode})");
                    var candles = await _candle.GetCandlesAsync(stockCode, CandleType.Minute, 1, 900);
                    
                    this.BeginInvoke((Action)(() =>
                    {
                        if (!chart.IsDisposed)
                        {
                            chart.LoadChartData(stockCode, stockName, new List<CandleData>(candles));
                            AppendLog($"[차트] 데이터 로드 완료: {candles.Count}개");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    AppendLog($"[차트] 데이터 로드 실패: {ex.Message}");
                }
            });
        }

        public async Task ExecuteConditionAsync(int index, string name)
        {
            try
            {
                AppendLog($"조건검색 실행 요청: {name} ({index})");
                string resp = await _conn.ExecuteConditionAsync(index, name);
                
                if (!string.IsNullOrEmpty(resp))
                {
                    AppendLog($"조건검색 결과 수신: {resp.Length}자");
                    this.BeginInvoke((Action)(() =>
                    {
                        ShowOrActivate(ref _watchForm);
                        _watchForm?.OnConditionResult(resp);
                    }));
                }
            }
            catch (Exception ex)
            {
                AppendLog($"조건검색 실행 실패: {ex.Message}");
            }
        }

        public void ShowConditionSearch()
        {
            if (_conn == null || !_conn.IsConnected)
            {
                MessageBox.Show("서버에 연결된 상태에서만 가능합니다.");
                return;
            }

            using (var dlg = new ConditionSearchForm(this))
            {
                dlg.ShowDialog(this);
            }
        }

        // ═══════════════════════════════════════════
        //  실시간 구독 (WatchForm에서 호출)
        // ═══════════════════════════════════════════
        public async Task SubscribeRealtimeAsync(string code)
        {
            if (_market == null) return;
            try
            {
                await _market.SubscribeAsync(code);
                AppendLog($"실시간 등록: {code}");
            }
            catch (Exception ex)
            {
                AppendLog($"실시간 등록 실패: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════
        //  공개 서비스 접근자 (자식 폼용)
        // ═══════════════════════════════════════════
        public ConnectionService Connection => _conn;
        public CandleService Candles => _candle;
        public OrderService Orders => _order;

        // ═══════════════════════════════════════════
        //  로그
        // ═══════════════════════════════════════════
        public void AppendLog(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), msg);
                return;
            }
            var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            _logForm?.AppendLog(timestamped);
        }

        // ═══════════════════════════════════════════
        //  잔고 조회 (신규)
        // ═══════════════════════════════════════════
        private async Task RequestBalanceAsync()
        {
            if (_conn == null || !_conn.IsConnected)
            {
                AppendLog("[오류] 서버에 연결되지 않았습니다");
                return;
            }

            try
            {
                AppendLog("잔고 조회 요청...");
                var resp = await _conn.RequestAsync(MessageTypes.BalanceRequest, null, 10000);

                if (resp.respType == MessageTypes.BalanceResponse)
                {
                    var balances = BinarySerializer.DeserializeBalanceBatch(resp.respBody);
                    AppendLog($"잔고 수신: {balances.Count}종목");

                    foreach (var b in balances)
                    {
                        _portfolioForm?.UpdatePosition(b);
                    }
                }
                else if (resp.respType == MessageTypes.ErrorResponse)
                {
                    AppendLog("[오류] " + Encoding.UTF8.GetString(resp.respBody));
                }
            }
            catch (Exception ex)
            {
                AppendLog($"잔고 조회 실패: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════
        //  헬퍼: 싱글톤 DockContent 표시/활성화
        // ═══════════════════════════════════════════
        private void ShowOrActivate<T>(ref T form) where T : DockContent, new()
        {
            if (form == null || form.IsDisposed)
            {
                form = CreateForm<T>();
                form.Show(_dockPanel, GetDefaultDockState<T>());
            }
            else
            {
                form.Activate();
            }
        }

        private T CreateForm<T>() where T : DockContent, new()
        {
            if (typeof(T) == typeof(WatchForm))
                return (T)(DockContent)new WatchForm(this);
            return new T();
        }

        private DockState GetDefaultDockState<T>()
        {
            if (typeof(T) == typeof(LogForm)) return DockState.DockBottom;
            if (typeof(T) == typeof(SettingsForm)) return DockState.DockRight;
            if (typeof(T) == typeof(WatchForm)) return DockState.DockLeft;
            if (typeof(T) == typeof(PortfolioForm)) return DockState.DockBottom;
            return DockState.Document;
        }

        // ═══════════════════════════════════════════
        //  멀티차트 생성
        // ═══════════════════════════════════════════
        private void ShowMultiChart()
        {
            var mcf = new MultiChartForm(_candle, _order);
            mcf.Show(_dockPanel, DockState.Document);
        }

        // ═══════════════════════════════════════════
        //  정리
        // ═══════════════════════════════════════════
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _clockTimer?.Stop();
            _conn?.Disconnect();
            _conn?.Dispose();
            base.OnFormClosing(e);
        }
    }

    // ═══════════════════════════════════════════════
    //  다크 테마 렌더러
    // ═══════════════════════════════════════════════
    internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(30, 30, 40)))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new Pen(Color.FromArgb(70, 70, 70)))
                e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
        }
    }
}