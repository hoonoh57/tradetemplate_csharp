using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 투명하고 단순한 논리를 지향하는 전략 평가 엔진.
    /// 모든 조건은 3개 계층 이하의 트리 구조로 평가되며, 결과는 불변 객체로 반환됨.
    /// </summary>
    public class StrategyEngine
    {
        public EvaluationResult Evaluate(StrategyDefinition strategy, MarketSnapshot current, MarketSnapshot previous)
        {
            if (strategy == null || current == null) return null;

            var states = new Dictionary<string, bool>();

            // 1. 모든 개별 조건(ConditionCell) 선행 평가
            // 중복 계산을 피하기 위해 모든 규칙의 조건을 모아서 한 번만 평가함.
            var allConditions = strategy.BuyRules
                .Concat(strategy.SellRules)
                .SelectMany(g => g.Conditions)
                .Where(c => c.IsActive)
                .GroupBy(c => c.Id)
                .Select(g => g.First());

            foreach (var cell in allConditions)
            {
                states[cell.Id] = EvaluateCell(cell, current, previous);
            }

            // 2. Buy 논리 게이트 평가 (여러 게이트 중 하나라도 참이면 매수 신호 - OR 구조)
            bool isBuy = strategy.BuyRules
                .Where(g => g.IsActive)
                .Any(gate => EvaluateGate(gate, states));

            // 3. Sell 논리 게이트 평가 (여러 게이트 중 하나라도 참이면 매도 신호 - OR 구조)
            bool isSell = strategy.SellRules
                .Where(g => g.IsActive)
                .Any(gate => EvaluateGate(gate, states));

            return new EvaluationResult(current.Time, strategy.Name, isBuy, isSell, states);
        }

        private bool EvaluateCell(ConditionCell cell, MarketSnapshot curr, MarketSnapshot prev)
        {
            double valA = curr.GetValue(cell.IndicatorA);
            double valB = cell.IndicatorB != null ? curr.GetValue(cell.IndicatorB) : (cell.ConstantValue ?? double.NaN);

            if (double.IsNaN(valA) || double.IsNaN(valB)) return false;

            bool result = false;
            switch (cell.Operator)
            {
                case ComparisonOperator.GreaterThan: result = valA > valB; break;
                case ComparisonOperator.LessThan: result = valA < valB; break;
                case ComparisonOperator.GreaterThanOrEqual: result = valA >= valB; break;
                case ComparisonOperator.LessThanOrEqual: result = valA <= valB; break;
                case ComparisonOperator.Equal: result = Math.Abs(valA - valB) < 0.0000001; break;
                case ComparisonOperator.NotEqual: result = Math.Abs(valA - valB) >= 0.0000001; break;
                case ComparisonOperator.CrossUp:
                    if (prev == null) return false;
                    double prevA = prev.GetValue(cell.IndicatorA);
                    double prevB = cell.IndicatorB != null ? prev.GetValue(cell.IndicatorB) : (cell.ConstantValue ?? double.NaN);
                    result = (prevA <= prevB) && (valA > valB);
                    break;
                case ComparisonOperator.CrossDown:
                    if (prev == null) return false;
                    double pA = prev.GetValue(cell.IndicatorA);
                    double pB = cell.IndicatorB != null ? prev.GetValue(cell.IndicatorB) : (cell.ConstantValue ?? double.NaN);
                    result = (pA >= pB) && (valA < valB);
                    break;
            }

            return cell.IsInverted ? !result : result;
        }

        private bool EvaluateGate(LogicGate gate, Dictionary<string, bool> states)
        {
            var activeConditions = gate.Conditions.Where(c => c.IsActive).ToList();
            if (activeConditions.Count == 0) return false;

            if (gate.Operator == LogicalOperator.AND)
            {
                // AND 게이트: 모든 조건이 참이어야 함
                return activeConditions.All(c => states.TryGetValue(c.Id, out bool res) && res);
            }
            else
            {
                // OR 게이트: 하나라도 참이면 참
                return activeConditions.Any(c => states.TryGetValue(c.Id, out bool res) && res);
            }
        }
    }
}
