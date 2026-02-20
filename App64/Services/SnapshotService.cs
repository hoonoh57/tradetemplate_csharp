using System;
using System.Collections.Generic;
using System.Linq;
using App64.Controls;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 차트 데이터와 지표 데이터를 통합하여 '시장 상태 스냅샷' 리스트를 생성하는 서비스.
    /// 전략 엔진(StrategyEngine)이 소비하는 통합 데이터 소스 역할을 함.
    /// </summary>
    public static class SnapshotService
    {
        public static List<MarketSnapshot> CreateSnapshots(string stockCode, List<FastChart.OHLCV> candles, List<FastChart.CustomSeries> indicators, double todayOpen = 0, StrategyDefinition strategy = null, List<BarData> externalDailyContext = null)
        {
            if (candles == null || candles.Count == 0) return new List<MarketSnapshot>();
            
            // 1. 필요한 가상 지표 추출 (DAILY_HIGH_COND_{days}_{pct})
            var dailyHighReqs = new List<(string key, int days, int pct)>();
            if (strategy != null)
            {
                var conds = strategy.BuyRules.Concat(strategy.SellRules).SelectMany(g => g.Conditions);
                foreach (var c in conds)
                {
                    CheckDailyReq(c.IndicatorA, dailyHighReqs);
                    CheckDailyReq(c.IndicatorB, dailyHighReqs);
                }
            }

            // 2. 일봉 데이터 집계 (Daily Aggregation)
            // 외부 데이터가 있으면 ComputeAllDailyValues 내부에서 우선 사용됨

            var snapshots = new List<MarketSnapshot>();
            
            var indicatorValues = indicators?
                .Where(s => s.Values != null && s.Values.Count > 0)
                .ToDictionary(s => s.SeriesName, s => s.Values);

            for (int i = 0; i < candles.Count; i++)
            {
                var candle = candles[i];
                var snapshotIndicators = new Dictionary<string, double>();

                if (indicatorValues != null)
                {
                    foreach (var kvp in indicatorValues)
                    {
                        if (i < kvp.Value.Count) snapshotIndicators[kvp.Key] = kvp.Value[i];
                        else snapshotIndicators[kvp.Key] = double.NaN;
                    }
                }

                // [가상 지표] 시가 대비 등락율 (%)
                double openChg = candle.Open > 0 ? (candle.Close - candle.Open) / candle.Open * 100.0 : 0;
                snapshotIndicators["CHG_OPEN_PCT"] = openChg;

                // [가상 지표] VI 상한 근접지점 (10% 상승의 99% 지점)
                double tOpen = (todayOpen > 0) ? todayOpen : candle.Open; 
                snapshotIndicators["VI_UP_99"] = tOpen * 1.10 * 0.99; 
                
                // [가상 지표] 수익률
                snapshotIndicators["PROFIT_PCT"] = 0.0; 

                snapshots.Add(new MarketSnapshot(
                    stockCode,
                    candle.DateVal,
                    candle.Close,
                    candle.Open,
                    candle.High,
                    candle.Low,
                    candle.Volume,
                    snapshotIndicators
                ));
            }

            // [V2] Post-process to fill daily values
            if (dailyHighReqs.Count > 0)
            {
                var calculatedValues = ComputeAllDailyValues(candles, dailyHighReqs, externalDailyContext);
                foreach (var snap in snapshots)
                {
                    int dateKey = GetDateKey(snap.Time);
                    foreach (var kvp in calculatedValues)
                    {
                        if (kvp.Value.TryGetValue(dateKey, out double val))
                        {
                            snap.SetIndicator(kvp.Key, val);
                        }
                    }
                }
            }

            return snapshots;
        }

        private static void CheckDailyReq(string key, List<(string, int, int)> list)
        {
            if (string.IsNullOrEmpty(key)) return;
            // Key format: DAILY_HIGH_COND_{days}_{pct}
            var parts = key.Split('_');
            if (parts.Length == 5 && parts[0] == "DAILY" && parts[1] == "HIGH" && parts[2] == "COND")
            {
                if (int.TryParse(parts[3], out int d) && int.TryParse(parts[4], out int p))
                {
                    if (!list.Any(x => x.Item1 == key)) list.Add((key, d, p));
                }
            }
        }

        private static int GetDateKey(DateTime dt) => dt.Year * 10000 + dt.Month * 100 + dt.Day;

        private static Dictionary<string, Dictionary<int, double>> ComputeAllDailyValues(List<FastChart.OHLCV> candles, List<(string key, int days, int pct)> reqs, List<BarData> externalDaily = null)
        {
            // 1. 일봉 데이터 준비
            var dailyCandles = new List<DailyCandle>();

            if (externalDaily != null && externalDaily.Count > 0)
            {
                // [Case A] 외부 일봉 데이터 사용 (우선순위 높음)
                DailyCandle prev = null;
                // 날짜순 정렬 필수
                foreach (var b in externalDaily.OrderBy(x => x.Time))
                {
                    var dc = new DailyCandle 
                    { 
                        DateKey = GetDateKey(b.Time), 
                        Open = b.Open, High = b.High, Low = b.Low, Close = b.Close 
                    };
                    
                    if (prev != null && prev.Close > 0) 
                        dc.ChangePct = (dc.Close - prev.Close) / prev.Close * 100.0;
                    else 
                        dc.ChangePct = 0;
                        
                    dailyCandles.Add(dc);
                    prev = dc;
                }
            }
            else if (candles.Count > 0)
            {
                // [Case B] 분봉 -> 일봉 변환 (데이터 부족 시 Fallback)
                var grouped = candles.GroupBy(x => GetDateKey(x.DateVal)).OrderBy(g => g.Key);
                
                DailyCandle prevDaily = null;
                foreach (var g in grouped)
                {
                    double o = g.First().Open;
                    double h = g.Max(x => x.High);
                    double l = g.Min(x => x.Low);
                    double c = g.Last().Close;
                    
                    var dc = new DailyCandle { DateKey = g.Key, Open = o, High = h, Low = l, Close = c };
                    
                    if (prevDaily != null && prevDaily.Close > 0)
                    {
                        dc.ChangePct = (dc.Close - prevDaily.Close) / prevDaily.Close * 100.0;
                    }
                    else
                    {
                        dc.ChangePct = 0; 
                    }
                    
                    dailyCandles.Add(dc);
                    prevDaily = dc;
                }
            }

            // 2. 각 요청(Req)별로 값 계산
            var result = new Dictionary<string, Dictionary<int, double>>();
            
            foreach (var req in reqs)
            {
                var valuesMap = new Dictionary<int, double>();
                // Key: DateKey, Value: Calculated High
                
                // Sliding Window for Days
                for (int i = 0; i < dailyCandles.Count; i++)
                {
                    int start = Math.Max(0, i - req.days + 1);
                    double maxHigh = 0; // 조건 만족하는 날들 중 Max High (없으면 0 or NaN?) -> 보통 돌파 로직이므로 0이면 돌파 안됨.
                    
                    bool found = false;
                    for (int j = start; j <= i; j++) // 최근 N일(오늘 포함 or 미포함? 보통 오늘 포함해서 검색)
                    {
                        var d = dailyCandles[j];
                        // 조건: 전일대비 pct% 이상 상승한 날
                        // 주의: 사용자의 요청은 "10일 중.. 상승률 10% 이상인 일자의 고가"
                        // 즉, 과거의 그 날의 고가를 의미함.
                        // 오늘이 그 날이면 오늘 고가? -> 보통 과거 기준봉을 의미.
                        // 하지만 "돌파"하려면 현재가는 그 값보다 낮았다가 올라가야 함.
                        
                        // * 중요: 오늘(j==i)이 포함되면, 오늘 시점에서 오늘 고가를 돌파한다는 건 자기 모순일 수 있음.
                        // 하지만 장중 돌파를 위해선 오늘 아침에 생긴 고가일 수도 있고...
                        // 보통 이런 조건 매매에선 "최근 N일간(오늘 제외 혹은 포함) 발생한 장대양봉의 고가"를 라인으로 긋고 그걸 돌파하는지 봄.
                        // 정석: j < i (오늘 제외) 혹은 j <= i (오늘 포함). 사용자는 "10일 중"이라고 했으므로 오늘 포함이 맞음.
                        
                        if (d.ChangePct >= req.pct)
                        {
                            if (d.High > maxHigh)
                            {
                                maxHigh = d.High;
                                found = true;
                            }
                        }
                    }
                    
                    // 해당 날짜(DateKey)의 분봉들은 이 maxHigh 값을 지표로 가짐
                    if (found) valuesMap[dailyCandles[i].DateKey] = maxHigh;
                    else valuesMap[dailyCandles[i].DateKey] = double.MaxValue; // 못 찾으면 돌파 불가능하게 아주 큰 값 (안전장치)
                }
                result[req.key] = valuesMap;
            }

            return result;
        }

        private class DailyCandle {
            public int DateKey;
            public double Open, High, Low, Close;
            public double ChangePct;
        }
    }
}
