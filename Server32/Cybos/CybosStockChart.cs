using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Common.Enums;
using Common.Models;

namespace Server32.Cybos
{
    /// <summary>
    /// CybosPlus용 차트 데이터 조회기 (StockChart) — 불변 패턴 지향
    /// 키움보다 빠르고 안정적인 과거 데이터 로딩을 지원합니다.
    /// </summary>
    public class CybosStockChart
    {
        private readonly CybosConnector _connector;

        public event Action<string> OnLog;

        public CybosStockChart(CybosConnector connector)
        {
            _connector = connector;
        }

        /// <summary>
        /// 지정된 개수만큼 캔들 데이터를 조회합니다.
        /// 1분봉인 경우, 120틱 캔들을 자동으로 추가 조회하여 참여도(TickCount)를 동기화합니다.
        /// </summary>
        public IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, int count)
        {
            if (!_connector.IsConnected) return new List<CandleData>().AsReadOnly();

            string cybosCode = EnsureCybosCode(code);
            dynamic chart = null;

            try
            {
                // [성능 최적화] COM 개체를 현재 스레드에서 생성하여 마샬링 지연(7초 -> 0.1초) 제거
                chart = Activator.CreateInstance(Type.GetTypeFromProgID("CpSysDib.StockChart"));

                // 1. 메인 캔들 조회
                var mainBuffer = RequestRawCandles(chart, cybosCode, type, interval, count);
                if (mainBuffer.Count == 0) return mainBuffer.AsReadOnly();

                // 2. 1분봉인 경우의 참여도(120틱 캔들) 동기화
                if (type == CandleType.Minute && interval == 1)
                {
                    SyncTickIntensity(chart, cybosCode, mainBuffer);
                }

                return mainBuffer.AsReadOnly();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("[Cybos] GetCandles 오류: " + ex.Message);
                return new List<CandleData>().AsReadOnly();
            }
            finally
            {
                // [필독] COM 개체 명시적 해제 (메모리 누수 방지)
                if (chart != null)
                {
                    Marshal.ReleaseComObject(chart);
                }
            }
        }

