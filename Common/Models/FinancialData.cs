using System;

namespace Common.Models
{
    /// <summary>
    /// 재무 정보 — 불변(Immutable)
    /// </summary>
    public sealed class FinancialData
    {
        public string Code { get; }
        public int Year { get; }
        public int Quarter { get; }
        public long Revenue { get; }
        public long OperatingProfit { get; }
        public long NetIncome { get; }
        public long TotalAssets { get; }
        public long TotalDebt { get; }
        public long Equity { get; }
        public double EPS { get; }
        public double PER { get; }
        public double PBR { get; }
        public double ROE { get; }
        public double DebtRatio { get; }

        public FinancialData(
            string code, int year, int quarter,
            long revenue, long operatingProfit, long netIncome,
            long totalAssets, long totalDebt, long equity,
            double eps, double per, double pbr, double roe, double debtRatio)
        {
            Code = code ?? "";
            Year = year;
            Quarter = quarter;
            Revenue = revenue;
            OperatingProfit = operatingProfit;
            NetIncome = netIncome;
            TotalAssets = totalAssets;
            TotalDebt = totalDebt;
            Equity = equity;
            EPS = eps;
            PER = per;
            PBR = pbr;
            ROE = roe;
            DebtRatio = debtRatio;
        }

        public override string ToString() =>
            $"{Code} {Year}Q{Quarter} Revenue:{Revenue} PER:{PER:F1} ROE:{ROE:F1}%";
    }
}