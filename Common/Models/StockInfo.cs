using System;
using Common.Enums;

namespace Common.Models
{
    /// <summary>
    /// 종목 정보 — 불변(Immutable) 구조체
    /// 생성 후 변경 불가. 변경 시 새 인스턴스를 생성.
    /// </summary>
    public sealed class StockInfo
    {
        public string Code { get; }
        public string Name { get; }
        public MarketType Market { get; }
        public int FaceValue { get; }
        public long ListedShares { get; }
        public long MarketCap { get; }
        public string SectorCode { get; }
        public string SectorName { get; }
        public int PriceUnit { get; }
        public bool IsSuspended { get; }
        public bool IsAdminIssue { get; }
        public DateTime ListedDate { get; }

        public StockInfo(
            string code, string name, MarketType market,
            int faceValue, long listedShares, long marketCap,
            string sectorCode, string sectorName, int priceUnit,
            bool isSuspended, bool isAdminIssue, DateTime listedDate)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Market = market;
            FaceValue = faceValue;
            ListedShares = listedShares;
            MarketCap = marketCap;
            SectorCode = sectorCode ?? "";
            SectorName = sectorName ?? "";
            PriceUnit = priceUnit;
            IsSuspended = isSuspended;
            IsAdminIssue = isAdminIssue;
            ListedDate = listedDate;
        }

        /// <summary>변경이 필요한 필드만 새 값으로 복사 생성</summary>
        public StockInfo With(
            string code = null, string name = null, MarketType? market = null,
            int? faceValue = null, long? listedShares = null, long? marketCap = null,
            string sectorCode = null, string sectorName = null, int? priceUnit = null,
            bool? isSuspended = null, bool? isAdminIssue = null, DateTime? listedDate = null)
        {
            return new StockInfo(
                code ?? Code, name ?? Name, market ?? Market,
                faceValue ?? FaceValue, listedShares ?? ListedShares, marketCap ?? MarketCap,
                sectorCode ?? SectorCode, sectorName ?? SectorName, priceUnit ?? PriceUnit,
                isSuspended ?? IsSuspended, isAdminIssue ?? IsAdminIssue, listedDate ?? ListedDate);
        }

        public override string ToString() => $"[{Code}] {Name} ({Market})";
        public override int GetHashCode() => Code?.GetHashCode() ?? 0;
        public override bool Equals(object obj) => obj is StockInfo s && s.Code == Code;
    }
}