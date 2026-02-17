using System;
using Common.Models;

namespace Common.Interfaces
{
    /// <summary>
    /// 실시간 시세 수신 인터페이스
    /// 10% 변형 가능 영역 — 데이터소스별 구현 교체
    /// </summary>
    public interface IMarketDataReceiver
    {
        event Action<MarketData> OnMarketDataReceived;
        event Action<TradeResult> OnTradeResultReceived;
        event Action<OrderInfo> OnOrderUpdateReceived;

        void Subscribe(string code);
        void Unsubscribe(string code);
        void SubscribeAll();
        void UnsubscribeAll();
    }
}