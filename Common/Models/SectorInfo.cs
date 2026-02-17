using System;
using System.Collections.Generic;

namespace Common.Models
{
    /// <summary>
    /// 섹터/업종 정보 — 불변(Immutable)
    /// </summary>
    public sealed class SectorInfo
    {
        public string SectorCode { get; }
        public string SectorName { get; }
        public double ChangeRate { get; }
        public long TradingValue { get; }
        public IReadOnlyList<string> LeadingCodes { get; }
        public DateTime UpdateTime { get; }

        public SectorInfo(
            string sectorCode, string sectorName, double changeRate,
            long tradingValue, IReadOnlyList<string> leadingCodes, DateTime updateTime)
        {
            SectorCode = sectorCode ?? "";
            SectorName = sectorName ?? "";
            ChangeRate = changeRate;
            TradingValue = tradingValue;
            LeadingCodes = leadingCodes ?? Array.Empty<string>();
            UpdateTime = updateTime;
        }

        public override string ToString() =>
            $"[{SectorCode}] {SectorName} ({ChangeRate:+0.00;-0.00}%) {LeadingCodes.Count} leaders";
    }
}