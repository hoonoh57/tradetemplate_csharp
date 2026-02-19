using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bridge;
using Common.Enums;
using Common.Models;
using Server32.Kiwoom;
using Server32.Cybos;

namespace Server32
{
    public class ServerDispatcher
    {
        private readonly PipeServer _pipe;
        private readonly MainForm _mainForm;

        private KiwoomConnector _kiwoom;
        private KiwoomTrManager _kiwoomTr;
        private KiwoomRealtimeReceiver _kiwoomRealtime;
        private KiwoomOrderExecutor _kiwoomOrder;
        private OrderManager _orderManager;
        private CybosConnector _cybos;
        private CybosStockChart _cybosChart;
        private CybosRealtimeReceiver _cybosRealtime;
        private CybosOrderExecutor _cybosOrder;

        private int _msgCount;
        private int _rtCount;

        public event Action<string> OnLog;
        public event Action<int, int> OnStatsUpdated;

        public ServerDispatcher(PipeServer pipe, MainForm mainForm)
        {
            _pipe = pipe;
            _mainForm = mainForm;
            _pipe.OnMessageReceived += OnPipeMessage;

            this.OnLog += msg => PushLog(msg);
        }

        private void PushLog(string msg)
        {
            try
            {
                if (_pipe.IsClientConnected)
                {
                    byte[] body = Encoding.UTF8.GetBytes(msg);
                    _pipe.SendAsync(MessageTypes.SystemLogPush, 0, body).ConfigureAwait(false);
                }
            }
            catch { }
        }

