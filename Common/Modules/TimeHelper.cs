using System;

namespace Common.Modules
{
    /// <summary>
    /// 시간 관련 유틸리티 — 정적(Static) 모듈
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>장 시작 시간 (09:00)</summary>
        public static readonly TimeSpan MarketOpen = new TimeSpan(9, 0, 0);

        /// <summary>장 종료 시간 (15:30)</summary>
        public static readonly TimeSpan MarketClose = new TimeSpan(15, 30, 0);

        /// <summary>장전 시간외 시작 (08:30)</summary>
        public static readonly TimeSpan PreMarketOpen = new TimeSpan(8, 30, 0);

        /// <summary>장후 시간외 종료 (18:00)</summary>
        public static readonly TimeSpan PostMarketClose = new TimeSpan(18, 0, 0);

        /// <summary>현재 장 중인지 여부</summary>
        public static bool IsMarketOpen()
        {
            var now = DateTime.Now.TimeOfDay;
            return now >= MarketOpen && now <= MarketClose;
        }

        /// <summary>현재 시간외 시간인지 여부</summary>
        public static bool IsExtendedHours()
        {
            var now = DateTime.Now.TimeOfDay;
            return (now >= PreMarketOpen && now < MarketOpen) ||
                   (now > MarketClose && now <= PostMarketClose);
        }

        /// <summary>오늘이 거래일인지 여부 (주말 제외, 공휴일 미포함)</summary>
        public static bool IsTradingDay()
        {
            var dow = DateTime.Today.DayOfWeek;
            return dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday;
        }

        /// <summary>시간 문자열 → DateTime 변환 (HHMMSS)</summary>
        public static DateTime ParseTime(string timeStr)
        {
            if (timeStr == null || timeStr.Length < 6) return DateTime.MinValue;
            int h = int.Parse(timeStr.Substring(0, 2));
            int m = int.Parse(timeStr.Substring(2, 2));
            int s = int.Parse(timeStr.Substring(4, 2));
            return DateTime.Today.Add(new TimeSpan(h, m, s));
        }

        /// <summary>날짜 문자열 → DateTime 변환 (YYYYMMDD)</summary>
        public static DateTime ParseDate(string dateStr)
        {
            if (dateStr == null || dateStr.Length < 8) return DateTime.MinValue;
            int y = int.Parse(dateStr.Substring(0, 4));
            int m = int.Parse(dateStr.Substring(4, 2));
            int d = int.Parse(dateStr.Substring(6, 2));
            return new DateTime(y, m, d);
        }
    }
}