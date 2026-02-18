using System;
using System.Collections.Generic;
using Common.Enums;
using Common.Interfaces;
using Common.Models;

namespace Server32.Cybos
{
    /// <summary>
    /// CybosPlus 차트 배치 다운로드 — Skills §3.2 StockChart 준수
    /// COM: CpSysDib.StockChart
    /// 제한: 1분봉 2년, 5분봉 5년, 틱 20일, 일봉 무제한
    /// 조회제한: 15초에 60회
    /// </summary>
    public sealed class CybosStockChart : ICandleProvider
    {
        private readonly CybosConnector _conn;

        public CybosStockChart(CybosConnector conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        }

        /// <summary>개수 기준 캔들 조회</summary>
        public IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, int count)
        {
            _conn.WaitForRateLimit();

            dynamic chart = Activator.CreateInstance(Type.GetTypeFromProgID("CpSysDib.StockChart"));
            chart.SetInputValue(0, "A" + code);              // 종목코드
            chart.SetInputValue(1, (short)2);                 // 개수 기준
            chart.SetInputValue(4, count);                    // 요청 개수
            chart.SetInputValue(5, new short[] { 0, 1, 2, 3, 4, 5, 8 }); // 날짜,시간,OHLCV
            chart.SetInputValue(6, ToChartGubun(type));       // 차트구분
            if (type == CandleType.Minute && interval > 0)
                chart.SetInputValue(7, interval);             // 분봉 주기
            chart.SetInputValue(9, (short)1);                 // 수정주가

            chart.BlockRequest();

            return ParseChartResult(chart);
        }

        /// <summary>기간 기준 캔들 조회</summary>
        public IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, DateTime from, DateTime to)
        {
            _conn.WaitForRateLimit();

            dynamic chart = Activator.CreateInstance(Type.GetTypeFromProgID("CpSysDib.StockChart"));
            chart.SetInputValue(0, "A" + code);
            chart.SetInputValue(1, (short)1);                 // 기간 기준
            chart.SetInputValue(2, to.ToString("yyyyMMdd"));
            chart.SetInputValue(3, from.ToString("yyyyMMdd"));
            chart.SetInputValue(5, new short[] { 0, 1, 2, 3, 4, 5, 8 });
            chart.SetInputValue(6, ToChartGubun(type));
            if (type == CandleType.Minute && interval > 0)
                chart.SetInputValue(7, interval);
            chart.SetInputValue(9, (short)1);

            chart.BlockRequest();

            return ParseChartResult(chart);
        }

        /// <summary>연속 조회로 대량 데이터 수집</summary>
        public IReadOnlyList<CandleData> GetCandlesBatch(string code, CandleType type, int interval, int totalCount)
        {
            var allCandles = new List<CandleData>();
            int remaining = totalCount;
            int batchSize = 2000; // Cybos 최대 수신 개수

            while (remaining > 0)
            {
                int requestCount = Math.Min(remaining, batchSize);
                var batch = GetCandles(code, type, interval, requestCount);

                if (batch.Count == 0) break;

                allCandles.AddRange(batch);
                remaining -= batch.Count;

                if (batch.Count < requestCount) break; // 더 이상 데이터 없음
            }

            return allCandles.AsReadOnly();
        }

        public void Subscribe(string code, CandleType type, int interval)
        {
            // 실시간 차트는 DsCbo1.StockCur 사용 (CybosRealtimeReceiver에서 처리)
        }

        public void Unsubscribe(string code, CandleType type, int interval)
        {
        }

        // ── 내부 메서드 ──

        private static IReadOnlyList<CandleData> ParseChartResult(dynamic chart)
        {
            int received = 0;
            try { received = (int)chart.GetHeaderValue(3); }
            catch { return new List<CandleData>().AsReadOnly(); }

            var list = new List<CandleData>(received);
            for (int i = 0; i < received; i++)
            {
                try
                {
                    int dateVal = Convert.ToInt32(chart.GetDataValue(0, i));  // YYYYMMDD
                    int timeVal = 0;
                    try { timeVal = Convert.ToInt32(chart.GetDataValue(1, i)); } catch { } // HHMM

                    DateTime dt = ParseCybosDateTime(dateVal, timeVal);
                    int open   = Convert.ToInt32(chart.GetDataValue(2, i));
                    int high   = Convert.ToInt32(chart.GetDataValue(3, i));
                    int low    = Convert.ToInt32(chart.GetDataValue(4, i));
                    int close  = Convert.ToInt32(chart.GetDataValue(5, i));
                    long volume = Convert.ToInt64(chart.GetDataValue(6, i));

                    list.Add(new CandleData(dt, open, high, low, close, volume));
                }
                catch
                {
                    // 개별 행 파싱 실패 → 건너뜀
                }
            }
            return list.AsReadOnly();
        }

        private static char ToChartGubun(CandleType t)
        {
            switch (t)
            {
                case CandleType.Day:    return 'D';
                case CandleType.Week:   return 'W';
                case CandleType.Month:  return 'M';
                case CandleType.Minute: return 'm';
                case CandleType.Tick:   return 'T';
                default: return 'D';
            }
        }

        private static DateTime ParseCybosDateTime(int date, int time)
        {
            if (date <= 0) return DateTime.MinValue;
            int y = date / 10000;
            int m = (date % 10000) / 100;
            int d = date % 100;
            int h = time / 100;
            int mm = time % 100;

            if (y < 1900 || y > 2100) return DateTime.MinValue;
            if (m < 1 || m > 12) m = 1;
            if (d < 1 || d > 31) d = 1;
            if (h < 0 || h > 23) h = 0;
            if (mm < 0 || mm > 59) mm = 0;

            try { return new DateTime(y, m, d, h, mm, 0); }
            catch { return DateTime.MinValue; }
        }
    }
}