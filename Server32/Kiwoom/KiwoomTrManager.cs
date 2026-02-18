using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Enums;
using Common.Models;

namespace Server32.Kiwoom
{
    public class KiwoomTrManager
    {
        private readonly KiwoomConnector _connector;
        private TaskCompletionSource<bool> _trTcs;
        private string _lastTrCode;
        private string _lastRqName;

        // 캔들 버퍼
        private readonly List<CandleData> _candleBuffer = new List<CandleData>();

        // 잔고 조회 결과
        private long _totalBuyAmount;
        private long _totalEvalAmount;
        private long _totalProfitLoss;
        private double _totalProfitRate;
        private readonly List<HoldingItem> _holdings = new List<HoldingItem>();

        // 미체결 조회 결과
        private readonly List<PendingOrder> _pendingOrders = new List<PendingOrder>();

        public event Action<string> OnLog;

        public KiwoomTrManager(KiwoomConnector connector)
        {
            _connector = connector;
            _connector.OnReceiveTrData += OnTrDataReceived;
            OnLog?.Invoke("[TrManager] 이벤트 핸들러 등록 완료");
        }

        // ══════════════════════════════════════════════
        //  계좌평가잔고내역 (opw00018)
        // ══════════════════════════════════════════════

        public async Task QueryAccountBalanceAsync(string accountNo, int timeoutMs = 10000)
        {
            _holdings.Clear();
            _totalBuyAmount = 0;
            _totalEvalAmount = 0;
            _totalProfitLoss = 0;
            _totalProfitRate = 0;

            _lastTrCode = "opw00018";
            _lastRqName = "계좌평가잔고내역요청";

            _connector.SetInputValue("계좌번호", accountNo);
            _connector.SetInputValue("비밀번호", "");
            _connector.SetInputValue("비밀번호입력매체구분", "00");
            _connector.SetInputValue("조회구분", "1");
            _connector.SetInputValue("거래소구분", "");

            _trTcs = new TaskCompletionSource<bool>();
            int ret = _connector.CommRqData(_lastRqName, _lastTrCode, 0, "3001");
            OnLog?.Invoke($"[TR] opw00018 요청 (ret={ret}), 대기 rqName=\"{_lastRqName}\"");

            if (ret != 0)
            {
                OnLog?.Invoke($"[TR] opw00018 요청 실패 (ret={ret})");
                return;
            }

            var completed = await Task.WhenAny(_trTcs.Task, Task.Delay(timeoutMs));
            if (completed != _trTcs.Task)
            {
                OnLog?.Invoke("[TR] opw00018 타임아웃 — 이벤트 미수신");
                return;
            }

            // 결과 로깅
            OnLog?.Invoke("════════════════════════════════════════");
            OnLog?.Invoke($"  [총잔고] 매입금액: {_totalBuyAmount:N0}원");
            OnLog?.Invoke($"  [총잔고] 평가금액: {_totalEvalAmount:N0}원");
            OnLog?.Invoke($"  [총잔고] 손익합계: {_totalProfitLoss:N0}원 ({_totalProfitRate:F2}%)");
            OnLog?.Invoke("────────────────────────────────────────");

            if (_holdings.Count == 0)
            {
                OnLog?.Invoke("  보유종목 없음");
            }
            else
            {
                foreach (var h in _holdings)
                {
                    OnLog?.Invoke($"  [{h.Code}] {h.Name} | {h.Qty}주 | 매입:{h.BuyPrice:N0} | 현재:{h.CurPrice:N0} | 손익:{h.ProfitLoss:N0}원 ({h.ProfitRate:F2}%)");
                }
            }
            OnLog?.Invoke("════════════════════════════════════════");
        }

        // ══════════════════════════════════════════════
        //  미체결요청 (opt10075)
        // ══════════════════════════════════════════════

