using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bridge;
using Common.Models;
using App64.Services;

namespace App64
{
    public partial class MainForm : Form
    {
        private ConnectionService _connection;
        private MarketDataService _marketData;
        private OrderService _orderService;
        private CandleService _candleService;
        private int _msgCount;

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            _connection = new ConnectionService();
            _connection.OnConnectionChanged += OnConnectionChanged;
            _connection.OnLog += Log;

            _marketData = new MarketDataService(_connection);
            _marketData.OnMarketDataUpdated += OnMarketDataUpdated;

            _orderService = new OrderService(_connection);
            _candleService = new CandleService(_connection);
        }

        // ── 서버 연결 ──

        private async void mnuServerConnect_Click(object sender, EventArgs e)
        {
            mnuServerConnect.Enabled = false;
            Log("서버 연결 중...");

            try
            {
                await _connection.ConnectAsync();
                var status = await _connection.CheckLoginAsync();
                Log($"로그인 상태 — Kiwoom: {status.kiwoom}, Cybos: {status.cybos}");

                mnuServerDisconnect.Enabled = true;
            }
            catch (Exception ex)
            {
                Log($"[ERR] 연결 실패: {ex.Message}");
                mnuServerConnect.Enabled = true;
            }
        }

        private void mnuServerDisconnect_Click(object sender, EventArgs e)
        {
            _marketData.UnsubscribeAll();
            _connection.Disconnect();
            mnuServerConnect.Enabled = true;
            mnuServerDisconnect.Enabled = false;
            Log("서버 연결 해제");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _marketData?.UnsubscribeAll();
            _connection?.Disconnect();
        }

        // ── 이벤트 핸들러 ──

        private void OnConnectionChanged(bool connected)
        {
            SafeInvoke(() =>
            {
                lblConnection.Text = connected ? "● 연결됨" : "● 미연결";
                lblConnection.ForeColor = connected
                    ? System.Drawing.Color.FromArgb(80, 250, 120)
                    : System.Drawing.Color.Gray;
            });
        }

        private void OnMarketDataUpdated(MarketData md)
        {
            _msgCount++;
            SafeInvoke(() =>
            {
                lblMsgCount.Text = $"Msg: {_msgCount}";
                UpdateWatchListItem(md);
            });
        }

        private void UpdateWatchListItem(MarketData md)
        {
            foreach (ListViewItem item in lvWatchList.Items)
            {
                if (item.Text == md.StockCode)
                {
                    item.SubItems[2].Text = md.Price.ToString("N0");
                    item.SubItems[3].Text = md.ChangeRate.ToString("F2") + "%";
                    item.SubItems[4].Text = md.Volume.ToString("N0");

                    item.SubItems[3].ForeColor = md.Change > 0
                        ? System.Drawing.Color.FromArgb(255, 80, 80)
                        : md.Change < 0
                            ? System.Drawing.Color.FromArgb(80, 120, 255)
                            : System.Drawing.Color.White;
                    return;
                }
            }

            // 새 종목 추가
            var newItem = new ListViewItem(new string[] {
                md.StockCode, "", md.Price.ToString("N0"),
                md.ChangeRate.ToString("F2") + "%", md.Volume.ToString("N0")
            });
            lvWatchList.Items.Add(newItem);
        }

        // ── 공개 메서드 (다른 폼에서 호출) ──

        public async Task SubscribeRealtimeAsync(string code)
        {
            await _marketData.SubscribeAsync(code);
            Log($"[RT+] {code} 실시간 구독");
        }

        public async Task<System.Collections.Generic.IReadOnlyList<CandleData>> GetCandlesAsync(
            string code, char chartType = 'D', int count = 100, int interval = 1)
        {
            return await _candleService.GetCandlesAsync(code, chartType, count, interval);
        }

        public async Task<OrderInfo> SendOrderAsync(string code, bool buy, int price, int qty)
        {
            return await _orderService.SendOrderAsync(code, buy, price, qty);
        }

        // ── 유틸 ──

        public void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            SafeInvoke(() =>
            {
                if (txtLog.TextLength > 100000) txtLog.Clear();
                txtLog.AppendText(line + Environment.NewLine);
            });
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
    }
}