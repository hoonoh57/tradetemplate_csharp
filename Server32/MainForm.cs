using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bridge;
using Common.Modules;

namespace Server32
{
    public partial class MainForm : Form
    {
        private PipeServer _pipeServer;
        private ServerDispatcher _dispatcher;
        private bool _initialized;

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            StartServer();
        }

        // ═══════════════════════════════════════════
        //  서버 자동 시작 (사용자 조작 없음)
        // ═══════════════════════════════════════════
        private async void StartServer()
        {
            try
            {
                // 1) Pipe 서버 시작
                _pipeServer = new PipeServer();
                _pipeServer.OnClientConnectionChanged += OnPipeConnection;
                _pipeServer.OnError += err => Log($"[PIPE ERR] {err}");
                _ = _pipeServer.StartAsync();
                UpdatePipeStatus(false);
                Log("[PIPE] Named Pipe 서버 자동 대기중...");

                // 2) 디스패처 생성
                _dispatcher = new ServerDispatcher(_pipeServer, this);
                _dispatcher.OnLog += Log;
                _dispatcher.OnStatsUpdated += UpdateStats;

                // 3) 키움/Cybos 자동 초기화
                Log("=== 키움/Cybos 자동 초기화 시작 ===");
                await _dispatcher.InitializeAsync();
                _initialized = true;
                Log("=== 초기화 완료 — App64 명령 대기 ===");
            }
            catch (Exception ex)
            {
                Log($"[ERR] 서버 시작 실패: {ex.Message}");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _dispatcher?.Shutdown();
            _pipeServer?.Stop();
            _pipeServer?.Dispose();
        }

        // ═══════════════════════════════════════════
        //  Pipe 연결 이벤트
        // ═══════════════════════════════════════════
        private void OnPipeConnection(bool connected)
        {
            SafeInvoke(() => UpdatePipeStatus(connected));
            Log(connected ? "[PIPE] App64 접속" : "[PIPE] App64 연결 해제");
        }

        // ═══════════════════════════════════════════
        //  UI 업데이트 (thread-safe)
        // ═══════════════════════════════════════════
        public void UpdateKiwoomStatus(bool connected)
        {
            SafeInvoke(() =>
            {
                lblKiwoomStatus.Text = connected ? "● Kiwoom: 접속" : "● Kiwoom: 미접속";
                lblKiwoomStatus.ForeColor = connected
                    ? System.Drawing.Color.FromArgb(80, 250, 120)
                    : System.Drawing.Color.Gray;
            });
        }

        public void UpdateCybosStatus(bool connected)
        {
            SafeInvoke(() =>
            {
                lblCybosStatus.Text = connected ? "● Cybos: 접속" : "● Cybos: 미접속";
                lblCybosStatus.ForeColor = connected
                    ? System.Drawing.Color.FromArgb(80, 200, 255)
                    : System.Drawing.Color.Gray;
            });
        }

        private void UpdatePipeStatus(bool connected)
        {
            SafeInvoke(() =>
            {
                lblPipeStatus.Text = connected ? "● Pipe: 연결됨" : "● Pipe: 대기중";
                lblPipeStatus.ForeColor = connected
                    ? System.Drawing.Color.FromArgb(255, 220, 80)
                    : System.Drawing.Color.Gray;
            });
        }

        private void UpdateStats(int msgCount, int rtCount)
        {
            SafeInvoke(() =>
            {
                lblStats.Text = $"Msg: {msgCount} | RT: {rtCount}";
            });
        }

        public void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            SafeInvoke(() =>
            {
                if (txtLog.TextLength > 100000)
                    txtLog.Clear();
                txtLog.AppendText(line + Environment.NewLine);
            });
            LogManager.Instance.Info(msg);
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        // App64에서 호출하던 메서드 — 이제 불필요하지만 호환성 유지
        public Task StartTradingAsync() => Task.CompletedTask;
        public void StopTrading() { }
    }
}