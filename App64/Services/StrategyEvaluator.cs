using System;
using System.Collections.Generic;
using System.Linq;
using App64.Controls;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 차트 데이터와 전략 정의를 결합하여 최종 신호를 생성하고, 
    /// 각 시점의 평가 결과를 관리하는 상위 조정 서비스.
    /// </summary>
    public class StrategyEvaluator
    {
        private readonly StrategyEngine _engine = new StrategyEngine();

        /// <summary>
        /// 전체 데이터셋에 대해 전략을 평가하고 이력(Results)을 생성함.
        /// </summary>
        public List<EvaluationResult> RunHistorical(StrategyDefinition strategy, List<FastChart.OHLCV> data, List<FastChart.CustomSeries> indicators, double todayOpen = 0)
        {
            if (strategy == null || data == null || data.Count == 0) return new List<EvaluationResult>();

            var snapshots = SnapshotService.CreateSnapshots(data, indicators, todayOpen);
            var results = new List<EvaluationResult>();

            // 가상 포지션 상태 추적 (수익률 기반 매도를 위해)
            double entryPrice = 0;
            bool hasPosition = false;

            for (int i = 0; i < snapshots.Count; i++)
            {
                var snap = snapshots[i];

                // [상태 반영] 현재 포지션이 있다면 수익률(PROFIT_PCT) 계산하여 주입
                if (hasPosition && entryPrice > 0)
                {
                    snap.SetIndicator("PROFIT_PCT", (snap.Close - entryPrice) / entryPrice * 100.0);
                }
                else
                {
                    snap.SetIndicator("PROFIT_PCT", 0.0);
                }

                var res = _engine.Evaluate(strategy, snapshots, i);
                
                // [상태 업데이트] 매수/매도 시그널에 따른 가상 포지션 변화 기록
                if (res.IsBuySignal && !hasPosition)
                {
                    hasPosition = true;
                    entryPrice = snap.Close;
                }
                else if (res.IsSellSignal && hasPosition)
                {
                    hasPosition = false;
                    entryPrice = 0;
                }

                results.Add(res);
            }

            return results;
        }

        /// <summary>
        /// 생성된 평가 결과를 기반으로 FastChart에 표시할 신호(SignalMarker) 리스트를 생성함.
        /// </summary>
        public List<FastChart.SignalMarker> GenerateMarkers(List<EvaluationResult> results, List<FastChart.OHLCV> data)
        {
            var markers = new List<FastChart.SignalMarker>();
            if (results == null || data == null) return markers;

            for (int i = 0; i < results.Count; i++)
            {
                var res = results[i];
                if (i >= data.Count) break;

                if (res.IsBuySignal)
                {
                    markers.Add(new FastChart.SignalMarker
                    {
                        Time = res.Time,
                        Type = FastChart.SignalType.Buy,
                        Price = data[i].Low, // 매수 신호는 캔들 아래쪽에 표시하기 위해 저가 기준
                        Note = "Buy Signal (Pass all gates)"
                    });
                }
                else if (res.IsSellSignal)
                {
                    markers.Add(new FastChart.SignalMarker
                    {
                        Time = res.Time,
                        Type = FastChart.SignalType.Sell,
                        Price = data[i].High, // 매도 신호는 캔들 위쪽에 표시하기 위해 고가 기준
                        Note = "Sell Signal (Pass all gates)"
                    });
                }
            }
            return markers;
        }
    }
}
