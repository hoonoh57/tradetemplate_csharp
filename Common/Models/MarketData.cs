using System;

namespace Common.Models
{
    /// <summary>
    /// 실시간 시세 — 불변(Immutable) 구조체
    /// 초고속 갱신을 위해 struct 사용
    /// </summary>
    public readonly struct MarketData : IEquatable<MarketData>
    {
        public string Code { get; }
        public DateTime Time { get; }
        public int Price { get; }
        public int Open { get; }
        public int High { get; }
        public int Low { get; }
        public int PrevClose { get; }
        public long Volume { get; }
        public long AccVolume { get; }
        public long AccTradingValue { get; }
        public int BidPrice1 { get; }
        public int AskPrice1 { get; }
        public int BidQty1 { get; }
        public int AskQty1 { get; }
        public double StrengthRate { get; }

        public MarketData(
            string code, DateTime time, int price, int open, int high, int low,
            int prevClose, long volume, long accVolume, long accTradingValue,
            int bidPrice1, int askPrice1, int bidQty1, int askQty1, double strengthRate)
        {
            Code = code;
            Time = time;
            Price = price;
            Open = open;
            High = high;
            Low = low;
            PrevClose = prevClose;
            Volume = volume;
            AccVolume = accVolume;
            AccTradingValue = accTradingValue;
            BidPrice1 = bidPrice1;
            AskPrice1 = askPrice1;
            BidQty1 = bidQty1;
            AskQty1 = askQty1;
            StrengthRate = strengthRate;
        }

        /// <summary>등락률 (%)</summary>
        public double ChangeRate => PrevClose != 0 ? (double)(Price - PrevClose) / PrevClose * 100.0 : 0;

        /// <summary>등락폭</summary>
        public int Change => Price - PrevClose;

        /// <summary>상한가 여부 (30%)</summary>
        public bool IsUpperLimit => PrevClose != 0 && ChangeRate >= 29.9;

        /// <summary>하한가 여부 (-30%)</summary>
        public bool IsLowerLimit => PrevClose != 0 && ChangeRate <= -29.9;

        public bool Equals(MarketData other) => Code == other.Code && Time == other.Time;
        public override bool Equals(object obj) => obj is MarketData m && Equals(m);
        public override int GetHashCode() => (Code?.GetHashCode() ?? 0) ^ Time.GetHashCode();
        public override string ToString() =>
            $"{Code} P:{Price} ({ChangeRate:+0.00;-0.00}%) V:{AccVolume}";
    }
}