namespace Common.Enums
{
    /// <summary>거래 상태 (불변)</summary>
    public enum TradeState
    {
        None = 0,
        Waiting = 1,        // 대기
        Entered = 2,        // 진입완료
        PartialEntry = 3,   // 부분진입
        Exiting = 4,        // 청산중
        Exited = 5,         // 청산완료
        Error = 99,
    }

    /// <summary>전략 상태 (불변)</summary>
    public enum StrategyState
    {
        Stopped = 0,
        Running = 1,
        Paused = 2,
        Error = 99,
    }
}