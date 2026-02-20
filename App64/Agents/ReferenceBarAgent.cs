using System;
using System.Collections.Generic;
using System.Linq;
using App64.Services;
using Common.Models;
using App64.Controls;

namespace App64.Agents
{
    /// <summary>
    /// 기준봉(Reference Bar) 패턴 분석 에이전트.
    /// 의미 있는 대량 거래/장대 양봉 발생 후의 눌림목(Flag) 또는 매물대 돌파(Breakout) 패턴을 식별합니다.
    /// 특히, 기준봉 발생 후 조정 없이 매물대 상단을 돌파하는 강력한 상승세에 가점을 부여합니다.
    /// </summary>
    public class ReferenceBarAgent : TradingAgent
    {
        public override AgentType Type => AgentType.ReferenceBar;
        public override string Name => "Reference Bar Agent";

        public ReferenceBarAgent()
        {
            _weights["FlagPattern"] = 1.0;
            _weights["ResistanceBreakout"] = 1.5; // 매물대 상단 돌파 (가점)
        }

        public override AgentResult Analyze(List<FastChart.OHLCV> data, int currentIndex, string stockCode = "")
        {
            if (currentIndex < 20) return new AgentResult { Agent = Type, Score = 50 };

            var snapshot = GetRecentReferenceBar(data, currentIndex);
            if (snapshot == null) 
                return new AgentResult { Agent = Type, Score = 50, Note = "No Reference Bar" };
            
            var refBar = snapshot.Bar;
            int daysSince = currentIndex - snapshot.Index;
            double currentClose = data[currentIndex].Close;
            double refHigh = refBar.High;
            double refLow = refBar.Low; // 시가 or 저가

            double score = 50.0;
            string patternName = "None";

            // 1. 눌림목 (Flag Pattern): 기준봉 발생 후 거래량 감소하며 조정 -> 5/20 이평선 지지
            // (여기서는 간단히 가격 조정 폭으로 판단)
            bool isFlag = currentClose < refHigh && currentClose > refLow; 
            if (isFlag && daysSince >= 2 && daysSince <= 10)
            {
                // 조정폭이 너무 깊지 않아야 함 (상단 1/3 ~ 1/2 지지)
                double retracement = (refHigh - currentClose) / (refHigh - refLow);
                if (retracement < 0.5) 
                {
                    score += 15 * _weights["FlagPattern"];
                    patternName = "Flag (Healthy Pullback)";
                }
            }

            // 2. 매물대 상단 돌파 (Resistance Breakout): 
            // 기준봉 발생 후 옆으로 기거나(기간조정) 살짝 눌리다가, 
            // '조정 없이' 혹은 '짧은 조정 후' 기준봉 고가(매물대 상단)를 강력하게 돌파
            if (currentClose > refHigh)
            {
                // 돌파 시 거래량 확인 (기준봉 대비 50% 이상이면 유의미)
                bool volSupport = data[currentIndex].Volume >= refBar.Volume * 0.5;
                
                if (volSupport)
                {
                    score += 30 * _weights["ResistanceBreakout"];
                    patternName = "Resistance Breakout (Strong)";
                    
                    // [추가 가점] 기간 조정이 짧았다면 (급등주 패턴)
                    if (daysSince <= 5) score += 10;
                }
            }

            return new AgentResult
            {
                Agent = Type,
                Score = Math.Min(100, Math.Max(0, score)),
                Note = $"Pattern: {patternName}, RefBar: {daysSince} days ago (Vol: {refBar.Volume})",
                KeyLevels = new List<double> { refHigh, refLow }
            };
        }

        private class RefBarSnapshot { public FastChart.OHLCV Bar; public int Index; }

        private RefBarSnapshot GetRecentReferenceBar(List<FastChart.OHLCV> data, int currentIndex)
        {
            // 최근 20봉 이내에서 기준봉 찾기
            // 조건: 거래량이 평소 5배 이상 OR 등락률 10% 이상 (장대양봉)
            for (int i = currentIndex - 1; i >= Math.Max(0, currentIndex - 20); i--)
            {
                var bar = data[i];
                if (bar.Close <= bar.Open) continue; // 양봉만

                // 거래량 5배 (단순 비교)
                double avgVol = 0;
                int count = 0;
                for (int j = 1; j <= 5; j++) 
                {
                    if (i - j >= 0) { avgVol += data[i-j].Volume; count++; }
                }
                if (count > 0) avgVol /= count;

                bool isVolSpike = (avgVol > 0) && (bar.Volume > avgVol * 5);
                bool isPriceSpike = (bar.Close - bar.Open) / bar.Open >= 0.10; // 10% 이상 상승

                if (isVolSpike || isPriceSpike)
                {
                    return new RefBarSnapshot { Bar = bar, Index = i };
                }
            }
            return null;
        }
    }
}