        public async Task InitializeAsync()
        {
            // ★ 1단계: Cybos 먼저 체크 (필수)
            try
            {
                _cybos = new CybosConnector();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("[ERR] CybosPlus COM 초기화 실패: " + ex.Message);
                MessageBox.Show("Cybos 연결을 확인하세요!\nCybosPlus를 먼저 실행 후 관리자 권한으로 다시 시작하세요.",
                    "Cybos 연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            if (!_cybos.IsConnected)
            {
                OnLog?.Invoke("[ERR] CybosPlus 미접속 상태");
                MessageBox.Show("Cybos 연결을 확인하세요!\nCybosPlus가 로그인되어 있지 않습니다.",
                    "Cybos 연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            _cybos.InitTrade();
            _mainForm.UpdateCybosStatus(true);
            OnLog?.Invoke("[Cybos] 접속 확인 완료");

            if (true)
            {
                _cybosChart = new CybosStockChart(_cybos);
                _cybosChart.Initialize();
                _cybosRealtime = new CybosRealtimeReceiver(_cybos);
                _cybosRealtime.Initialize();
                _cybosRealtime.OnMarketDataReceived += md => PushMarketData(md);
                _cybosOrder = new CybosOrderExecutor(_cybos);
                _cybosOrder.Initialize();
                OnLog?.Invoke("[Cybos] 차트/실시간/주문 모듈 초기화 완료");
            }

            // ★ 2단계: 키움 초기화
            _kiwoom = new KiwoomConnector();
            _kiwoom.OnLog += msg => OnLog?.Invoke(msg);
            bool kiwoomInit = _kiwoom.Initialize(_mainForm);
            if (kiwoomInit)
            {
                OnLog?.Invoke("[키움] COM 초기화 성공, 로그인 시도...");
                bool kiwoomOk = await _kiwoom.LoginAsync(60000);
                _mainForm.UpdateKiwoomStatus(kiwoomOk);
                OnLog?.Invoke(kiwoomOk
                    ? "[키움] 로그인 성공. 계좌: " + _kiwoom.GetFirstAccount()
                    : "[키움] 로그인 실패 또는 타임아웃");

                if (kiwoomOk)
                {
                    _kiwoomTr = new KiwoomTrManager(_kiwoom);
                    _kiwoomTr.OnLog += msg => OnLog?.Invoke(msg);
                    _kiwoomTr.OnRealCondition += (code, type, name, idx) =>
                    {
                        string payload = $"{code}|{type}|{name}|{idx}";
                        _pipe.SendAsync(MessageTypes.ConditionRealtime, 0, Encoding.UTF8.GetBytes(payload)).ConfigureAwait(false);
                    };

                    _kiwoomRealtime = new KiwoomRealtimeReceiver(_kiwoom);
                    _kiwoomRealtime.Initialize();
                    _kiwoomRealtime.OnMarketDataReceived += md => PushMarketData(md);
                    _kiwoomOrder = new KiwoomOrderExecutor(_kiwoom);
                    _kiwoomOrder.SetAccount(_kiwoom.GetFirstAccount());

                    // OrderManager 초기화
                    _orderManager = new OrderManager(_kiwoom, _kiwoom.GetFirstAccount());
                    _orderManager.OnLog += msg => OnLog?.Invoke(msg);

                    // ★ 3단계: 계좌 조회
                    string acct = _kiwoom.GetFirstAccount();
                    OnLog?.Invoke($"[키움] 계좌 {acct} 잔고/보유/미체결 조회 시작...");

                    await Task.Delay(1000);
                    await _kiwoomTr.QueryAccountBalanceAsync(acct);

                    await Task.Delay(1000);
                    await _kiwoomTr.QueryPendingOrdersAsync(acct);

                    // LoadConditionListAsync만 호출하여 목록만 로드
                    await _kiwoomTr.LoadConditionListAsync();

                    //// ★ 5단계: 주문 테스트 (장중에만 실행)
                    //await RunOrderTestIfMarketOpen();

                    OnLog?.Invoke("[키움] 전체 초기화 완료");
                }
            }
            else
            {
                _mainForm.UpdateKiwoomStatus(false);
                OnLog?.Invoke("[키움] COM 초기화 실패 (OpenAPI 미설치?)");
            }
        }

        private async Task RunOrderTestIfMarketOpen()
        {
            var now = DateTime.Now;
            bool isMarketHours = now.Hour >= 9 && (now.Hour < 15 || (now.Hour == 15 && now.Minute <= 20));

            if (!_kiwoom.IsSimulation)
            {
                OnLog?.Invoke("[주문테스트] 실서버 — 자동 테스트 건너뜀");
                return;
            }

            if (!isMarketHours)
            {
                OnLog?.Invoke($"[주문테스트] 장외시간({now:HH:mm}) — 자동 테스트 건너뜀 (장중 09:00~15:20에 실행)");
                return;
            }

            OnLog?.Invoke("[주문테스트] 모의투자 장중 — 자동 테스트 실행");
            await Task.Delay(2000);

            var testRunner = new OrderTestRunner(_orderManager, _kiwoom, _kiwoom.GetFirstAccount());
            testRunner.OnLog += msg => OnLog?.Invoke(msg);
            await testRunner.RunAllTestsAsync();
        }

        public void Shutdown()
        {
            _pipe.OnMessageReceived -= OnPipeMessage;
        }

        // ── 주문테스트 ──

        public bool IsOrderTestReady =>
            _kiwoom != null && _kiwoom.IsConnected && _kiwoom.IsSimulation && _orderManager != null;

        public async Task RunOrderTestAsync()
        {
            if (!IsOrderTestReady)
            {
                OnLog?.Invoke("[주문테스트] 준비 안됨 (키움 미접속 또는 실서버)");
                return;
            }

            var testRunner = new OrderTestRunner(_orderManager, _kiwoom, _kiwoom.GetFirstAccount());
            testRunner.OnLog += msg => OnLog?.Invoke(msg);
            await testRunner.RunAllTestsAsync();
        }




        private void PushMarketData(MarketData md)
        {
            try
            {
                _rtCount++;
                byte[] body = BinarySerializer.SerializeMarketData(md);
                _pipe.SendAsync(MessageTypes.RealtimePush, 0, body).ConfigureAwait(false);
                OnStatsUpdated?.Invoke(_msgCount, _rtCount);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("Push 실패: " + ex.Message);
            }
        }

        private async void OnPipeMessage(ushort msgType, uint seqNo, byte[] body)
        {
            _msgCount++;
            OnStatsUpdated?.Invoke(_msgCount, _rtCount);

            try
            {
                switch (msgType)
                {
                    case MessageTypes.LoginRequest:
                        await HandleLogin(seqNo);
                        break;

                    case MessageTypes.OrderRequest:
                        await HandleOrder(seqNo, body);
                        break;
                    case MessageTypes.RealtimeSubscribe:
                        HandleRealtimeSubscribe(body);
                        break;
                    case MessageTypes.RealtimeUnsubscribe:
                        HandleRealtimeUnsubscribe(body);
                        break;
                    case MessageTypes.Heartbeat:
                        await _pipe.SendAsync(MessageTypes.Heartbeat, seqNo, null);
                        break;

                    case MessageTypes.ConditionRequest:
                        await HandleConditionRequest(seqNo, body);
                        break;

                    case MessageTypes.BalanceRequest:
                        await HandleBalanceRequest(seqNo);
                        break;

                    case MessageTypes.CandleBatchRequest:
                        await HandleCandleBatchRequest(seqNo, body);
                        break;

                    case MessageTypes.OrderTestRequest:
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (!IsOrderTestReady)
                                {
                                    var errBody = Encoding.UTF8.GetBytes("주문테스트 불가: 키움 미접속 또는 실서버");
                                    await _pipe.SendAsync(MessageTypes.ErrorResponse, seqNo, errBody);
                                    return;
                                }

                                OnLog?.Invoke("[주문테스트] App64 요청 수신 — 테스트 시작");

                                var sb = new System.Text.StringBuilder();
                                var testRunner = new OrderTestRunner(_orderManager, _kiwoom, _kiwoom.GetFirstAccount());
                                testRunner.OnLog += line =>
                                {
                                    sb.AppendLine(line);
                                    OnLog?.Invoke(line);
                                };

                                await testRunner.RunAllTestsAsync();

                                var resultBody = Encoding.UTF8.GetBytes(sb.ToString());
                                await _pipe.SendAsync(MessageTypes.OrderTestResponse, seqNo, resultBody);
                                OnLog?.Invoke("[주문테스트] 결과 App64로 전송 완료");
                            }
                            catch (Exception ex)
                            {
                                OnLog?.Invoke($"[주문테스트 ERR] {ex.Message}");
                                var errBody = Encoding.UTF8.GetBytes($"테스트 실행 오류: {ex.Message}");
                                try { await _pipe.SendAsync(MessageTypes.ErrorResponse, seqNo, errBody); } catch { }
                            }
                        });
                        break;



                    default:
                        OnLog?.Invoke("알 수 없는 메시지: 0x" + msgType.ToString("X4"));
                        break;
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("메시지 처리 오류: " + ex.Message);
                try
                {
                    byte[] errBody = BinarySerializer.SerializeString(ex.Message);
                    await _pipe.SendAsync(MessageTypes.ErrorResponse, seqNo, errBody);
                }
                catch { }
            }
        }

        private async Task HandleLogin(uint seqNo)
        {
            bool kiwoomOk = _kiwoom != null && _kiwoom.IsConnected;
            bool cybosOk = _cybos != null && _cybos.IsConnected;

            using (var ms = new MemoryStream(2))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(kiwoomOk);
                bw.Write(cybosOk);
                await _pipe.SendAsync(MessageTypes.LoginResponse, seqNo, ms.ToArray());
            }
            OnLog?.Invoke("로그인 상태: 키움=" + kiwoomOk + ", Cybos=" + cybosOk);
        }

        private async Task HandleCandleBatch(uint seqNo, byte[] body)
        {
            using (var ms = new MemoryStream(body))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                string code = br.ReadString();
                CandleType type = (CandleType)br.ReadInt32();
                int interval = br.ReadInt32();
                int count = br.ReadInt32();

                OnLog?.Invoke("캔들 조회: " + code + " " + type + " " + count + "개");

                IReadOnlyList<CandleData> candles;
                if (_cybosChart != null)
                    candles = _cybosChart.GetCandles(code, type, interval, count);
                else
                    candles = new List<CandleData>().AsReadOnly();

                byte[] respBody = BinarySerializer.SerializeCandleBatch(candles);
                await _pipe.SendAsync(MessageTypes.CandleBatchResponse, seqNo, respBody);
                OnLog?.Invoke("캔들 응답: " + candles.Count + "개");
            }
        }

