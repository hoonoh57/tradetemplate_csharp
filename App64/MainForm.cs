using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using App64.Services;
using Bridge;
using Common.Enums;
using Common.Models;

namespace App64
{
    public partial class MainForm : Form
    {
        private ConnectionService _conn;
        private MarketDataService _market;
        private OrderService _order;
        private CandleService _candle;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "TradingSystem — App64";
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _conn = new ConnectionService();
            _conn.OnLog += msg => AppendLog(msg);
            _conn.OnConnectionChanged += connected =>
            {
                this.BeginInvoke((Action)(() =>
                {
                    lblConnection.Text = connected ? "● 연결됨" : "● 끊김";
                    lblConnection.ForeColor = connected ? Color.FromArgb(80, 250, 120) : Color.Red;
                    btnOrderTest.Enabled = connected;
                }));
            };

            // Push 메시지 수신 처리 (Server32 → App64)
            _conn.OnPushReceived += (msgType, seqNo, body) =>
            {
                if (msgType == MessageTypes.OrderTestResponse)
                {
                    string log = Encoding.UTF8.GetString(body);
                    AppendLog(log);
                }
            };

            _market = new MarketDataService(_conn);
            _market.OnMarketDataUpdated += md =>
            {
                this.BeginInvoke((Action)(() => UpdateWatchListItem(md)));
            };

            _order = new OrderService(_conn);
            _candle = new CandleService(_conn);

            AppendLog("App64 시작됨. 서버 연결 대기...");

            try
            {
                await _conn.ConnectAsync();
                var loginStatus = await _conn.CheckLoginAsync();
                lblKiwoom.Text = loginStatus.Item1 ? "K:OK" : "K:X";
                lblKiwoom.ForeColor = loginStatus.Item1 ? Color.FromArgb(80, 250, 120) : Color.Gray;
                lblCybos.Text = loginStatus.Item2 ? "C:OK" : "C:X";
                lblCybos.ForeColor = loginStatus.Item2 ? Color.FromArgb(80, 200, 255) : Color.Gray;
                btnOrderTest.Enabled = true;
                AppendLog("서버 연결 성공");
            }
            catch (Exception ex)
            {
                AppendLog("서버 연결 실패: " + ex.Message);
            }
        }

        // ── 주문 극한테스트 버튼 ──
        private async void btnOrderTest_Click(object sender, EventArgs e)
        {
            btnOrderTest.Enabled = false;
            btnOrderTest.Text = "테스트 실행중...";
            AppendLog("═══════ 주문 극한테스트 요청 → Server32 ═══════");

            try
            {
                // 파이프로 테스트 명령 전송, 응답 대기 (최대 120초 — 테스트가 길 수 있음)
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
                btnOrderTest.Enabled = _conn?.IsConnected ?? false;
                btnOrderTest.Text = "주문 극한테스트";
            }
        }

        private void UpdateWatchListItem(MarketData md)
        {
            foreach (ListViewItem item in lvWatchList.Items)
            {
                if (item.Text == md.Code)
                {
                    item.SubItems[2].Text = md.Price.ToString("N0");

                    double changeRate = md.ChangeRate;
                    string sign = changeRate >= 0 ? "+" : "";
                    item.SubItems[3].Text = sign + changeRate.ToString("F2") + "%";

                    if (changeRate > 0)
                        item.ForeColor = Color.Red;
                    else if (changeRate < 0)
                        item.ForeColor = Color.Blue;
                    else
                        item.ForeColor = Color.Black;

                    item.SubItems[4].Text = md.AccVolume.ToString("N0");
                    return;
                }
            }
        }

        private async void mnuServerConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await _conn.ConnectAsync();
                AppendLog("수동 연결 성공");
            }
            catch (Exception ex)
            {
                AppendLog("수동 연결 실패: " + ex.Message);
            }
        }

        private void mnuServerDisconnect_Click(object sender, EventArgs e)
        {
            _conn?.Disconnect();
            AppendLog("연결 해제됨");
        }

        public async void SubscribeRealtimeAsync(string code)
        {
            try
            {
                await _market.SubscribeAsync(code);
                AppendLog("실시간 등록: " + code);
            }
            catch (Exception ex)
            {
                AppendLog("실시간 등록 실패: " + ex.Message);
            }
        }

        public async void GetCandlesAsync(string code)
        {
            try
            {
                var candles = await _candle.GetCandlesAsync(code, CandleType.Daily, 1, 200);
                AppendLog(code + " 캔들 " + candles.Count + "개 수신");
            }
            catch (Exception ex)
            {
                AppendLog("캔들 조회 실패: " + ex.Message);
            }
        }

        public async void SendOrderAsync(string code, int price, int qty)
        {
            try
            {
                var result = await _order.SendOrderAsync(code, price, qty);
                AppendLog("주문 결과: " + result.Message);
            }
            catch (Exception ex)
            {
                AppendLog("주문 실패: " + ex.Message);
            }
        }

        private void AppendLog(string msg)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke((Action<string>)AppendLog, msg);
                return;
            }
            if (txtLog.TextLength > 200000)
                txtLog.Clear();
            txtLog.AppendText(DateTime.Now.ToString("[HH:mm:ss.fff] ") + msg + Environment.NewLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _conn?.Disconnect();
            _conn?.Dispose();
            base.OnFormClosing(e);
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void mnuViewLog_Click(object sender, EventArgs e)
        {
            this.tabRight.SelectedTab = this.tabLog;
        }




    }

    //protected override void OnFormClosing(FormClosingEventArgs e)

}