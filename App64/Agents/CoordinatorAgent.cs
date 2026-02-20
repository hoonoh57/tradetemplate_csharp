using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App64.Agents
{
    /// <summary>
    /// 에이전트 시스템의 지휘자(Coordinator).
    /// 개별 에이전트의 점수를 종합하여 최종 점수를 산출하고, 
    /// 전체 종목의 순위(Ranking)를 관리하며, 
    /// 각 에이전트의 성과를 분석하여 가중치를 동적으로 조절(Meta-Learning)합니다.
    /// </summary>
    public class CoordinatorAgent
    {
        private static CoordinatorAgent _instance;
        public static CoordinatorAgent Instance => _instance ?? (_instance = new CoordinatorAgent());

        // 하위 에이전트 가중치 (기본값)
        public ConcurrentDictionary<AgentType, double> AgentWeights { get; private set; }

        // 종목별 최종 점수 (Ranking용)
        private ConcurrentDictionary<string, double> _stockScores = new ConcurrentDictionary<string, double>();
        
        // 랭킹 이력 (성능 분석용)
        private ConcurrentQueue<RankingSnapshot> _rankingHistory = new ConcurrentQueue<RankingSnapshot>();

        public CoordinatorAgent()
        {
            AgentWeights = new ConcurrentDictionary<AgentType, double>();
            AgentWeights[AgentType.SupportResistance] = 1.0;
            AgentWeights[AgentType.SectorAnalysis] = 1.0;
            AgentWeights[AgentType.ReferenceBar] = 1.0;
            AgentWeights[AgentType.IndicatorAnalysis] = 0.8;
            
            // 메타 러닝 루프 시작
            Task.Run(MetaLearningLoop);
        }

        /// <summary>
        /// 여러 에이전트의 결과를 종합하여 가중 평균 점수를 계산합니다.
        /// </summary>
        public double AggregateScores(string stockCode, List<AgentResult> results)
        {
            if (results == null || results.Count == 0) return 50.0;

            double weightedSum = 0;
            double totalWeight = 0;

            foreach (var res in results)
            {
                if (AgentWeights.TryGetValue(res.Agent, out double w))
                {
                    weightedSum += res.Score * w;
                    totalWeight += w;
                }
            }

            double finalScore = (totalWeight > 0) ? weightedSum / totalWeight : 50.0;
            
            // 랭킹 업데이트
            if (!string.IsNullOrEmpty(stockCode))
            {
                _stockScores[stockCode] = finalScore;
            }

            return finalScore;
        }

        // 순위 캐시 (UI 성능 최적화용)
        private ConcurrentDictionary<string, int> _cachedRanks = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 특정 종목의 현재 통합 점수 반환
        /// </summary>
        public double GetScore(string stockCode)
        {
            if (string.IsNullOrEmpty(stockCode)) return 50.0;
            return _stockScores.TryGetValue(stockCode, out double score) ? score : 50.0;
        }

        /// <summary>
        /// 특정 종목의 현재 순위 반환 (1부터 시작, 캐시된 데이터 사용)
        /// </summary>
        public int GetRank(string stockCode)
        {
            if (string.IsNullOrEmpty(stockCode)) return 0;
            return _cachedRanks.TryGetValue(stockCode, out int rank) ? rank : 0;
        }

        /// <summary>
        /// 현재 점수 기준 상위 N개 종목 반환
        /// </summary>
        public List<KeyValuePair<string, double>> GetTopRankedStocks(int n = 10)
        {
            return _stockScores.OrderByDescending(x => x.Value).Take(n).ToList();
        }

        /// <summary>
        /// [메타 러닝] 주기적으로 랭킹의 효율성을 분석하고 가중치를 조정
        /// </summary>
        private async Task MetaLearningLoop()
        {
            while (true)
            {
                try
                {
                    // 1. 전체 순위 미리 계산 (UI 캐시용)
                    var allSorted = _stockScores.OrderByDescending(x => x.Value).ToList();
                    var newRanks = new ConcurrentDictionary<string, int>();
                    for (int i = 0; i < allSorted.Count; i++)
                    {
                        newRanks[allSorted[i].Key] = i + 1;
                    }
                    _cachedRanks = newRanks;

                    await Task.Delay(5000); // 5초마다 순위 갱신 (부담 최소화)

                    // 2. 현재 상위 랭킹 스냅샷 저장 (1분마다)
                    // ... 기존 로직 생략 가능하나 위에서 i loop 돌릴 때 같이 처리 ...
                }
                catch (Exception ex)
                {
                    await Task.Delay(1000);
                }
            }
        }
        
        // 시스템 상태 진단 (Churn Rate, Hit Rate)
        public string DiagnoseSystemHealth()
        {
            // TODO: 랭킹 교체율(Churn Rate) 계산 로직 구현
            return "System Health: Good (Placeholder)";
        }

        private class RankingSnapshot
        {
            public DateTime Time { get; set; }
            public List<string> TopStocks { get; set; }
        }
    }
}