        private string EnsureCybosCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            if (code.StartsWith("A") || code.Length > 6) return code;
            return "A" + code;
        }

        private List<CandleData> RequestRawCandles(dynamic chart, string code, CandleType type, int interval, int count, DateTime? stopTime = null)
        {
            var result = new List<CandleData>();
            int pageCount = 0;
            int remaining = count;
            bool reachedStop = false;

            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            long totalApiMs = 0;
            long totalParseMs = 0;

            string unitStr = (type == CandleType.Tick) ? $"{interval}틱" : (type == CandleType.Minute ? $"{interval}분" : "일봉");
            OnLog?.Invoke($"[Cybos-Raw] {code} {unitStr} 요청시작: {DateTime.Now:HH:mm:ss.fff}");

            try
            {
                while (remaining > 0 && !reachedStop)
                {
                    _connector.WaitForRateLimit();
                    pageCount++;

                    chart.SetInputValue(0, code); // 종목코드
                    chart.SetInputValue(1, '2'); // '2': 개수 단위 조회
                    chart.SetInputValue(4, remaining); // 요청 개수
                    chart.SetInputValue(5, new int[] { 0, 1, 2, 3, 4, 5, 8, 9 }); // 0:날짜, 1:시간, 2:시, 3:고, 4:저, 5:종, 8:거량, 9:거금
                    chart.SetInputValue(6, ToChartGubun(type)); // D, m, T ...
                    chart.SetInputValue(9, '1'); // '1': 수정주가 사용

                    if (type == CandleType.Minute || type == CandleType.Tick)
                        chart.SetInputValue(7, interval);

                    var swApi = System.Diagnostics.Stopwatch.StartNew();
                    chart.BlockRequest();
                    swApi.Stop();
                    totalApiMs += swApi.ElapsedMilliseconds;

                    int received = (int)chart.GetHeaderValue(3);
                    if (received <= 0)
                    {
                        string msg = chart.GetDibMsg1();
                        if (pageCount == 1) OnLog?.Invoke($"[Cybos] 데이터 없음 ({code}): {msg}");
                        break;
                    }

                    var swParse = System.Diagnostics.Stopwatch.StartNew();
                    for (int i = 0; i < received; i++)
                    {
                        var candle = ParseRow(chart, code, type, i);
                        
                        if (stopTime.HasValue && candle.DateTime < stopTime.Value)
                        {
                            reachedStop = true;
                            break;
                        }

                        result.Add(candle);
                        if (result.Count >= count) break;
                    }
                    swParse.Stop();
                    totalParseMs += swParse.ElapsedMilliseconds;

                    if (reachedStop || result.Count >= count || (int)chart.Continue == 0) break;
                    remaining = count - result.Count;
                }

                swTotal.Stop();
                OnLog?.Invoke($"[Cybos-Raw] {code} {unitStr} 로드완료: {DateTime.Now:HH:mm:ss.fff} (총 {swTotal.ElapsedMilliseconds}ms | API: {totalApiMs}ms, Parse: {totalParseMs}ms, Paging: {pageCount})");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Cybos] RequestRawCandles 오류 ({code}): " + ex.Message);
            }

            return result;
        }

        private void SyncTickIntensity(dynamic chart, string cybosCode, List<CandleData> minList)
        {
            try
            {
                // 분봉 리스트 중 가장 오래된 봉의 시간을 기준으로 틱 데이터 조회 범위를 한정
                DateTime oldestTime = minList[minList.Count - 1].DateTime;
                
                // 지표 계산(이동평균 등)을 위해 oldestTime보다 약간 더 이전까지 데이터를 가져옴 (예: 50틱 캔들 여유)
                // 하지만 스캔 범위는 분봉의 최하단 시간까지만 동기화하면 되므로 무분별한 6배 증액은 삭제
                int maxRequest = 5000; // 절대 상한선
                var tickList = RequestRawCandles(chart, cybosCode, CandleType.Tick, 120, maxRequest, oldestTime.AddMinutes(-5));

                if (tickList.Count == 0)
                {
                    OnLog?.Invoke($"[Cybos-Sync] {cybosCode} 유효 틱데이터 없음. 동기화 스킵.");
                    return;
                }

                int totalMatched = 0;
                int tickIdx = 0;

                // 최적화된 슬라이딩 윈도우 매칭 (O(N+M))
                for (int i = 0; i < minList.Count; i++)
                {
                    var minBar = minList[i];
                    var nextBarTime = minBar.DateTime.AddMinutes(1);

                    while (tickIdx < tickList.Count && tickList[tickIdx].DateTime >= nextBarTime)
                    {
                        tickIdx++;
                    }

                    int countInMin = 0;
                    int tempIdx = tickIdx;
                    while (tempIdx < tickList.Count && tickList[tempIdx].DateTime >= minBar.DateTime)
                    {
                        countInMin++;
                        tempIdx++;
                    }

                    if (countInMin > 0)
                        totalMatched++;

                    // TickCount 필드 교정 (Raw Ticks 단위로 변환)
                    minList[i] = new CandleData(
                        minBar.Code, minBar.DateTime, minBar.Type,
                        minBar.Open, minBar.High, minBar.Low, minBar.Close,
                        minBar.Volume, minBar.TradingValue, countInMin * 120
                    );
                }

                OnLog?.Invoke($"[Cybos-Sync] {cybosCode} 매칭 완료: {totalMatched}분봉 (TickUnit: 120, TotalTicks: {tickList.Count}, Opt)");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Cybos-Sync] 오류 ({cybosCode}): " + ex.Message);
            }
        }

        private CandleData ParseRow(dynamic chart, string code, CandleType type, int i)
        {
            int date = (int)chart.GetDataValue(0, i);
            int time = (int)chart.GetDataValue(1, i);
            int open = (int)chart.GetDataValue(2, i);
            int high = (int)chart.GetDataValue(3, i);
            int low = (int)chart.GetDataValue(4, i);
            int close = (int)chart.GetDataValue(5, i);
            long volume = Convert.ToInt64(chart.GetDataValue(6, i));
            long tradingValue = Convert.ToInt64(chart.GetDataValue(7, i));

            DateTime dt;
            if (type == CandleType.Daily || type == CandleType.Weekly || type == CandleType.Monthly)
            {
                dt = new DateTime(date / 10000, (date / 100) % 100, date % 100);
            }
            else
            {
                dt = new DateTime(date / 10000, (date / 100) % 100, date % 100, time / 100, time % 100, 0);
            }

            // 코드에서 'A' 제거하여 Kiwoom 호환 형식으로 복원
            string cleanCode = code.StartsWith("A") ? code.Substring(1) : code;

            return new CandleData(
                code: cleanCode, dateTime: dt, type: type,
                open: open, high: high, low: low, close: close,
                volume: volume, tradingValue: tradingValue, tickCount: 0);
        }

        private static char ToChartGubun(CandleType type)
        {
            switch (type)
            {
                case CandleType.Daily: return 'D';
                case CandleType.Weekly: return 'W';
                case CandleType.Monthly: return 'M';
                case CandleType.Minute: return 'm';
                case CandleType.Tick: return 'T';
                default: return 'D';
            }
        }
    }
}
