using System.Collections.Generic;
using Common.Models;
using Common.Enums;

namespace Common.Interfaces
{
    /// <summary>
    /// 종목 정보 제공자 인터페이스
    /// 10% 변형 가능 영역 — 데이터소스별 구현 교체
    /// </summary>
    public interface IStockProvider
    {
        StockInfo GetStock(string code);
        IReadOnlyList<StockInfo> GetAllStocks();
        IReadOnlyList<StockInfo> GetStocksByMarket(MarketType market);
        IReadOnlyList<StockInfo> GetStocksBySector(string sectorCode);
        bool IsValid(string code);
    }
}