using System;
using Common.Enums;

namespace Common.Modules
{
    /// <summary>
    /// 가격 관련 유틸리티 — 정적(Static) 모듈
    /// </summary>
    public static class PriceHelper
    {
        /// <summary>호가 단위 계산 (코스피 기준)</summary>
        public static int GetTickSize(int price, MarketType market)
        {
            if (market == MarketType.Kosdaq || market == MarketType.KosdaqETF)
                return GetKosdaqTickSize(price);
            return GetKospiTickSize(price);
        }

        private static int GetKospiTickSize(int price)
        {
            if (price < 2000) return 1;
            if (price < 5000) return 5;
            if (price < 20000) return 10;
            if (price < 50000) return 50;
            if (price < 200000) return 100;
            if (price < 500000) return 500;
            return 1000;
        }

        private static int GetKosdaqTickSize(int price)
        {
            if (price < 2000) return 1;
            if (price < 5000) return 5;
            if (price < 20000) return 10;
            if (price < 50000) return 50;
            if (price < 200000) return 100;
            if (price < 500000) return 500;
            return 1000;
        }

        /// <summary>가격을 호가 단위로 정렬 (내림)</summary>
        public static int RoundDownToTick(int price, MarketType market)
        {
            int tick = GetTickSize(price, market);
            return (price / tick) * tick;
        }

        /// <summary>가격을 호가 단위로 정렬 (올림)</summary>
        public static int RoundUpToTick(int price, MarketType market)
        {
            int tick = GetTickSize(price, market);
            return ((price + tick - 1) / tick) * tick;
        }

        /// <summary>상한가 계산 (전일종가 기준 +30%)</summary>
        public static int CalcUpperLimit(int prevClose, MarketType market)
        {
            int limit = (int)(prevClose * 1.3);
            return RoundDownToTick(limit, market);
        }

        /// <summary>하한가 계산 (전일종가 기준 -30%)</summary>
        public static int CalcLowerLimit(int prevClose, MarketType market)
        {
            int limit = (int)(prevClose * 0.7);
            return RoundUpToTick(limit, market);
        }

        /// <summary>수익률 계산</summary>
        public static double CalcProfitRate(int entryPrice, int currentPrice)
        {
            if (entryPrice == 0) return 0;
            return (double)(currentPrice - entryPrice) / entryPrice * 100.0;
        }
    }
}