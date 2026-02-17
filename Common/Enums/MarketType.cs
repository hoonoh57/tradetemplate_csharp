namespace Common.Enums
{
    /// <summary>시장 구분 (불변)</summary>
    public enum MarketType
    {
        None = 0,
        Kospi = 1,
        Kosdaq = 2,
        KospiETF = 3,
        KosdaqETF = 4,
        Konex = 5,
        Futures = 10,
        Options = 11,
    }
}