using System;
using System.Collections.Concurrent;
using Common.Interfaces;
using Common.Models;

namespace Server32.Cybos
{
    /// <summary>
    /// CybosPlus 실시간 시세 수신 — Skills §3.3 DsCbo1.StockCur 준수
    /// </summary>
    public sealed class CybosRealtimeReceiver : IMarketDataReceiver
    {
        private readonly ConcurrentDictionary<string, dynamic> _subscribers
            = new ConcurrentDictionary<string, dynamic>();

        public event Action<MarketData> OnMarketDataReceived;
        public event Action<TradeResult> OnTradeResultReceived;
        public event Action<OrderInfo> OnOrderUpdateReceived;

        public void Subscribe(string code)
        {
            if (_subscribers.ContainsKey(code)) return;

            try
            {
                dynamic stockCur = Activator.CreateInstance(
                    Type.GetTypeFromProgID("DsCbo1.StockCur"));
                stockCur.SetInputValue(0, "A" + code);
                stockCur.Subscribe();
                _subscribers[code] = stockCur;
            }
            catch (Exception)
            {
                // COM 초기화 실패 시 무시
            }
        }

        public void Unsubscribe(string code)
        {
            if (_subscribers.TryRemove(code, out dynamic sub))
            {
                try { sub.Unsubscribe(); } catch { }
            }
        }

        public void SubscribeAll()
        {
            // 전 종목 구독은 리소스 문제로 미지원
        }

        public void UnsubscribeAll()
        {
            foreach (var kv in _subscribers)
            {
                try { kv.Value.Unsubscribe(); } catch { }
            }
            _subscribers.Clear();
        }

        /// <summary>외부에서 Cybos Received 이벤트 발생 시 호출</summary>
        public void ProcessStockCurData(string code, dynamic stockCur)
        {
            try
            {
                var md = new MarketData(
                    stockCode:  code,
                    price:      (int)stockCur.GetHeaderValue(13),
                    change:     (int)stockCur.GetHeaderValue(14),
                    changeRate: 0f,
                    volume:     Convert.ToInt64(stockCur.GetHeaderValue(15)),
                    high:       0,
                    low:        0,
                    open:       0,
                    tradeTime:  DateTime.Now,
                    askPrice:   (int)stockCur.GetHeaderValue(18),
                    bidPrice:   (int)stockCur.GetHeaderValue(19)
                );
                OnMarketDataReceived?.Invoke(md);
            }
            catch { }
        }

        /// <summary>구독 중인 종목 수</summary>
        public int SubscribedCount => _subscribers.Count;
    }
}