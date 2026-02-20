using System;
using System.Collections.Generic;
using System.Linq;
using App64.Services;
using Common.Models;
using App64.Controls;

namespace App64.Agents
{
    /// <summary>
    /// 차트의 스윙 고점/저점을 분석하여 주요 지지/저항선을 식별하는 에이전트.
    /// 현재 가격이 지지선 근처에서 반등하거나 저항선을 돌파할 때 높은 점수를 부여합니다.
    /// </summary>
    public class SupportResistanceAgent : TradingAgent
    {
        public override AgentType Type => AgentType.SupportResistance;
        public override string Name => "Support/Resistance Agent";

        public SupportResistanceAgent()
        {
            // 초기 가중치 설정
            _weights["Breakout"] = 1.0;
            _weights["Bounce"] = 1.2;
            _weights["ResistanceRejection"] = 0.8;
        }

        public override AgentResult Analyze(List<FastChart.OHLCV> data, int currentIndex, string stockCode = "")
        {
            if (currentIndex < 20) return new AgentResult { Agent = Type, Score = 50 }; // 데이터 부족

            var currentPrice = data[currentIndex].Close;
            var levels = IdentifyKeyLevels(data, currentIndex);
            
            // 가장 가까운 지지/저항 찾기
            double nearestSupport = 0;
            double nearestResistance = double.MaxValue;

            foreach (var lvl in levels)
            {
                if (lvl < currentPrice && lvl > nearestSupport) nearestSupport = lvl;
                if (lvl > currentPrice && lvl < nearestResistance) nearestResistance = lvl;
            }

            double score = 50.0;
            
            // 1. 지지선 매매 (Bounce Strategy): 지지선에 근접(1~2%) 후 양봉 발생 시 가점
            if (nearestSupport > 0)
            {
                double distPct = (currentPrice - nearestSupport) / nearestSupport * 100.0;
                if (distPct < 2.0 && distPct > 0) // 지지선 위 2% 이내
                {
                    // 양봉 확인
                    if (data[currentIndex].Close > data[currentIndex].Open)
                        score += 20 * _weights["Bounce"];
                }
            }

            // 2. 저항선 돌파 (Breakout Strategy): 저항선 근처(1% 이내) 혹은 막 돌파 시
            if (nearestResistance < double.MaxValue)
            {
                double distPct = (nearestResistance - currentPrice) / currentPrice * 100.0;
                if (distPct < 1.0 && distPct > -1.0) // 저항선 전후 1%
                {
                    // 거래량 실린 돌파 확인
                    double avgVol = data.Skip(currentIndex - 5).Take(5).Average(x => x.Volume);
                    if (data[currentIndex].Volume > avgVol * 1.5 && data[currentIndex].Close > nearestResistance)
                        score += 25 * _weights["Breakout"];
                }
            }

            return new AgentResult
            {
                Agent = Type,
                Score = Math.Min(100, Math.Max(0, score)),
                KeyLevels = levels,
                Note = $"Sup: {nearestSupport:F0}, Res: {nearestResistance:F0}"
            };
        }

        /// <summary>
        /// 과거 N봉 간의 스윙 고점/저점을 클러스터링하여 주요 레벨 추출
        /// </summary>
        private List<double> IdentifyKeyLevels(List<FastChart.OHLCV> data, int currentIndex)
        {
            var detected = new List<double>();
            int lookback = 60; // 최근 60봉(약 1시간) 기준
            int start = Math.Max(0, currentIndex - lookback);

            for (int i = start + 2; i < currentIndex - 2; i++)
            {
                bool isHigh = data[i].High > data[i-1].High && data[i].High > data[i-2].High &&
                              data[i].High > data[i+1].High && data[i].High > data[i+2].High;
                
                bool isLow = data[i].Low < data[i-1].Low && data[i].Low < data[i-2].Low &&
                             data[i].Low < data[i+1].Low && data[i].Low < data[i+2].Low;

                if (isHigh) detected.Add(data[i].High);
                if (isLow) detected.Add(data[i].Low);
            }

            // 클러스터링 (비슷한 가격대 뭉치기 - 1% 오차 범위)
            return ClusterLevels(detected);
        }

        private List<double> ClusterLevels(List<double> rawLevels)
        {
            var result = new List<double>();
            rawLevels.Sort();

            while (rawLevels.Count > 0)
            {
                double pivot = rawLevels[0];
                var cluster = rawLevels.Where(l => Math.Abs(l - pivot) / pivot < 0.01).ToList();
                
                // 클러스터의 평균값을 대표 레벨로 사용
                result.Add(cluster.Average());
                
                // 처리된 값 제거
                rawLevels.RemoveAll(l => cluster.Contains(l));
            }
            return result;
        }
    }
}
