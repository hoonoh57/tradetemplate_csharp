using System;
using System.Collections.Generic;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 사용자님의 자연어 요구사항을 '원자적 조건'과 '논리 게이트'로 정밀하게 맵핑해주는 브릿지.
    /// 코딩 없이 로직을 조립하는 '자유로운 선순환 구조'의 핵심 창구 역할을 수행합니다.
    /// </summary>
    public static class StrategyBridge
    {
        public static StrategyDefinition CreateFromNaturalLanguage(string nlPrompt)
        {
            // [비전 실현] 사용자가 자연어로 주문한 "상승전환 + 강세 확인 -> 매수 / 이평이탈 -> 매도(추세고려)" 로직 구현
            if (string.IsNullOrEmpty(nlPrompt)) return null;

            if (nlPrompt.Contains("상환") || nlPrompt.Contains("상승전환") || nlPrompt.Contains("강세"))
            {
                // 1. 원자적 조건(Condition Cells) 설계
                // 3계층 이하로 유지하며, 단순한 요소의 선택과 파라미터만으로 동작하도록 구성
                
                var condTrendUp = new ConditionCell(
                    id: "C_TREND_UP", 
                    desc: "추세 상승 전환 (Price CrossUp SuperTrend)", 
                    indicatorA: "Price", 
                    op: ComparisonOperator.CrossUp, 
                    indicatorB: "SuperTrend"
                );

                var condTickStrong = new ConditionCell(
                    id: "C_TICK_STRONG", 
                    desc: "틱강도 강세 (TickRate > 5.0)", 
                    indicatorA: "TICK_RAT", 
                    op: ComparisonOperator.GreaterThan, 
                    constantValue: 5.0
                );

                var condMaExit = new ConditionCell(
                    id: "C_MA_EXIT", 
                    desc: "20이평 하향 이탈 (Price CrossDown MA_20)", 
                    indicatorA: "Price", 
                    op: ComparisonOperator.CrossDown, 
                    indicatorB: "MA_20"
                );

                var condTrendAlive = new ConditionCell(
                    id: "C_TREND_ALIVE", 
                    desc: "추세 유지 중 (Price > SuperTrend)", 
                    indicatorA: "Price", 
                    op: ComparisonOperator.GreaterThan, 
                    indicatorB: "SuperTrend"
                );

                // 2. 논리 게이트(Logic Gates) 조립
                // "1분봉 상승전환 AND 시장 강세" -> 매수 실행
                var buyGate = new LogicGate(
                    name: "Strategy_Entry", 
                    op: LogicalOperator.AND, 
                    conditions: new List<ConditionCell> { condTrendUp, condTickStrong }
                );

                // "20이평 하향 돌파 AND NOT(추세 살아있음)" -> 매도 실행
                // "추세가 살아있으면 매도를 자제"라는 사용자 철학을 논리 게이트에 정밀 투영함
                var sellGate = new LogicGate(
                    name: "Strategy_Exit", 
                    op: LogicalOperator.AND, 
                    conditions: new List<ConditionCell> { 
                        condMaExit, 
                        // Inverted: true -> NOT (Price > SuperTrend)
                        new ConditionCell("C_TREND_DEAD", "추세 이탈 확인", "Price", ComparisonOperator.GreaterThan, "SuperTrend", null, true, true) 
                    }
                );

                return new StrategyDefinition(
                    name: "UltraTrend_V1",
                    desc: "자연어 기반 커스텀 전략: " + nlPrompt,
                    buy: new List<LogicGate> { buyGate },
                    sell: new List<LogicGate> { sellGate }
                );
            }

            return null;
        }
    }
}
