using System;
using Common.Enums;

namespace Common.Models
{
    /// <summary>
    /// 체결 결과 — 불변(Immutable)
    /// </summary>
    public sealed class TradeResult
    {
        public string TradeNo { get; }
        public string OrderNo { get; }
        public string Code { get; }
        public string Name { get; }
        public OrderType Type { get; }
        public int Price { get; }
        public int Qty { get; }
        public long Amount { get; }
        public DateTime TradeTime { get; }

        public TradeResult(
            string tradeNo, string orderNo, string code, string name,
            OrderType type, int price, int qty, long amount, DateTime tradeTime)
        {
            TradeNo = tradeNo ?? "";
            OrderNo = orderNo ?? "";
            Code = code ?? "";
            Name = name ?? "";
            Type = type;
            Price = price;
            Qty = qty;
            Amount = amount;
            TradeTime = tradeTime;
        }

        public override string ToString() =>
            $"[{TradeNo}] {Code} {Type} P:{Price} Q:{Qty} T:{TradeTime:HH:mm:ss}";
    }
}