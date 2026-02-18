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
        private int _msgCount;
        private int _rtCount;

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            Log("=== 서버 시작 ===");

            try
            {
                // 1) Pipe 서버 시작
                _pipeServer = new PipeServer();
                _pipeServer.OnClientConnectionChanged += OnPipeConnection;
                _pipeServer.OnError += err => SafeLog($"[PIPE ERR] {err}");
                _ = _pipeServer.StartAsync();
                UpdatePipeStatus(false);
                Log("[PIPE] Named Pipe 서버 대기중...");

                // 2) 디스패처 생성 (키움/Cybos 초기화 포함)
                _dispatcher = new ServerDispatcher(_pipeServer, this);
                _dispatcher.OnLog += SafeLog;
                _dispatcher.OnStatsUpdated += UpdateStats;
                await _dispatcher.InitializeAsync();

                Log("[OK] 서버 초기화 완료");
            }
            catch (Exception ex)
            {
                Log($"[ERR] 시작 실패: {ex.Message}");
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Log("=== 서버 중지 ===");
            _dispatcher?.Shutdown();
            _pipeServer?.Stop();
            _pipeServer?.Dispose();
            _pipeServer = null;
            _dispatcher = null;

            UpdateKiwoomStatus(false);
            UpdateCybosStatus(false);
            UpdatePipeStatus(false);

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            Log("[OK] 서버 중지 완료");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _dispatcher?.Shutdown();
            _pipeServer?.Dispose();
        }

        // ── UI 업데이트 (thread-safe) ──

        private void OnPipeConnection(bool connected)
        {
            SafeInvoke(() => UpdatePipeStatus(connected));
            SafeLog(connected ? "[PIPE] 클라이언트 접속" : "[PIPE] 클라이언트 해제");
        }

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
            _msgCount = msgCount;
            _rtCount = rtCount;
            SafeInvoke(() =>
            {
                lblStats.Text = $"Msg: {_msgCount} | RT: {_rtCount} codes";
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

        private void SafeLog(string msg)
        {
            Log(msg);
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }
    }
}