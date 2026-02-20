using System;
using System.Collections.Generic;
using Common.Models;
using App64.Controls;

namespace App64.Agents
{
    public enum AgentType
    {
        SupportResistance,
        SectorAnalysis,
        ReferenceBar,
        PatternRecognition
    }

    /// <summary>
    /// 에이전트 분석 결과 (점수 및 상세 정보)
    /// </summary>
    public class AgentResult
    {
        public AgentType Agent { get; set; }
        public double Score { get; set; } // 0.0 ~ 100.0 (높을수록 강한 매수 신호)
        public string Note { get; set; }
        public List<double> KeyLevels { get; set; } = new List<double>(); // 지지/저항 레벨 등
        public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 학습 데이터 (매매 결과 피드백용)
    /// </summary>
    public class LearningData
    {
        public string StockCode { get; set; }
        public DateTime EntryTime { get; set; }
        public double ContextScore { get; set; } // 당시 에이전트 점수
        public string PatternType { get; set; } // 적용된 패턴
        public double ProfitPct { get; set; } // 최종 수익률
        public bool IsSuccess => ProfitPct > 0;
    }

    /// <summary>
    /// 모든 지능형 에이전트의 기본 클래스.
    /// 분석(Analyze)과 학습(Learn) 기능을 제공합니다.
    /// </summary>
    public abstract class TradingAgent
    {
        public abstract AgentType Type { get; }
        public abstract string Name { get; }
        
        // 에이전트 내부 가중치 (학습에 의해 조정됨)
        protected Dictionary<string, double> _weights = new Dictionary<string, double>();

        /// <summary>
        /// 주어진 시점의 데이터를 분석하여 점수를 반환합니다.
        /// </summary>
        public abstract AgentResult Analyze(List<FastChart.OHLCV> data, int currentIndex, string stockCode = "");

        /// <summary>
        /// 매매 결과를 바탕으로 내부 가중치를 조정(학습)합니다.
        /// </summary>
        public virtual void Learn(LearningData feedback)
        {
            // 기본 학습 로직 (구체적인 건 각 에이전트가 오버라이드)
            // 예: 성공 시 해당 패턴의 가중치 증가
            if (_weights.ContainsKey(feedback.PatternType))
            {
                double learningRate = 0.05;
                double reward = feedback.IsSuccess ? 1.0 : -1.0;
                _weights[feedback.PatternType] += learningRate * reward;
                
                // 가중치 범위 제한 (0.1 ~ 5.0)
                _weights[feedback.PatternType] = Math.Max(0.1, Math.Min(5.0, _weights[feedback.PatternType]));
            }
        }
    }
}