        public async Task QueryPendingOrdersAsync(string accountNo, int timeoutMs = 10000)
        {
            _pendingOrders.Clear();

            _lastTrCode = "opt10075";
            _lastRqName = "미체결요청";

            _connector.SetInputValue("계좌번호", accountNo);
            _connector.SetInputValue("체결구분", "1");
            _connector.SetInputValue("매매구분", "0");

            _trTcs = new TaskCompletionSource<bool>();
            int ret = _connector.CommRqData(_lastRqName, _lastTrCode, 0, "3002");
            OnLog?.Invoke($"[TR] opt10075 요청 (ret={ret}), 대기 rqName=\"{_lastRqName}\"");

            if (ret != 0)
            {
                OnLog?.Invoke($"[TR] opt10075 요청 실패 (ret={ret})");
                return;
            }

            var completed = await Task.WhenAny(_trTcs.Task, Task.Delay(timeoutMs));
            if (completed != _trTcs.Task)
            {
                OnLog?.Invoke("[TR] opt10075 타임아웃 — 이벤트 미수신");
                return;
            }

            // 결과 로깅
            OnLog?.Invoke("═══════════ [미체결 현황] ═══════════");
            if (_pendingOrders.Count == 0)
            {
                OnLog?.Invoke("  미체결 주문 없음");
            }
            else
            {
                foreach (var p in _pendingOrders)
                {
                    OnLog?.Invoke($"  주문번호:{p.OrderNo} | [{p.Code}] {p.Name} | {p.OrderType} | {p.Qty}주 @ {p.Price:N0}원 | 미체결:{p.RemainQty}주");
                }
            }
            OnLog?.Invoke("════════════════════════════════════════");
        }

        // ══════════════════════════════════════════════
        //  캔들 조회 (opt10081 / opt10080)
        // ══════════════════════════════════════════════

        public async Task<IReadOnlyList<CandleData>> RequestCandlesAsync(
            string code, CandleType type, int count, int timeoutMs = 10000)
        {
            _candleBuffer.Clear();
            _lastTrCode = type == CandleType.Daily ? "opt10081" : "opt10080";
            _lastRqName = "캔들조회";

            _connector.SetInputValue("종목코드", code);
            _connector.SetInputValue("기준일자", DateTime.Now.ToString("yyyyMMdd"));
            _connector.SetInputValue("수정주가구분", "1");

            _trTcs = new TaskCompletionSource<bool>();
            _connector.CommRqData(_lastRqName, _lastTrCode, 0, "2000");

            var timeout = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(_trTcs.Task, timeout);
            if (completed == timeout) return _candleBuffer.AsReadOnly();

            return _candleBuffer.AsReadOnly();
        }

        // ══════════════════════════════════════════════
        //  TR 데이터 수신 핸들러
        // ══════════════════════════════════════════════

        private void OnTrDataReceived(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            // ★ 디버그: 모든 TR 수신 로깅
            OnLog?.Invoke($"[TR수신] sTrCode=\"{e.sTrCode}\", sRQName=\"{e.sRQName}\", sScrNo=\"{e.sScrNo}\"");

            if (e.sRQName != _lastRqName)
            {
                OnLog?.Invoke($"[TR수신] rqName 불일치: 기대=\"{_lastRqName}\", 수신=\"{e.sRQName}\" → 무시");
                return;
            }

            try
            {
                if (e.sTrCode.Equals("opw00018", StringComparison.OrdinalIgnoreCase))
                    ParseAccountBalance(e);
                else if (e.sTrCode.Equals("opt10075", StringComparison.OrdinalIgnoreCase))
                    ParsePendingOrders(e);
                else if (e.sTrCode == "opt10081" || e.sTrCode == "opt10080")
                    ParseCandles(e);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[TR] 파싱 오류 ({e.sTrCode}): {ex.Message}");
            }

            _trTcs?.TrySetResult(true);
        }

        private void ParseAccountBalance(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            // 싱글 데이터 (총잔고)
            string buyAmt = _connector.GetCommData(e.sTrCode, e.sRQName, 0, "총매입금액").Trim();
            string evalAmt = _connector.GetCommData(e.sTrCode, e.sRQName, 0, "총평가금액").Trim();
            string plAmt = _connector.GetCommData(e.sTrCode, e.sRQName, 0, "총평가손익금액").Trim();
            string plRate = _connector.GetCommData(e.sTrCode, e.sRQName, 0, "총수익률(%)").Trim();

            OnLog?.Invoke($"[TR파싱] 원본값 — 매입:\"{buyAmt}\" 평가:\"{evalAmt}\" 손익:\"{plAmt}\" 수익률:\"{plRate}\"");

            long.TryParse(buyAmt, out _totalBuyAmount);
            long.TryParse(evalAmt, out _totalEvalAmount);
            long.TryParse(plAmt, out _totalProfitLoss);
            double.TryParse(plRate, out _totalProfitRate);

            // 실서버 수익률 변환 (소수점 없이 올 경우 /100)
            if (!_connector.IsSimulation && Math.Abs(_totalProfitRate) > 100)
                _totalProfitRate /= 100.0;

            // 멀티 데이터 (보유종목)
            int cnt = _connector.GetRepeatCnt(e.sTrCode, e.sRQName);
            OnLog?.Invoke($"[TR파싱] 보유종목 수: {cnt}");

            for (int i = 0; i < cnt; i++)
            {
                string code = _connector.GetCommData(e.sTrCode, e.sRQName, i, "종목번호").Trim().Replace("A", "");
                string name = _connector.GetCommData(e.sTrCode, e.sRQName, i, "종목명").Trim();
                string qty = _connector.GetCommData(e.sTrCode, e.sRQName, i, "보유수량").Trim();
                string buyPrice = _connector.GetCommData(e.sTrCode, e.sRQName, i, "매입가").Trim();
                string curPrice = _connector.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim();
                string pl = _connector.GetCommData(e.sTrCode, e.sRQName, i, "평가손익").Trim();
                string rate = _connector.GetCommData(e.sTrCode, e.sRQName, i, "수익률(%)").Trim();

                _holdings.Add(new HoldingItem
                {
                    Code = code,
                    Name = name,
                    Qty = ParseInt(qty),
                    BuyPrice = Math.Abs(ParseLong(buyPrice)),
                    CurPrice = Math.Abs(ParseLong(curPrice)),
                    ProfitLoss = ParseLong(pl),
                    ProfitRate = ParseDouble(rate)
                });
            }
        }

