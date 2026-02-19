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
        public static List<MarketSnapshot> CreateSnapshots(List<FastChart.OHLCV> candles, List<FastChart.CustomSeries> indicators)
        {
            if (candles == null || candles.Count == 0) return new List<MarketSnapshot>();

            var snapshots = new List<MarketSnapshot>();
            
            // 모든 지표를 이름별 딕셔너리로 미리 분류 (성능 최적화)
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
                        // 지표 데이터의 길이가 캔들보다 짧을 수 있으므로 인덱스 체크
                        if (i < kvp.Value.Count)
                        {
                            snapshotIndicators[kvp.Key] = kvp.Value[i];
                        }
                        else
                        {
                            snapshotIndicators[kvp.Key] = double.NaN;
                        }
                    }
                }

                snapshots.Add(new MarketSnapshot(
                    candle.DateVal,
                    candle.Close,
                    candle.Open,
                    candle.High,
                    candle.Low,
                    candle.Volume,
                    snapshotIndicators
                ));
            }

            return snapshots;
        }
    }
}
