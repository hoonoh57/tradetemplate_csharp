namespace Common.Enums
{
    /// <summary>캔들 종류 (불변)</summary>
    public enum CandleType
    {
        Tick = 0,
        Second = 1,
        Minute = 2,
        Daily = 3,
        Weekly = 4,
        Monthly = 5,
    }

    /// <summary>캔들 주기 (불변)</summary>
    public enum CandleInterval
    {
        Tick1 = 1,
        Second1 = 1,
        Second3 = 3,
        Second5 = 5,
        Second10 = 10,
        Minute1 = 1,
        Minute3 = 3,
        Minute5 = 5,
        Minute10 = 10,
        Minute15 = 15,
        Minute30 = 30,
        Minute60 = 60,
        Day1 = 1,
        Week1 = 1,
        Month1 = 1,
    }
}