        private void ParsePendingOrders(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            int cnt = _connector.GetRepeatCnt(e.sTrCode, e.sRQName);
            OnLog?.Invoke($"[TR파싱] 미체결 건수: {cnt}");

            for (int i = 0; i < cnt; i++)
            {
                string orderNo = _connector.GetCommData(e.sTrCode, e.sRQName, i, "주문번호").Trim();
                string code = _connector.GetCommData(e.sTrCode, e.sRQName, i, "종목코드").Trim();
                string name = _connector.GetCommData(e.sTrCode, e.sRQName, i, "종목명").Trim();
                string orderType = _connector.GetCommData(e.sTrCode, e.sRQName, i, "매매구분").Trim();
                string qty = _connector.GetCommData(e.sTrCode, e.sRQName, i, "주문수량").Trim();
                string price = _connector.GetCommData(e.sTrCode, e.sRQName, i, "주문가격").Trim();
                string remainQty = _connector.GetCommData(e.sTrCode, e.sRQName, i, "미체결수량").Trim();

                _pendingOrders.Add(new PendingOrder
                {
                    OrderNo = orderNo,
                    Code = code,
                    Name = name,
                    OrderType = orderType,
                    Qty = ParseInt(qty),
                    Price = Math.Abs(ParseLong(price)),
                    RemainQty = ParseInt(remainQty)
                });
            }
        }

        private void ParseCandles(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            int cnt = _connector.GetRepeatCnt(e.sTrCode, e.sRQName);
            for (int i = 0; i < cnt; i++)
            {
                string date = _connector.GetCommData(e.sTrCode, e.sRQName, i, "일자").Trim();
                string open = _connector.GetCommData(e.sTrCode, e.sRQName, i, "시가").Trim();
                string high = _connector.GetCommData(e.sTrCode, e.sRQName, i, "고가").Trim();
                string low = _connector.GetCommData(e.sTrCode, e.sRQName, i, "저가").Trim();
                string close = _connector.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim();
                string vol = _connector.GetCommData(e.sTrCode, e.sRQName, i, "거래량").Trim();

                DateTime dt = DateTime.ParseExact(date, "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture);

                _candleBuffer.Add(new CandleData(
                    code: "", dateTime: dt, type: CandleType.Daily,
                    open: Math.Abs(int.Parse(open)),
                    high: Math.Abs(int.Parse(high)),
                    low: Math.Abs(int.Parse(low)),
                    close: Math.Abs(int.Parse(close)),
                    volume: long.Parse(vol),
                    tradingValue: 0
                ));
            }
        }

        // ── 유틸 ──

        private static int ParseInt(string s)
        {
            int.TryParse(s.Replace(",", ""), out int v);
            return v;
        }

        private static long ParseLong(string s)
        {
            long.TryParse(s.Replace(",", ""), out long v);
            return v;
        }

        private static double ParseDouble(string s)
        {
            double.TryParse(s.Replace(",", ""), out double v);
            return v;
        }
    }

    // ══════════════════════════════════════════════
    //  데이터 클래스
    // ══════════════════════════════════════════════

    public class HoldingItem
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public long BuyPrice { get; set; }
        public long CurPrice { get; set; }
        public long ProfitLoss { get; set; }
        public double ProfitRate { get; set; }
    }

    public class PendingOrder
    {
        public string OrderNo { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string OrderType { get; set; }
        public int Qty { get; set; }
        public long Price { get; set; }
        public int RemainQty { get; set; }
    }
}
