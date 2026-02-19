using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public enum ComparisonOperator
    {
        [EnumMember] GreaterThan,
        [EnumMember] LessThan,
        [EnumMember] GreaterThanOrEqual,
        [EnumMember] LessThanOrEqual,
        [EnumMember] Equal,
        [EnumMember] NotEqual,
        [EnumMember] CrossUp,
        [EnumMember] CrossDown
    }

    [DataContract]
    public enum LogicalOperator
    {
        [EnumMember] AND,
        [EnumMember] OR
    }

    [DataContract]
    public class ConditionCell
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public string IndicatorA { get; set; }
        [DataMember] public ComparisonOperator Operator { get; set; }
        [DataMember] public string IndicatorB { get; set; }
        [DataMember] public double? ConstantValue { get; set; }
        [DataMember] public bool IsActive { get; set; }
        [DataMember] public bool IsInverted { get; set; }

        public ConditionCell() { } // Serialization constructor
        public ConditionCell(string id, string desc, string indicatorA, ComparisonOperator op, string indicatorB = null, double? constantValue = null, bool isActive = true, bool isInverted = false)
        {
            Id = id; Description = desc; IndicatorA = indicatorA; Operator = op;
            IndicatorB = indicatorB; ConstantValue = constantValue; IsActive = isActive; IsInverted = isInverted;
        }

        public ConditionCell SetActive(bool active) => new ConditionCell(Id, Description, IndicatorA, Operator, IndicatorB, ConstantValue, active, IsInverted);
    }

    [DataContract]
    public class LogicGate
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public LogicalOperator Operator { get; set; }
        [DataMember] public List<ConditionCell> Conditions { get; set; }
        [DataMember] public bool IsActive { get; set; }

        public LogicGate() { Conditions = new List<ConditionCell>(); }
        public LogicGate(string name, LogicalOperator op, List<ConditionCell> conditions, bool isActive = true)
        {
            Name = name; Operator = op; Conditions = conditions ?? new List<ConditionCell>(); IsActive = isActive;
        }
    }

    [DataContract]
    public class StrategyDefinition
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public List<LogicGate> BuyRules { get; set; }
        [DataMember] public List<LogicGate> SellRules { get; set; }

        public StrategyDefinition() { BuyRules = new List<LogicGate>(); SellRules = new List<LogicGate>(); }
        public StrategyDefinition(string name, string desc, List<LogicGate> buy, List<LogicGate> sell)
        {
            Name = name; Description = desc; BuyRules = buy ?? new List<LogicGate>(); SellRules = sell ?? new List<LogicGate>();
        }
    }

    [DataContract]
    public class EvaluationResult
    {
        [DataMember] public DateTime Time { get; set; }
        [DataMember] public string StrategyName { get; set; }
        [DataMember] public bool IsBuySignal { get; set; }
        [DataMember] public bool IsSellSignal { get; set; }
        [DataMember] public Dictionary<string, bool> ConditionStates { get; set; }

        public EvaluationResult() { ConditionStates = new Dictionary<string, bool>(); }
        public EvaluationResult(DateTime time, string strategyName, bool isBuy, bool isSell, Dictionary<string, bool> states)
        {
            Time = time; StrategyName = strategyName; IsBuySignal = isBuy; IsSellSignal = isSell;
            ConditionStates = states ?? new Dictionary<string, bool>();
        }
    }
}