        private async Task HandleOrder(uint seqNo, byte[] body)
        {
            using (var ms = new MemoryStream(body))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                string code = br.ReadString();
                OrderType orderType = (OrderType)br.ReadInt32();
                int price = br.ReadInt32();
                int qty = br.ReadInt32();
                string source = br.ReadString();

                OnLog?.Invoke("주문: " + code + " " + orderType + " P:" + price + " Q:" + qty);

                OrderInfo result;

                // OrderManager를 통한 주문 (키움)
                if (source != "cybos" && _orderManager != null)
                {
                    int kiwoomOrderType = orderType == OrderType.Buy ? 1 : 2;
                    string hogaGb = price == 0 ? "03" : "00";
                    var managed = await _orderManager.SubmitOrderAsync(code, kiwoomOrderType, qty, price, hogaGb);

                    result = new OrderInfo(
                        orderNo: managed.KiwoomOrderNo ?? "", origOrderNo: "", code: code,
                        name: managed.Name, type: orderType,
                        condition: OrderCondition.Normal,
                        state: managed.Status == OrderStatus.Failed ? OrderState.Rejected : OrderState.Submitted,
                        orderPrice: price, orderQty: qty,
                        execPrice: managed.LastFilledPrice, execQty: managed.FilledQty,
                        remainQty: managed.RemainingQty,
                        orderTime: DateTime.Now, execTime: managed.LastFilledTime,
                        accountNo: _kiwoom.GetFirstAccount(),
                        message: managed.RejectReason ?? "");
                }
                else if (source == "cybos" && _cybosOrder != null)
                {
                    result = _cybosOrder.SendOrder(code, orderType, price, qty);
                }
                else
                {
                    result = new OrderInfo(
                        orderNo: "", origOrderNo: "", code: code, name: "",
                        type: orderType, condition: OrderCondition.Normal,
                        state: OrderState.Rejected,
                        orderPrice: price, orderQty: qty,
                        execPrice: 0, execQty: 0, remainQty: qty,
                        orderTime: DateTime.Now, execTime: DateTime.MinValue,
                        accountNo: "", message: "연결된 API 없음");
                }

                byte[] respBody = BinarySerializer.SerializeOrder(result);
                await _pipe.SendAsync(MessageTypes.OrderResponse, seqNo, respBody);
                OnLog?.Invoke("주문 응답: " + result.State);
            }
        }

