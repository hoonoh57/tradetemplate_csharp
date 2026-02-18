using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bridge;
using Common.Enums;
using Common.Models;
using Server32.Kiwoom;
using Server32.Cybos;

namespace Server32
{
    /// <summary>
    /// 통합 메시지 디스패처 — Pipe 메시지를 키움/Cybos 모듈로 라우팅
    /// Skills 파일 §8 데이터소스 선택 전략 준수
    /// </summary>
    public sealed class ServerDispatcher
    {
        private readonly PipeServer _pipe;
        private readonly MainForm _form;
        private KiwoomConnector _kiwoom;
        private KiwoomTrManager _kiwoomTr;
        private KiwoomRealtimeReceiver _kiwoomRt;
        private KiwoomOrderExecutor _kiwoomOrder;
        private CybosConnector _cybos;
        private CybosStockChart _cybosChart;
        private CybosRealtimeReceiver _cybosRt;
        private CybosOrderExecutor _cybosOrder;

        private int _msgCount;
        private int _rtCodeCount;

        public event Action<string> OnLog;
        public event Action<int, int> OnStatsUpdated;

        public ServerDispatcher(PipeServer pipe, MainForm form)
        {
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _pipe.OnMessageReceived += OnMessage;
        }

        public async Task InitializeAsync()
        {
            // 키움 초기화 (COM은 STA 스레드에서)
            try
            {
                _kiwoom = new KiwoomConnector();
                bool kOk = _kiwoom.Initialize();
                _form.UpdateKiwoomStatus(kOk);
                if (kOk)
                {
                    _kiwoomTr = new KiwoomTrManager(_kiwoom);
                    _kiwoomRt = new KiwoomRealtimeReceiver(_kiwoom);
                    _kiwoomOrder = new KiwoomOrderExecutor(_kiwoom);
                    _kiwoomRt.OnMarketDataReceived += OnKiwoomRealtimeData;
                    OnLog?.Invoke("[KIWOOM] 초기화 완료");
                }
                else
                {
                    OnLog?.Invoke("[KIWOOM] 초기화 실패 — 키움 OpenAPI가 설치되지 않았거나 로그인 필요");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[KIWOOM ERR] {ex.Message}");
                _form.UpdateKiwoomStatus(false);
            }

            // Cybos 초기화
            try
            {
                _cybos = new CybosConnector();
                bool cOk = _cybos.IsConnected;
                _form.UpdateCybosStatus(cOk);
                if (cOk)
                {
                    _cybos.InitTrade();
                    _cybosChart = new CybosStockChart(_cybos);
                    _cybosRt = new CybosRealtimeReceiver();
                    string acc = _cybos.GetFirstAccount();
                    string goods = _cybos.GetGoodsCode(acc);
                    _cybosOrder = new CybosOrderExecutor(acc, goods);
                    _cybosRt.OnMarketDataReceived += OnCybosRealtimeData;
                    OnLog?.Invoke($"[CYBOS] 초기화 완료 — 남은조회: {_cybos.RemainingCount}");
                }
                else
                {
                    OnLog?.Invoke("[CYBOS] 미접속 — CYBOS Plus를 관리자 권한으로 실행하세요");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[CYBOS ERR] {ex.Message}");
                _form.UpdateCybosStatus(false);
            }

            await Task.CompletedTask;
        }

        private async void OnMessage(ushort msgType, uint seqNo, byte[] body)
        {
            Interlocked.Increment(ref _msgCount);
            OnStatsUpdated?.Invoke(_msgCount, _rtCodeCount);

            try
            {
                switch (msgType)
                {
                    case MessageTypes.LoginRequest:
                        await HandleLogin(seqNo);
                        break;

                    case MessageTypes.CandleBatchRequest:
                        await HandleCandleBatch(seqNo, body);
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

                    case MessageTypes.StockInfoRequest:
                        await HandleStockInfo(seqNo, body);
                        break;

                    case MessageTypes.BalanceRequest:
                        await HandleBalance(seqNo);
                        break;

                    case MessageTypes.ConditionRequest:
                        await HandleCondition(seqNo, body);
                        break;

                    case MessageTypes.Heartbeat:
                        await _pipe.SendAsync(MessageTypes.Heartbeat, seqNo, new byte[0]);
                        break;

                    default:
                        OnLog?.Invoke($"[WARN] Unknown msg: 0x{msgType:X4}");
                        await _pipe.SendErrorAsync(seqNo, $"Unknown message type: 0x{msgType:X4}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[ERR] {MessageTypes.GetName(msgType)}: {ex.Message}");
                try { await _pipe.SendErrorAsync(seqNo, ex.Message); } catch { }
            }
        }

        // ── 핸들러 구현 ──

        private async Task HandleLogin(uint seqNo)
        {
            bool kOk = _kiwoom?.IsConnected ?? false;
            bool cOk = _cybos?.IsConnected ?? false;
            byte[] resp = new byte[] { (byte)(kOk ? 1 : 0), (byte)(cOk ? 1 : 0) };
            await _pipe.SendAsync(MessageTypes.LoginResponse, seqNo, resp);
            OnLog?.Invoke($"[LOGIN] Kiwoom={kOk}, Cybos={cOk}");
        }

        private async Task HandleCandleBatch(uint seqNo, byte[] body)
        {
            // body: "종목코드|차트구분|개수" or "종목코드|차트구분|개수|분봉주기"
            string[] parts = Encoding.UTF8.GetString(body).Split('|');
            string code = parts[0];
            char chartGubun = parts.Length > 1 ? parts[1][0] : 'D';
            int count = parts.Length > 2 ? int.Parse(parts[2]) : 100;
            int interval = parts.Length > 3 ? int.Parse(parts[3]) : 1;

            CandleType ct = chartGubun switch
            {
                'D' => CandleType.Day, 'W' => CandleType.Week,
                'M' => CandleType.Month, 'm' => CandleType.Minute,
                'T' => CandleType.Tick, _ => CandleType.Day
            };

            // Skills §8: 캔들 배치는 Cybos 우선
            if (_cybosChart != null && (_cybos?.IsConnected ?? false))
            {
                var candles = _cybosChart.GetCandles(code, ct, interval, count);
                byte[] resp = BinarySerializer.SerializeCandleBatch(candles);
                await _pipe.SendAsync(MessageTypes.CandleBatchResponse, seqNo, resp);
                OnLog?.Invoke($"[CANDLE] {code} {chartGubun} x{count} → {candles.Count}건 (Cybos)");
            }
            else if (_kiwoomTr != null)
            {
                // Cybos 불가시 키움 fallback
                OnLog?.Invoke($"[CANDLE] {code} — Cybos 미접속, 키움 TR 대체 (미구현)");
                await _pipe.SendErrorAsync(seqNo, "Cybos not connected, Kiwoom TR fallback not yet implemented");
            }
            else
            {
                await _pipe.SendErrorAsync(seqNo, "No data source available");
            }
        }

        private async Task HandleOrder(uint seqNo, byte[] body)
        {
            // body: "종목코드|B/S|가격|수량|소스(kiwoom/cybos)"
            string[] parts = Encoding.UTF8.GetString(body).Split('|');
            string code = parts[0];
            OrderType ot = parts[1] == "B" ? OrderType.Buy : OrderType.Sell;
            int price = int.Parse(parts[2]);
            int qty = int.Parse(parts[3]);
            string source = parts.Length > 4 ? parts[4] : "kiwoom";
            OrderCondition cond = price == 0 ? OrderCondition.Market : OrderCondition.Limit;

            OrderInfo result;
            // Skills §8: 주문은 키움 우선
            if (source == "cybos" && _cybosOrder != null)
                result = _cybosOrder.SendOrder(code, ot, cond, price, qty);
            else if (_kiwoomOrder != null)
                result = _kiwoomOrder.SendOrder(code, ot, cond, price, qty);
            else
            {
                await _pipe.SendErrorAsync(seqNo, "No order executor available");
                return;
            }

            byte[] resp = BinarySerializer.SerializeOrder(result);
            await _pipe.SendAsync(MessageTypes.OrderResponse, seqNo, resp);
            OnLog?.Invoke($"[ORDER] {code} {ot} {qty}주 @{price} → {result.State} ({source})");
        }

        private void HandleRealtimeSubscribe(byte[] body)
        {
            string code = Encoding.UTF8.GetString(body);
            // Skills §8: 실시간은 키움 우선
            _kiwoomRt?.Subscribe(code);
            Interlocked.Increment(ref _rtCodeCount);
            OnStatsUpdated?.Invoke(_msgCount, _rtCodeCount);
            OnLog?.Invoke($"[RT+] {code} 구독");
        }

        private void HandleRealtimeUnsubscribe(byte[] body)
        {
            string code = Encoding.UTF8.GetString(body);
            _kiwoomRt?.Unsubscribe(code);
            Interlocked.Decrement(ref _rtCodeCount);
            OnStatsUpdated?.Invoke(_msgCount, _rtCodeCount);
            OnLog?.Invoke($"[RT-] {code} 해제");
        }

        private async Task HandleStockInfo(uint seqNo, byte[] body)
        {
            string code = Encoding.UTF8.GetString(body);
            // 키움 opt10001로 종목 기본정보 조회
            if (_kiwoomTr != null)
            {
                OnLog?.Invoke($"[STOCK] {code} 정보 조회 (키움)");
                // TR 비동기 요청 → 응답은 이벤트로 처리 후 Pipe 전송
                await _pipe.SendErrorAsync(seqNo, "StockInfo TR pending — async event driven");
            }
            else
            {
                await _pipe.SendErrorAsync(seqNo, "Kiwoom not connected");
            }
        }

        private async Task HandleBalance(uint seqNo)
        {
            // Cybos 잔고 조회 시도
            if (_cybosOrder != null && (_cybos?.IsConnected ?? false))
            {
                // CpTd6033 사용
                OnLog?.Invoke("[BALANCE] 잔고 조회 (Cybos)");
                await _pipe.SendErrorAsync(seqNo, "Balance query pending");
            }
            else
            {
                await _pipe.SendErrorAsync(seqNo, "No balance source available");
            }
        }

        private async Task HandleCondition(uint seqNo, byte[] body)
        {
            OnLog?.Invoke("[COND] 조건검색 요청");
            await _pipe.SendErrorAsync(seqNo, "Condition search pending");
        }

        // ── 실시간 데이터 → Pipe Push ──

        private async void OnKiwoomRealtimeData(MarketData md)
        {
            if (!_pipe.IsClientConnected) return;
            try
            {
                byte[] data = BinarySerializer.SerializeMarketData(md);
                await _pipe.SendAsync(MessageTypes.RealtimePush, 0, data);
            }
            catch { }
        }

        private async void OnCybosRealtimeData(MarketData md)
        {
            if (!_pipe.IsClientConnected) return;
            try
            {
                byte[] data = BinarySerializer.SerializeMarketData(md);
                await _pipe.SendAsync(MessageTypes.RealtimePush, 0, data);
            }
            catch { }
        }

        public void Shutdown()
        {
            _kiwoomRt?.UnsubscribeAll();
            _cybosRt?.UnsubscribeAll();
            OnLog?.Invoke("[SHUTDOWN] 모든 실시간 해제");
        }
    }
}