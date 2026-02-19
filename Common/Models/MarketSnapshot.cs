using System;
using System.Collections.Generic;

namespace Common.Models
{
    /// <summary>
    /// 특정 시점의 시장 상태 스냅샷.
    /// 가격 정보와 모든 지표(Indicators) 값을 불변 객체로 캡처함.
    /// </summary>
    public sealed class MarketSnapshot
    {
        public DateTime Time { get; }
        public double Price { get; }
        public double Open { get; }
        public double High { get; }
        public double Low { get; }
        public double Volume { get; }
        
        // 지표 이름 -> 값 매핑 (예: "SMA20" -> 5550.0, "SuperTrend" -> 5400.0)
        public Dictionary<string, double> Indicators { get; }

        public MarketSnapshot(DateTime time, double price, double o, double h, double l, double v, Dictionary<string, double> indicators)
        {
            Time = time;
            Price = price;
            Open = o;
            High = h;
            Low = l;
            Volume = v;
            Indicators = indicators ?? new Dictionary<string, double>();
        }

        public void SetIndicator(string name, double value)
        {
            Indicators[name] = value;
        }

        public double GetValue(string name)
        {
            if (name == "Price" || name == "Close") return Price;
            if (name == "Open") return Open;
            if (name == "High") return High;
            if (name == "Low") return Low;
            if (name == "Volume") return Volume;
            
            if (Indicators.TryGetValue(name, out double val)) return val;
            return double.NaN;
        }
    }
}