        private async Task HandleConditionRequest(uint seqNo, byte[] body)
        {
            // 키움 미연결 시 방어 처리
            if (_kiwoomTr == null)
            {
                OnLog?.Invoke("[조건] 키움 미연결 상태 — 조건검색 불가");
                await _pipe.SendAsync(MessageTypes.ConditionRequest, seqNo,
                    BinarySerializer.SerializeString("ERROR:키움 미연결"));
                return;
            }

            string cmd = BinarySerializer.DeserializeString(body);
            if (cmd == "LIST")
            {
                try
                {
                    var list = await _kiwoomTr.LoadConditionListAsync();
                    var sb = new StringBuilder();
                    foreach (var c in list)
                    {
                        if (sb.Length > 0) sb.Append(";");
                        sb.Append($"{c.Index}^{c.Name}");
                    }
                    await _pipe.SendAsync(MessageTypes.ConditionRequest, seqNo, BinarySerializer.SerializeString(sb.ToString()));
                    OnLog?.Invoke($"[조건] 리스트 {list.Count}개 전송");
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke("[조건] 리스트 로드 실패: " + ex.Message);
                    await _pipe.SendAsync(MessageTypes.ConditionRequest, seqNo,
                        BinarySerializer.SerializeString("ERROR:" + ex.Message));
                }
            }
            else if (cmd.StartsWith("EXEC:"))
            {
                string[] parts = cmd.Substring(5).Split('^');
                if (parts.Length == 2 && int.TryParse(parts[0], out int idx))
                {
                    string name = parts[1];
                    OnLog?.Invoke($"[조건] 실행 요청: {name} ({idx})");
                    try
                    {
                        var codes = await _kiwoomTr.ExecuteConditionAsync(new Server32.Kiwoom.ConditionInfo { Index = idx, Name = name });

                        var stockInfoList = new StringBuilder();
                        foreach (var code in codes)
                        {
                            string cname = _kiwoom?.GetMasterCodeName(code) ?? code;
                            if (stockInfoList.Length > 0) stockInfoList.Append(";");
                            stockInfoList.Append($"{code}|{cname}|0");
                        }

                        await _pipe.SendAsync(MessageTypes.ConditionResult, seqNo, BinarySerializer.SerializeString(stockInfoList.ToString()));
                        OnLog?.Invoke($"[조건] 실행 결과 {codes.Count}종목 전송");
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke("[조건] 실행 실패: " + ex.Message);
                        await _pipe.SendAsync(MessageTypes.ConditionResult, seqNo,
                            BinarySerializer.SerializeString(""));
                    }
                }
            }
        }

