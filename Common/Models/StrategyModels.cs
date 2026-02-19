using System;
using System.Collections.Generic;

namespace Common.Models
{
    public enum ComparisonOperator
    {
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Equal,
        NotEqual,
        CrossUp,
        CrossDown
    }

    public enum LogicalOperator
    {
        AND,
        OR
    }

    /// <summary>
    /// 원자적 조건 (Condition Cell). 
    /// 예: "Price" > "SMA20", "TickIntensity" > 5.0
    /// </summary>
    public sealed class ConditionCell
    {
        public string Id { get; }
        public string Description { get; }
        public string IndicatorA { get; }
        public ComparisonOperator Operator { get; }
        public string IndicatorB { get; } // 다른 지표와 비교할 경우 사용
        public double? ConstantValue { get; } // 상수와 비교할 경우 사용 (IndicatorB가 null일 때)
        public bool IsActive { get; }
        public bool IsInverted { get; } // NOT 역할

        public ConditionCell(string id, string desc, string indicatorA, ComparisonOperator op, string indicatorB = null, double? constantValue = null, bool isActive = true, bool isInverted = false)
        {
            Id = id;
            Description = desc;
            IndicatorA = indicatorA;
            Operator = op;
            IndicatorB = indicatorB;
            ConstantValue = constantValue;
            IsActive = isActive;
            IsInverted = isInverted;
        }

        public ConditionCell SetActive(bool active) => new ConditionCell(Id, Description, IndicatorA, Operator, IndicatorB, ConstantValue, active, IsInverted);
    }

    /// <summary>
    /// 논리 게이트 (Logic Gate). 조건들을 AND/OR로 묶음.
    /// 계층이 깊어지는 것을 방지하기 위해 단일 레벨 리스트를 권장함.
    /// </summary>
    public sealed class LogicGate
    {
        public string Name { get; }
        public LogicalOperator Operator { get; }
        public IReadOnlyList<ConditionCell> Conditions { get; }
        public bool IsActive { get; }

        public LogicGate(string name, LogicalOperator op, IReadOnlyList<ConditionCell> conditions, bool isActive = true)
        {
            Name = name;
            Operator = op;
            Conditions = conditions ?? Array.Empty<ConditionCell>();
            IsActive = isActive;
        }
    }

    /// <summary>
    /// 전략 정의 (Strategy Definition). 
    /// 진입(Buy)과 청산(Sell) 로직의 집합.
    /// </summary>
    public sealed class StrategyDefinition
    {
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<LogicGate> BuyRules { get; }
        public IReadOnlyList<LogicGate> SellRules { get; }

        public StrategyDefinition(string name, string desc, IReadOnlyList<LogicGate> buy, IReadOnlyList<LogicGate> sell)
        {
            Name = name;
            Description = desc;
            BuyRules = buy ?? Array.Empty<LogicGate>();
            SellRules = sell ?? Array.Empty<LogicGate>();
        }
    }

    /// <summary>
    /// 평가 결과 스냅샷. 차트 시각화 및 투명한 검증을 위해 사용됨.
    /// </summary>
    public sealed class EvaluationResult
    {
        public DateTime Time { get; }
        public string StrategyName { get; }
        public bool IsBuySignal { get; }
        public bool IsSellSignal { get; }
        public IReadOnlyDictionary<string, bool> ConditionStates { get; } // Condition ID -> Pass/Fail

        public EvaluationResult(DateTime time, string strategyName, bool isBuy, bool isSell, IReadOnlyDictionary<string, bool> states)
        {
            Time = time;
            StrategyName = strategyName;
            IsBuySignal = isBuy;
            IsSellSignal = isSell;
            ConditionStates = states ?? new Dictionary<string, bool>();
        }
    }
}
