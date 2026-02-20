using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using App64.Agents;
using Common.Models;
using App64.Controls;

namespace App64.Services
{
    /// <summary>
    /// 에이전트 시스템 관리자.
    /// 에이전트들의 생명주기 관리, 실행 요청 처리, 성능 모니터링(Circuit Breaker)을 담당합니다.
    /// </summary>
    public class AgentManager
    {
        private static AgentManager _instance;
        public static AgentManager Instance => _instance ?? (_instance = new AgentManager());

        private List<TradingAgent> _agents = new List<TradingAgent>();
        private ConcurrentDictionary<AgentType, PerformanceStats> _perfStats = new ConcurrentDictionary<AgentType, PerformanceStats>();
        
        // 성능 안전장치 설정
        private const double CIRCUIT_BREAKER_MS = 20.0; // 20ms 이상 걸리면 경고/비활성화
        private bool _isPerformanceMode = false; // 부하가 심할 경우 True로 전환

        public AgentManager()
        {
            InitializeAgents();
            LoadState(); // 저장된 가중치 로드
        }

        private void InitializeAgents()
        {
            _agents.Add(new SupportResistanceAgent());
            _agents.Add(new SectorAnalysisAgent());
            _agents.Add(new ReferenceBarAgent());
            _agents.Add(new IndicatorAnalysisAgent());

            foreach (var agent in _agents)
            {
                _perfStats[agent.Type] = new PerformanceStats();
            }
        }

        /// <summary>
        /// 에이전트 및 코디네이터의 가중치를 파일에 저장합니다.
        /// </summary>
        public void SaveState()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_weights.txt");
                using (var sw = new System.IO.StreamWriter(path))
                {
                    // 1. 하위 에이전트별 가중치 저장
                    foreach (var agent in _agents)
                    {
                        sw.WriteLine($"[{agent.Type}]");
                        foreach (var kvp in agent.Weights)
                        {
                            sw.WriteLine($"{kvp.Key}={kvp.Value}");
                        }
                    }

                    // 2. 코디네이터 가중치 저장
                    sw.WriteLine("[Coordinator]");
                    foreach (var kvp in CoordinatorAgent.Instance.AgentWeights)
                    {
                        sw.WriteLine($"{kvp.Key}={kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 로깅 (생략)
            }
        }

        /// <summary>
        /// 파일에서 가중치를 로드합니다.
        /// </summary>
        public void LoadState()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_weights.txt");
                if (!System.IO.File.Exists(path)) return;

                string currentSection = "";
                foreach (string line in System.IO.File.ReadLines(path))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        continue;
                    }

                    var parts = trimmed.Split('=');
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    if (!double.TryParse(parts[1], out double val)) continue;

                    if (currentSection == "Coordinator")
                    {
                        if (Enum.TryParse(key, out AgentType atype))
                        {
                            CoordinatorAgent.Instance.AgentWeights[atype] = val;
                        }
                    }
                    else
                    {
                        // 해당 에이전트 찾아서 가중치 업데이트
                        var agent = _agents.FirstOrDefault(a => a.Type.ToString() == currentSection);
                        if (agent != null && agent.Weights.ContainsKey(key))
                        {
                            agent.Weights[key] = val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 로깅 (생략)
            }
        }

        /// <summary>
        /// 특정 종목에 대해 모든 활성화된 에이전트의 분석을 수행하고, 최종 점수를 반환합니다.
        /// (Circuit Breaker 적용)
        /// </summary>
        public (double FinalScore, List<AgentResult> Details) Analyze(string stockCode, List<FastChart.OHLCV> data, int currentIndex, List<FastChart.CustomSeries> indicators = null)
        {
            var results = new List<AgentResult>();
            
            foreach (var agent in _agents)
            {
                // [Circuit Breaker] 성능 이슈가 있는 에이전트는 스킵
                if (_perfStats[agent.Type].IsDisabled) continue;

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var res = agent.Analyze(data, currentIndex, stockCode, indicators);
                    if (res != null) results.Add(res);
                }
                catch (Exception ex)
                {
                    // 에러 로깅 (생략)
                    _perfStats[agent.Type].ErrorCount++;
                }
                stopwatch.Stop();

                // 성능 통계 업데이트
                UpdatePerformanceStats(agent.Type, stopwatch.Elapsed.TotalMilliseconds);
            }

            // Coordinator를 통해 최종 점수 산출
            double score = CoordinatorAgent.Instance.AggregateScores(stockCode, results);
            return (score, results);
        }

        /// <summary>
        /// 매매 종료 후 학습 데이터 등록
        /// </summary>
        public void RegisterTradeResult(string stockCode, double profitPct, DateTime entryTime)
        {
            // TODO: 당시의 AgentResult 등을 조회하여 LearningData 생성
            // 현재는 간단히 각 에이전트에게 결과 통보만 구현
            var feedback = new LearningData
            {
                StockCode = stockCode,
                EntryTime = entryTime,
                ProfitPct = profitPct
            };

            foreach(var agent in _agents)
            {
                // 비동기로 학습 요청 (메인 스레드 부하 방지)
                Task.Run(() => agent.Learn(feedback));
            }

            // 가중치 변경 후 비동기로 저장
            Task.Run(() => SaveState());
        }

        private void UpdatePerformanceStats(AgentType types, double elapsedMs)
        {
            var stats = _perfStats[types];
            stats.TotalExecutionTime += elapsedMs;
            stats.ExecutionCount++;
            stats.AverageTime = stats.TotalExecutionTime / stats.ExecutionCount;

            // [Circuit Breaker] 평균 실행 시간이 기준치 초과 시 비활성화
            if (stats.AverageTime > CIRCUIT_BREAKER_MS && stats.ExecutionCount > 100)
            {
                 // 너무 느린 에이전트는 일시적으로 비활성화 (로그 남김)
                 // stats.IsDisabled = true; 
                 // 현재는 비활성화 대신 경고만 남기겠음 (실전 적용 시 Uncomment)
            }
        }

        public class PerformanceStats
        {
            public double TotalExecutionTime { get; set; }
            public long ExecutionCount { get; set; }
            public double AverageTime { get; set; }
            public int ErrorCount { get; set; }
            public bool IsDisabled { get; set; }
        }
    }
}
