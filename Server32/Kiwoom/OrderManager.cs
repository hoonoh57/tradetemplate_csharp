using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server32.Kiwoom
{
    // ══════════════════════════════════════════════
    //  주문 상태
    // ══════════════════════════════════════════════

    public enum OrderStatus
    {
        Queued,         // 큐 대기
        Submitted,      // SendOrder 호출됨
        Accepted,       // 접수 확인 (sGubun=0, 주문상태=접수)
        PartialFilled,  // 부분 체결
        Filled,         // 전량 체결
        Cancelled,      // 취소됨
        Rejected,       // 거부됨
        Failed          // SendOrder 실패
    }

    // ══════════════════════════════════════════════
    //  주문 정보
    // ══════════════════════════════════════════════

    public class ManagedOrder
    {
        public string ClientOrderId { get; set; }   // 내부 관리 ID
        public string KiwoomOrderNo { get; set; }    // 키움 주문번호
        public string Code { get; set; }
        public string Name { get; set; }
        public int OrderType { get; set; }           // 1=매수, 2=매도, 3=매수취소, 4=매도취소, 5=매수정정, 6=매도정정
        public int Qty { get; set; }
        public int Price { get; set; }
        public string HogaGb { get; set; }           // "00"=지정가, "03"=시장가
        public string OrgOrderNo { get; set; }       // 원주문번호 (정정/취소용)
        public string ScreenNo { get; set; }
        public string AccountNo { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Queued;
        public int FilledQty { get; set; }
        public int RemainingQty { get; set; }
        public long FilledAmount { get; set; }       // 체결누계금액
        public int LastFilledPrice { get; set; }     // 최종체결가
        public int LastFilledQty { get; set; }       // 최종체결수량

        public DateTime QueuedTime { get; set; }
        public DateTime SubmittedTime { get; set; }
        public DateTime AcceptedTime { get; set; }
        public DateTime LastFilledTime { get; set; }
        public DateTime CompletedTime { get; set; }

        public string RejectReason { get; set; }
        public string Message { get; set; }

        public List<FillRecord> Fills { get; set; } = new List<FillRecord>();

        public string OrderTypeStr
        {
            get
            {
                switch (OrderType)
                {
                    case 1: return "매수";
                    case 2: return "매도";
                    case 3: return "매수취소";
                    case 4: return "매도취소";
                    case 5: return "매수정정";
                    case 6: return "매도정정";
                    default: return $"미정({OrderType})";
                }
            }
        }

        public double AvgFillPrice => FilledQty > 0 ? (double)FilledAmount / FilledQty : 0;
        public TimeSpan Latency => CompletedTime > SubmittedTime
            ? CompletedTime - SubmittedTime : TimeSpan.Zero;
    }

    public class FillRecord
    {
        public string FillNo { get; set; }
        public int Price { get; set; }
        public int Qty { get; set; }
        public DateTime Time { get; set; }
    }

    // ══════════════════════════════════════════════
    //  잔고 (포지션)
    // ══════════════════════════════════════════════

    public class Position
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public long BuyPrice { get; set; }       // 매입단가
        public long TotalBuyAmount { get; set; } // 총매입가
        public int AvailableQty { get; set; }    // 주문가능수량
        public long CurrentPrice { get; set; }
        public double ProfitRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    // ══════════════════════════════════════════════
    //  OrderThrottler — 초당 5회 제한 관리
    // ══════════════════════════════════════════════

    public class OrderThrottler
    {
        private readonly int _maxPerSecond;
        private readonly Queue<DateTime> _timestamps = new Queue<DateTime>();
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(1, 1);

        public int QueuedCount => _pendingOrders.Count;
        private readonly ConcurrentQueue<Func<Task>> _pendingOrders = new ConcurrentQueue<Func<Task>>();
        private bool _processing;

        public event Action<string> OnLog;

        public OrderThrottler(int maxPerSecond = 4)  // 안전마진: 5회 제한이지만 4회로
        {
            _maxPerSecond = maxPerSecond;
        }

        public async Task<bool> WaitForSlotAsync(int timeoutMs = 5000)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutMs);

            while (DateTime.Now < deadline)
            {
                await _sem.WaitAsync();
                try
                {
                    // 1초 지난 타임스탬프 제거
                    while (_timestamps.Count > 0 && (DateTime.Now - _timestamps.Peek()).TotalMilliseconds > 1000)
                        _timestamps.Dequeue();

                    if (_timestamps.Count < _maxPerSecond)
                    {
                        _timestamps.Enqueue(DateTime.Now);
                        return true;
                    }
                }
                finally
                {
                    _sem.Release();
                }

                // 슬롯이 없으면 대기
                var oldest = _timestamps.Peek();
                int waitMs = (int)(1000 - (DateTime.Now - oldest).TotalMilliseconds) + 50;
                if (waitMs > 0)
                {
                    OnLog?.Invoke($"[쓰로틀] {waitMs}ms 대기 (슬롯 부족)");
                    await Task.Delay(Math.Min(waitMs, 1000));
                }
            }

            OnLog?.Invoke("[쓰로틀] 타임아웃!");
            return false;
        }

        public void Reset()
        {
            _timestamps.Clear();
        }
    }

    // ══════════════════════════════════════════════
    //  OrderManager — 주문 중앙 관리자
    // ══════════════════════════════════════════════

    public class OrderManager
    {
        private readonly KiwoomConnector _connector;
        private readonly OrderThrottler _throttler;
        private readonly string _accountNo;
        private int _clientOrderSeq;
        private int _screenSeq = 5000;

        // 주문장부
        private readonly ConcurrentDictionary<string, ManagedOrder> _ordersByClientId = new ConcurrentDictionary<string, ManagedOrder>();
        private readonly ConcurrentDictionary<string, ManagedOrder> _ordersByKiwoomNo = new ConcurrentDictionary<string, ManagedOrder>();

        // 잔고
        private readonly ConcurrentDictionary<string, Position> _positions = new ConcurrentDictionary<string, Position>();

        // 매매이력
        private readonly List<ManagedOrder> _tradeHistory = new List<ManagedOrder>();
        private readonly object _historyLock = new object();

        // 통계
        public int TotalOrders { get; private set; }
        public int TotalFilled { get; private set; }
        public int TotalRejected { get; private set; }
        public int TotalCancelled { get; private set; }

        public event Action<string> OnLog;
        public event Action<ManagedOrder> OnOrderUpdated;
        public event Action<Position> OnPositionUpdated;

        public OrderManager(KiwoomConnector connector, string accountNo)
        {
            _connector = connector;
            _accountNo = accountNo;
            _throttler = new OrderThrottler(4);
            _throttler.OnLog += msg => OnLog?.Invoke(msg);

            // 체결/잔고 이벤트 연결
            _connector.OnReceiveChejanData += OnChejanData;
            _connector.OnReceiveMsg += OnReceiveMsg;
        }

        // ── 주문 제출 ──

        public async Task<ManagedOrder> SubmitOrderAsync(
            string code, int orderType, int qty, int price, string hogaGb = "00", string orgOrderNo = "")
        {
            string clientId = $"ORD-{Interlocked.Increment(ref _clientOrderSeq):D6}";
            string scrNo = (_screenSeq++ % 200 + 5000).ToString();
            string name = _connector.GetMasterCodeName(code);

            var order = new ManagedOrder
            {
                ClientOrderId = clientId,
                Code = code,
                Name = name,
                OrderType = orderType,
                Qty = qty,
                Price = price,
                HogaGb = hogaGb,
                OrgOrderNo = orgOrderNo,
                ScreenNo = scrNo,
                AccountNo = _accountNo,
                Status = OrderStatus.Queued,
                RemainingQty = qty,
                QueuedTime = DateTime.Now
            };

            _ordersByClientId[clientId] = order;
            TotalOrders++;

            OnLog?.Invoke($"[주문] {clientId} 큐등록: {order.OrderTypeStr} {code} {name} {qty}주 @{price:N0} ({hogaGb})");

            // 쓰로틀링
            bool slotOk = await _throttler.WaitForSlotAsync();
            if (!slotOk)
            {
                order.Status = OrderStatus.Failed;
                order.RejectReason = "쓰로틀 타임아웃";
                order.CompletedTime = DateTime.Now;
                TotalRejected++;
                OnLog?.Invoke($"[주문] {clientId} 실패: 쓰로틀 타임아웃");
                OnOrderUpdated?.Invoke(order);
                return order;
            }

            // SendOrder 호출
            order.SubmittedTime = DateTime.Now;
            order.Status = OrderStatus.Submitted;

            int ret = _connector.SendOrder(
                clientId, scrNo, _accountNo,
                orderType, code, qty, price, hogaGb, orgOrderNo);

            OnLog?.Invoke($"[주문] {clientId} SendOrder ret={ret}");

            if (ret != 0)
            {
                order.Status = OrderStatus.Failed;
                order.RejectReason = $"SendOrder 실패 (ret={ret})";
                order.CompletedTime = DateTime.Now;
                TotalRejected++;
                OnLog?.Invoke($"[주문] {clientId} 실패: ret={ret}");
            }

            OnOrderUpdated?.Invoke(order);
            return order;
        }

        // ── 정정 주문 ──

        public async Task<ManagedOrder> ModifyOrderAsync(string kiwoomOrderNo, int newQty, int newPrice)
        {
            if (!_ordersByKiwoomNo.TryGetValue(kiwoomOrderNo, out var origOrder))
            {
                OnLog?.Invoke($"[정정] 원주문 {kiwoomOrderNo} 없음");
                return null;
            }

            int modifyType = origOrder.OrderType == 1 ? 5 : 6;  // 매수→매수정정, 매도→매도정정
            return await SubmitOrderAsync(origOrder.Code, modifyType, newQty, newPrice, "00", kiwoomOrderNo);
        }

        // ── 취소 주문 ──

        public async Task<ManagedOrder> CancelOrderAsync(string kiwoomOrderNo)
        {
            if (!_ordersByKiwoomNo.TryGetValue(kiwoomOrderNo, out var origOrder))
            {
                OnLog?.Invoke($"[취소] 원주문 {kiwoomOrderNo} 없음");
                return null;
            }

            int cancelType = origOrder.OrderType == 1 ? 3 : 4;  // 매수→매수취소, 매도→매도취소
            return await SubmitOrderAsync(origOrder.Code, cancelType, origOrder.RemainingQty, 0, "00", kiwoomOrderNo);
        }

        // ── OnReceiveChejanData 이벤트 처리 ──

        private void OnChejanData(_DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
        {
            string gubun = e.sGubun;

            if (gubun == "0")
                HandleOrderEvent();     // 접수/체결
            else if (gubun == "1")
                HandleBalanceEvent();   // 잔고
        }

        private void HandleOrderEvent()
        {
            string orderNo = GetChejan(9203).Trim();
            string code = GetChejan(9001).Trim().Replace("A", "");
            string name = GetChejan(302).Trim();
            string orderStatus = GetChejan(913).Trim();   // 접수, 확인, 체결
            string orderQtyStr = GetChejan(900).Trim();
            string orderPriceStr = GetChejan(901).Trim();
            string remainQtyStr = GetChejan(902).Trim();
            string filledAmtStr = GetChejan(903).Trim();
            string orgOrderNo = GetChejan(904).Trim();
            string orderGubun = GetChejan(905).Trim();    // +매수, -매도, 매수취소 등
            string fillNo = GetChejan(909).Trim();
            string fillPriceStr = GetChejan(910).Trim();
            string fillQtyStr = GetChejan(911).Trim();
            string orderTime = GetChejan(908).Trim();
            string rejectReason = GetChejan(919).Trim();

            OnLog?.Invoke($"[체잔] 주문#{orderNo} [{code}]{name} 상태={orderStatus} 구분={orderGubun} " +
                $"주문={orderQtyStr} 잔량={remainQtyStr} 체결가={fillPriceStr} 체결량={fillQtyStr} 체결번호={fillNo}");

            // 주문장부에서 찾기
            ManagedOrder order = null;
            if (!string.IsNullOrWhiteSpace(orderNo))
            {
                _ordersByKiwoomNo.TryGetValue(orderNo, out order);

                // 아직 등록 안 된 경우 (최초 접수)
                if (order == null)
                {
                    // ClientOrderId 기반으로 매칭 시도 (ScreenNo 또는 최근 Submitted 중 미매칭)
                    foreach (var kv in _ordersByClientId)
                    {
                        if (kv.Value.KiwoomOrderNo == null &&
                            kv.Value.Status == OrderStatus.Submitted &&
                            kv.Value.Code == code)
                        {
                            order = kv.Value;
                            order.KiwoomOrderNo = orderNo;
                            _ordersByKiwoomNo[orderNo] = order;
                            OnLog?.Invoke($"[체잔] {order.ClientOrderId} ↔ 키움주문#{orderNo} 매칭");
                            break;
                        }
                    }
                }
            }

            if (order == null)
            {
                OnLog?.Invoke($"[체잔] 주문#{orderNo} 미매칭 (외부 주문 또는 지연)");
                return;
            }

            // 상태 업데이트
            int remainQty = ParseInt(remainQtyStr);
            int fillPrice = Math.Abs(ParseInt(fillPriceStr));
            int fillQty = ParseInt(fillQtyStr);
            long filledAmt = ParseLong(filledAmtStr);

            order.RemainingQty = remainQty;
            order.FilledAmount = filledAmt;
            order.Name = name;

            if (orderStatus.Contains("접수"))
            {
                order.Status = OrderStatus.Accepted;
                order.AcceptedTime = DateTime.Now;
                OnLog?.Invoke($"[주문] {order.ClientOrderId} 접수완료 (키움#{orderNo})");
            }
            else if (orderStatus.Contains("체결"))
            {
                order.LastFilledPrice = fillPrice;
                order.LastFilledQty = fillQty;
                order.FilledQty += fillQty;
                order.LastFilledTime = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(fillNo))
                {
                    order.Fills.Add(new FillRecord
                    {
                        FillNo = fillNo,
                        Price = fillPrice,
                        Qty = fillQty,
                        Time = DateTime.Now
                    });
                }

                if (remainQty == 0)
                {
                    order.Status = OrderStatus.Filled;
                    order.CompletedTime = DateTime.Now;
                    TotalFilled++;
                    AddHistory(order);
                    OnLog?.Invoke($"[주문] {order.ClientOrderId} ★전량체결★ {order.FilledQty}주 @평균{order.AvgFillPrice:N0} ({order.Latency.TotalMilliseconds:F0}ms)");
                }
                else
                {
                    order.Status = OrderStatus.PartialFilled;
                    OnLog?.Invoke($"[주문] {order.ClientOrderId} 부분체결 {fillQty}주@{fillPrice:N0} (잔량={remainQty})");
                }
            }
            else if (orderStatus.Contains("확인"))
            {
                // 정정/취소 확인
                if (order.OrderType >= 3 && order.OrderType <= 6)
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CompletedTime = DateTime.Now;
                    TotalCancelled++;
                    AddHistory(order);
                    OnLog?.Invoke($"[주문] {order.ClientOrderId} 취소/정정 확인");
                }
            }

            if (!string.IsNullOrWhiteSpace(rejectReason))
            {
                order.RejectReason = rejectReason;
                order.Status = OrderStatus.Rejected;
                order.CompletedTime = DateTime.Now;
                TotalRejected++;
                OnLog?.Invoke($"[주문] {order.ClientOrderId} 거부: {rejectReason}");
            }

            OnOrderUpdated?.Invoke(order);
        }

        private void HandleBalanceEvent()
        {
            string code = GetChejan(9001).Trim().Replace("A", "");
            string name = GetChejan(302).Trim();
            int holdQty = ParseInt(GetChejan(930));
            long buyPrice = Math.Abs(ParseLong(GetChejan(931)));
            long totalBuy = Math.Abs(ParseLong(GetChejan(932)));
            int availQty = ParseInt(GetChejan(933));
            long curPrice = Math.Abs(ParseLong(GetChejan(10)));
            double profitRate = ParseDouble(GetChejan(8019));

            var pos = _positions.AddOrUpdate(code,
                _ => new Position
                {
                    Code = code,
                    Name = name,
                    Qty = holdQty,
                    BuyPrice = buyPrice,
                    TotalBuyAmount = totalBuy,
                    AvailableQty = availQty,
                    CurrentPrice = curPrice,
                    ProfitRate = profitRate,
                    LastUpdated = DateTime.Now
                },
                (_, existing) =>
                {
                    existing.Name = name;
                    existing.Qty = holdQty;
                    existing.BuyPrice = buyPrice;
                    existing.TotalBuyAmount = totalBuy;
                    existing.AvailableQty = availQty;
                    existing.CurrentPrice = curPrice;
                    existing.ProfitRate = profitRate;
                    existing.LastUpdated = DateTime.Now;
                    return existing;
                });

            OnLog?.Invoke($"[잔고] [{code}]{name} 보유={holdQty} 매입단가={buyPrice:N0} 현재가={curPrice:N0} 손익률={profitRate:F2}%");
            OnPositionUpdated?.Invoke(pos);
        }

        // ── OnReceiveMsg ──

        private void OnReceiveMsg(string scrNo, string rqName, string trCode, string msg)
        {
            OnLog?.Invoke($"[MSG] scrNo={scrNo} rqName={rqName} trCode={trCode} msg=\"{msg}\"");
        }

        // ── 조회 ──

        public IReadOnlyDictionary<string, ManagedOrder> GetActiveOrders()
            => _ordersByClientId.Where(kv =>
                kv.Value.Status == OrderStatus.Queued ||
                kv.Value.Status == OrderStatus.Submitted ||
                kv.Value.Status == OrderStatus.Accepted ||
                kv.Value.Status == OrderStatus.PartialFilled)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

        public IReadOnlyDictionary<string, Position> GetPositions()
            => _positions.ToDictionary(kv => kv.Key, kv => kv.Value);

        public IReadOnlyList<ManagedOrder> GetTradeHistory()
        {
            lock (_historyLock) return _tradeHistory.ToList().AsReadOnly();
        }

        public void PrintSummary()
        {
            OnLog?.Invoke("══════════ [OrderManager 요약] ══════════");
            OnLog?.Invoke($"  총 주문: {TotalOrders} | 체결: {TotalFilled} | 취소: {TotalCancelled} | 거부: {TotalRejected}");

            var active = GetActiveOrders();
            OnLog?.Invoke($"  활성 주문: {active.Count}건");
            foreach (var kv in active)
            {
                var o = kv.Value;
                OnLog?.Invoke($"    {o.ClientOrderId} [{o.Code}]{o.Name} {o.OrderTypeStr} {o.Qty}주@{o.Price:N0} 상태={o.Status} 잔량={o.RemainingQty}");
            }

            var positions = GetPositions();
            OnLog?.Invoke($"  보유 포지션: {positions.Count}종목");
            foreach (var kv in positions)
            {
                var p = kv.Value;
                if (p.Qty > 0)
                    OnLog?.Invoke($"    [{p.Code}]{p.Name} {p.Qty}주 매입={p.BuyPrice:N0} 현재={p.CurrentPrice:N0} 손익={p.ProfitRate:F2}%");
            }

            OnLog?.Invoke("════════════════════════════════════════");
        }

        // ── 유틸 ──

        private void AddHistory(ManagedOrder order)
        {
            lock (_historyLock) _tradeHistory.Add(order);
        }

        private string GetChejan(int fid) => _connector.GetChejanData(fid) ?? "";

        private static int ParseInt(string s)
        {
            int.TryParse(s.Trim().Replace(",", ""), out int v);
            return v;
        }

        private static long ParseLong(string s)
        {
            long.TryParse(s.Trim().Replace(",", ""), out long v);
            return v;
        }

        private static double ParseDouble(string s)
        {
            double.TryParse(s.Trim().Replace(",", ""), out double v);
            return v;
        }
    }
}