        private void HandleRealtimeSubscribe(byte[] body)
        {
            string code = BinarySerializer.DeserializeString(body);
            OnLog?.Invoke("실시간 등록: " + code);
            _kiwoomRealtime?.Subscribe(code);
            _cybosRealtime?.Subscribe(code);
        }

        private void HandleRealtimeUnsubscribe(byte[] body)
        {
            string code = BinarySerializer.DeserializeString(body);
            OnLog?.Invoke("실시간 해제: " + code);
            _kiwoomRealtime?.Unsubscribe(code);
            _cybosRealtime?.Unsubscribe(code);
        }

        private async Task HandleBalanceRequest(uint seqNo)
        {
            try
            {
                if (_kiwoomTr == null)
                {
                    await _pipe.SendErrorAsync(seqNo, "키움 미접속");
                    return;
                }

                string acct = _kiwoom.GetFirstAccount();
                var balances = await _kiwoomTr.QueryAccountBalanceAsync(acct);

                byte[] body = BinarySerializer.SerializeBalanceBatch(balances);
                await _pipe.SendAsync(MessageTypes.BalanceResponse, seqNo, body);
                OnLog?.Invoke($"[잔고] {balances.Count}종목 전송");
            }
            catch (Exception ex)
            {
                await _pipe.SendErrorAsync(seqNo, $"잔고조회 실패: {ex.Message}");
            }
        }

        private async Task HandleCandleBatchRequest(uint seqNo, byte[] body)
        {
            try
            {
                if (_kiwoomTr == null)
                {
                    await _pipe.SendErrorAsync(seqNo, "키움 미접속");
                    return;
                }

                using (var ms = new MemoryStream(body))
                using (var br = new BinaryReader(ms, Encoding.UTF8))
                {
                    string code = br.ReadString();
                    CandleType type = (CandleType)br.ReadInt32();
                    int interval = br.ReadInt32();
                    int count = br.ReadInt32();

                    OnLog?.Invoke($"[캔들] 요청: {code} ({type}, {interval}분, {count}개)");
                    var candles = await _kiwoomTr.RequestCandlesAsync(code, type, count);

                    byte[] respBody = BinarySerializer.SerializeCandleBatch(candles);
                    await _pipe.SendAsync(MessageTypes.CandleBatchResponse, seqNo, respBody);
                    OnLog?.Invoke($"[캔들] 전송: {candles.Count}개");
                }
            }
            catch (Exception ex)
            {
                await _pipe.SendErrorAsync(seqNo, $"캔들요청 실패: {ex.Message}");
            }
        }
    }
}
