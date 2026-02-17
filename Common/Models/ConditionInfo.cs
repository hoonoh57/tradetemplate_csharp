using System;
using System.Collections.Generic;

namespace Common.Models
{
    /// <summary>
    /// 조건식 정보 — 불변(Immutable)
    /// </summary>
    public sealed class ConditionInfo
    {
        public int Index { get; }
        public string Name { get; }
        public bool IsRealtime { get; }
        public IReadOnlyList<string> MatchedCodes { get; }
        public DateTime SearchTime { get; }

        public ConditionInfo(int index, string name, bool isRealtime,
            IReadOnlyList<string> matchedCodes, DateTime searchTime)
        {
            Index = index;
            Name = name ?? "";
            IsRealtime = isRealtime;
            MatchedCodes = matchedCodes ?? Array.Empty<string>();
            SearchTime = searchTime;
        }

        public ConditionInfo WithResults(IReadOnlyList<string> newCodes, DateTime searchTime)
        {
            return new ConditionInfo(Index, Name, IsRealtime, newCodes, searchTime);
        }

        public override string ToString() =>
            $"[{Index}] {Name} ({MatchedCodes.Count} hits) RT:{IsRealtime}";
    }
}