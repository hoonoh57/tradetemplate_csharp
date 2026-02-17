namespace Common.Enums
{
    /// <summary>주문 유형 (불변)</summary>
    public enum OrderType
    {
        None = 0,
        Buy = 1,
        Sell = 2,
        ModifyPrice = 3,
        ModifyQty = 4,
        Cancel = 5,
    }

    /// <summary>주문 조건 (불변)</summary>
    public enum OrderCondition
    {
        Normal = 0,         // 보통
        Immediate = 1,      // 즉시체결
        FOK = 2,            // 전량체결
        BestLimit = 3,      // 최유리
        BestMarket = 4,     // 최우선
        Market = 5,         // 시장가
        PreMarket = 6,      // 장전시간외
        PostMarket = 7,     // 장후시간외
    }

    /// <summary>주문 상태 (불변)</summary>
    public enum OrderState
    {
        None = 0,
        Submitted = 1,      // 접수
        Confirmed = 2,      // 확인
        Executed = 3,        // 체결
        PartialFill = 4,    // 부분체결
        Rejected = 5,       // 거부
        Cancelled = 6,      // 취소
        Modified = 7,       // 정정
    }
}