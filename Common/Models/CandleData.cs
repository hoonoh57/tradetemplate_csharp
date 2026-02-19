using System;
using Common.Enums;

namespace Common.Models
{
    /// <summary>
    /// 캔들 데이터 — 불변(Immutable) 구조체
    /// 고속 처리를 위해 struct 사용. 복사 비용 최소화.
    /// </summary>
    public readonly struct CandleData : IEquatable<CandleData>
    {
        public string Code { get; }
        public DateTime DateTime { get; }
        public CandleType Type { get; }
        public int Open { get; }
        public int High { get; }
        public int Low { get; }
        public int Close { get; }
        public long Volume { get; }
        public long TradingValue { get; }
        public int TickCount { get; } // [추가] 해당 봉 내의 틱 횟수 (또는 합산된 틱캔들 수)

        public CandleData(
            string code, DateTime dateTime, CandleType type,
            int open, int high, int low, int close,
            long volume, long tradingValue, int tickCount = 0)
        {
            Code = code;
            DateTime = dateTime;
            Type = type;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            TradingValue = tradingValue;
            TickCount = tickCount;
        }

        /// <summary>변동률 (%)</summary>
        public double ChangeRate => Open != 0 ? (double)(Close - Open) / Open * 100.0 : 0;

        /// <summary>캔들 몸통 크기</summary>
        public int BodySize => Math.Abs(Close - Open);

        /// <summary>윗꼬리</summary>
        public int UpperShadow => High - Math.Max(Open, Close);

        /// <summary>아랫꼬리</summary>
        public int LowerShadow => Math.Min(Open, Close) - Low;

        /// <summary>양봉 여부</summary>
        public bool IsBullish => Close > Open;

        public bool Equals(CandleData other) =>
            Code == other.Code && DateTime == other.DateTime && Type == other.Type;

        public override bool Equals(object obj) => obj is CandleData c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(Code, DateTime, Type);
        public override string ToString() =>
            $"{Code} {DateTime:yyyy-MM-dd HH:mm} O:{Open} H:{High} L:{Low} C:{Close} V:{Volume}";

        private static class HashCode
        {
            public static int Combine(object a, object b, object c) =>
                (a?.GetHashCode() ?? 0) ^ ((b?.GetHashCode() ?? 0) << 5) ^ ((c?.GetHashCode() ?? 0) << 10);
        }
    }
}