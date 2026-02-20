using System;
using System.Collections.Generic;
using System.Linq;
using App64.Services;
using Common.Models;
using App64.Controls;

namespace App64.Agents
{
    /// <summary>
    /// 섹터 강세와 대장주를 분석하는 에이전트.
    /// 현재 종목이 속한 섹터가 오늘 강한지, 그리고 이 종목이 대장주인지 판단합니다.
    /// </summary>
    public class SectorAnalysisAgent : TradingAgent
    {
        public override AgentType Type => AgentType.SectorAnalysis;
        public override string Name => "Sector Analysis Agent";

        // [가정] 외부에서 주입되는 섹터 정보. 
        // 실제로는 별도 서비스나 API를 통해 전체 종목의 등락률 맵(Map)을 받아야 함.
        public Dictionary<string, List<string>> SectorMap { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, double> RealTimeChangeMap { get; set; } = new Dictionary<string, double>();

        public SectorAnalysisAgent()
        {
            _weights["LeaderBonus"] = 1.5;
            _weights["SectorStrength"] = 1.0;
        }

        public override AgentResult Analyze(List<FastChart.OHLCV> data, int currentIndex, string stockCode = "", List<FastChart.CustomSeries> indicators = null)
        {
            // 섹터 정보가 없으면 중립 점수 반환
            if (string.IsNullOrEmpty(stockCode) || !SectorMap.Any()) 
                return new AgentResult { Agent = Type, Score = 50, Note = "No Sector Info" };

            // 1. 내 종목의 섹터 찾기
            string mySector = SectorMap.FirstOrDefault(x => x.Value.Contains(stockCode)).Key;
            if (mySector == null) return new AgentResult { Agent = Type, Score = 50, Note = "Unknown Sector" };

            // 2. 섹터 평균 등락률 계산
            var sectorStocks = SectorMap[mySector];
            double sectorSum = 0;
            int count = 0;
            string leaderCode = "";
            double maxChange = -999;

            foreach (var code in sectorStocks)
            {
                if (RealTimeChangeMap.TryGetValue(code, out double change))
                {
                    sectorSum += change;
                    count++;
                    if (change > maxChange)
                    {
                        maxChange = change;
                        leaderCode = code;
                    }
                }
            }

            double sectorAvg = count > 0 ? sectorSum / count : 0;
            double myChange = RealTimeChangeMap.ContainsKey(stockCode) ? RealTimeChangeMap[stockCode] : 0;

            // 3. 점수 산정
            double score = 50;

            // 섹터가 강세면 기본 점수 상승 (평균 3% 이상이면 강세)
            if (sectorAvg > 3.0) score += 20 * _weights["SectorStrength"];
            else if (sectorAvg > 1.0) score += 10 * _weights["SectorStrength"];
            else if (sectorAvg < -1.0) score -= 10;

            // 대장주 여부 (내가 대장주면 가점)
            bool isLeader = (stockCode == leaderCode);
            if (isLeader) score += 25 * _weights["LeaderBonus"];
            
            // 2등주 매매 (대장주가 상한가거나 급등 시, 2등주 따라가기)
            if (!isLeader && maxChange > 15.0 && myChange > 5.0)
            {
                 score += 15; // 2등주 추격 매수 점수
            }

            return new AgentResult
            {
                Agent = Type,
                Score = Math.Min(100, Math.Max(0, score)),
                Note = $"Sector: {mySector} (Avg: {sectorAvg:F1}%), Leader: {leaderCode}",
                ExtraData = new Dictionary<string, object> { { "IsLeader", isLeader }, { "SectorAvg", sectorAvg } }
            };
        }
    }
}
