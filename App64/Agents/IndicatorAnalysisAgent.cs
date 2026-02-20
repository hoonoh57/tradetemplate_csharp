using System;
using System.Collections.Generic;
using System.Linq;
using App64.Services;
using Common.Models;
using App64.Controls;

namespace App64.Agents
{
    /// <summary>
    /// 복합 지표(TickIntensity, MACD, 등) 상태를 분석하는 에이전트.
    /// 단순 값 비교를 넘어, 지표 간의 다이버전스나 추세 강화 여부를 판단합니다.
    /// 또한, 성과가 좋은 지표 파라미터를 학습하여 가중치를 조절합니다.
    /// </summary>
    public class IndicatorAnalysisAgent : TradingAgent
    {
        public override AgentType Type => AgentType.IndicatorAnalysis;
        public override string Name => "Indicator Analysis Agent";

        public IndicatorAnalysisAgent()
        {
            _weights["TickIntensity"] = 1.2;
            _weights["MACD_Trend"] = 1.0;
            _weights["RSI_Strength"] = 0.8;
            _weights["SuperTrend"] = 1.0;
        }

        public override AgentResult Analyze(List<FastChart.OHLCV> data, int currentIndex, string stockCode = "", List<FastChart.CustomSeries> indicators = null)
        {
            if (currentIndex < 20) return new AgentResult { Agent = Type, Score = 50 };

            double score = 50.0;
            var details = new List<string>();

            // 1. Tick Intensity (수급 강도)
            // 외부에서 Calculated Indicator로 넘어오거나, Data에 포함되어 있다고 가정.
            // 여기서는 indicators 리스트에서 찾음.
            double tickIntensity = GetIndicatorValue(indicators, "TickIntensity", currentIndex);
            if (tickIntensity > 0) // 값이 존재할 때만
            {
                // 기본 5.0 이상이면 강세
                if (tickIntensity >= 5.0) 
                {
                    score += 15 * _weights["TickIntensity"];
                    details.Add($"TickInt({tickIntensity:F1}) > 5");
                }
                else if (tickIntensity >= 1.0)
                {
                    score += 5 * _weights["TickIntensity"];
                }
                else 
                {
                    score -= 5; // 수급 약세 감점
                }
            }

            // 2. MACD (추세)
            double macdHist = GetIndicatorValue(indicators, "MACD_Hist", currentIndex);
            double macdHistPrev = GetIndicatorValue(indicators, "MACD_Hist", currentIndex - 1);
            if (macdHist != double.MinValue)
            {
                if (macdHist > 0 && macdHist > macdHistPrev)
                {
                    score += 10 * _weights["MACD_Trend"]; // 상승 확산
                    details.Add("MACD Expanding");
                }
                else if (macdHist > 0 && macdHist < macdHistPrev)
                {
                    score -= 5; // 상승 축소 (조정 가능성)
                }
                else if (macdHist < 0 && macdHist > macdHistPrev)
                {
                    score += 5; // 하락 축소 (반등 가능성)
                }
            }

            // 3. SuperTrend (추세 방향)
            double superTrend = GetIndicatorValue(indicators, "SuperTrend", currentIndex);
            double currentPrice = data[currentIndex].Close;
            if (superTrend != double.MinValue)
            {
                if (currentPrice > superTrend)
                {
                    score += 10 * _weights["SuperTrend"]; // 상승 추세 중
                    details.Add("SuperTrend Bullish");
                }
                else
                {
                    score -= 10; // 하락 추세 중
                }
            }

            return new AgentResult
            {
                Agent = Type,
                Score = Math.Min(100, Math.Max(0, score)),
                Note = string.Join(", ", details),
                ExtraData = new Dictionary<string, object> 
                { 
                    { "TickIntensity", tickIntensity },
                    { "MACD_Hist", macdHist }
                }
            };
        }

        private double GetIndicatorValue(List<FastChart.CustomSeries> indicators, string nameContains, int index)
        {
            if (indicators == null) return double.MinValue;
            
            // 이름에 특정 문자열이 포함된 시리즈 찾기 (대소문자 무시)
            var series = indicators.FirstOrDefault(s => s.SeriesName.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0);
            
            if (series != null && series.Values != null && index < series.Values.Count)
            {
                return series.Values[index];
            }
            return double.MinValue;
        }

        public override void Learn(LearningData feedback)
        {
            base.Learn(feedback);
            // 추후: 지표 파라미터(기간 등) 최적화 로직 추가 예정
            // 예: MACD(12,26) 보다 MACD(5,20)이 이 종목에 더 잘 맞았다면 가중치 이동
        }
    }
}
