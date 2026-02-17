using System;
using Common.Enums;

namespace Common.Models
{
    /// <summary>
    /// 잔고 정보 — 불변(Immutable)
    /// 실시간 갱신 시 새 인스턴스 생성
    /// </summary>
    public sealed class BalanceInfo
    {
        public string Code { get; }
        public string Name { get; }
        public int Qty { get; }
        public int AvgPrice { get; }
        public int CurrentPrice { get; }
        public long PurchaseAmount { get; }
        public long EvalAmount { get; }
        public long ProfitLoss { get; }
        public double ProfitRate { get; }
        public MarketType Market { get; }
        public DateTime UpdateTime { get; }

        public BalanceInfo(
            string code, string name, int qty, int avgPrice, int currentPrice,
            long purchaseAmount, long evalAmount, long profitLoss, double profitRate,
            MarketType market, DateTime updateTime)
        {
            Code = code ?? "";
            Name = name ?? "";
            Qty = qty;
            AvgPrice = avgPrice;
            CurrentPrice = currentPrice;
            PurchaseAmount = purchaseAmount;
            EvalAmount = evalAmount;
            ProfitLoss = profitLoss;
            ProfitRate = profitRate;
            Market = market;
            UpdateTime = updateTime;
        }

        public BalanceInfo WithPrice(int newPrice, DateTime updateTime)
        {
            long eval = (long)newPrice * Qty;
            long pl = eval - PurchaseAmount;
            double rate = PurchaseAmount != 0 ? (double)pl / PurchaseAmount * 100.0 : 0;
            return new BalanceInfo(Code, Name, Qty, AvgPrice, newPrice,
                PurchaseAmount, eval, pl, rate, Market, updateTime);
        }

        public override string ToString() =>
            $"[{Code}] {Name} Q:{Qty} P:{CurrentPrice} PL:{ProfitLoss:+#;-#;0} ({ProfitRate:F2}%)";
    }
}