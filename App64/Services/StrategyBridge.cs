using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 사용자님의 자유로운 자연어를 정밀한 기술적 논리로 해석하는 지능형 브릿지.
    /// 정규표현식(Regex)과 키워드 매핑을 결합하여 '그냥 말하는 대로' 전략을 설계합니다.
    /// </summary>
    public static class StrategyBridge
    {
        public static StrategyDefinition CreateFromNaturalLanguage(string nlPrompt)
        {
            if (string.IsNullOrEmpty(nlPrompt)) return null;

            // 1. 매수(진입)와 매도(청산) 섹션 분리
            string buyPart = "";
            string sellPart = "";

            var splitBuy = Regex.Split(nlPrompt, "매수|진행|진입", RegexOptions.IgnoreCase);
            if (splitBuy.Length > 1)
            {
                buyPart = splitBuy[0];
                var nextPart = splitBuy[1];
                var splitSell = Regex.Split(nextPart, "매도|청산|탈출", RegexOptions.IgnoreCase);
                if (splitSell.Length > 1) { sellPart = splitSell[0]; } // 매수 뒤에 오는 매도 조건
                else { sellPart = nextPart; } // 매수 설명 이후 나머지가 매도일 가능성
            }
            else
            {
                // 구분자가 명확하지 않으면 쉼표로 분리 시도
                var clauses = nlPrompt.Split(',', '.');
                buyPart = clauses[0];
                if (clauses.Length > 1) sellPart = string.Join(",", clauses.Skip(1));
            }

            // 2. 조건 추출 및 변환
            var buyConditions = ParseConditions(buyPart, true);
            var sellConditions = ParseConditions(sellPart, false);

            if (buyConditions.Count == 0 && sellConditions.Count == 0) return null;

            // 3. 전략 조립
            string strategyName = "AI_Custom_" + DateTime.Now.ToString("HHmmss");
            var buyGate = new LogicGate("EntryGate", LogicalOperator.AND, buyConditions);
            var sellGate = new LogicGate("ExitGate", LogicalOperator.OR, sellConditions);

            return new StrategyDefinition(
                strategyName,
                "자연어 해석 전략: " + nlPrompt,
                new List<LogicGate> { buyGate },
                new List<LogicGate> { sellGate },
                nlPrompt // [추가] 원문 프롬프트 저장
            );
        }

        private static List<ConditionCell> ParseConditions(string part, bool isBuy)
        {
            var results = new List<ConditionCell>();
            if (string.IsNullOrWhiteSpace(part)) return results;

            int condId = 1;

            // [패넌 0] N일 중 ... 고가 돌파 (복합 로직 - 일봉 기준)
            // "10일중 전일대비 종가 상승률이 10% 이상인 일자의 고가를 돌파"
            var mComplex = Regex.Match(part, @"(\d+)\s*(일|봉)\s*중\s*.*(\d+)\s*%\s*이상\s*.*고가를?\s*(돌파|이상)");
            if (mComplex.Success)
            {
                int days = int.Parse(mComplex.Groups[1].Value);
                int pct = int.Parse(mComplex.Groups[3].Value);
                
                // 특수 가상 지표 이름 생성: DAILY_HIGH_COND_{days}_{pct}
                // 이는 SnapshotService에서 동적으로 계산되어야 함
                string indicatorName = $"DAILY_HIGH_COND_{days}_{pct}";
                
                results.Add(new ConditionCell($"B{condId++}", $"{days}일중 {pct}%이상 상승일 고가 돌파", "Price", ComparisonOperator.CrossUp, indicatorName));
            }

            // 패턴 1: 시가대비 X% 돌파/이상/하락
            var mOpen = Regex.Match(part, @"시가대비\s*(\d+(\.\d+)?)\s*%?\s*(상승|하락)?\s*(돌파|이상|이하|초과|미만)");
            if (mOpen.Success)
            {
                double val = double.Parse(mOpen.Groups[1].Value);
                if (mOpen.Groups[3].Value == "하락") val = -val;
                string opStr = mOpen.Groups[4].Value;
                results.Add(new ConditionCell($"B{condId++}", $"시가대비 {val}% {opStr}", "CHG_OPEN_PCT", MapOperator(opStr), null, val));
            }

            // 패턴 2: 틱강도 X 이상/돌파
            var mTick = Regex.Match(part, @"(틱강도|체결강도)\s*(\w+)?\s*가?\s*(\d+(\.\d+)?)\s*(이상|돌파|초과)");
            if (mTick.Success)
            {
                double val = double.Parse(mTick.Groups[3].Value);
                results.Add(new ConditionCell($"B{condId++}", $"틱강도 {val} {mTick.Groups[5].Value}", "TICK_RAT", MapOperator(mTick.Groups[5].Value), null, val));
            }

            // 패턴 3: SuperTrend 상승/하락 추세
            if (part.Contains("supertrend") || part.Contains("슈퍼트렌드"))
            {
                if (part.Contains("상승추세") || part.Contains("위") || (isBuy && part.Contains("돌파")))
                    results.Add(new ConditionCell($"B{condId++}", "SuperTrend 상승 유지", "Price", ComparisonOperator.GreaterThan, "SuperTrend"));
                else if (part.Contains("하락추세") || part.Contains("아래") || (!isBuy && part.Contains("이탈")))
                    results.Add(new ConditionCell($"B{condId++}", "SuperTrend 하락 유지", "Price", ComparisonOperator.LessThan, "SuperTrend"));
            }

            // 패턴 4: 매도 특화 (VI 직전, 손절 등)
            if (!isBuy)
            {
                // VI 직전 매도
                if (part.Contains("vi") && (part.Contains("직전") || part.Contains("근접")))
                {
                    results.Add(new ConditionCell($"B{condId++}", "VI 상한가 근접 (99% 도달)", "Price", ComparisonOperator.GreaterThanOrEqual, "VI_UP_99"));
                }

                // 손절매 (-2% 하락 시 등)
                var mStop = Regex.Match(part, @"(-?\d+)\s*%\s*(하락|이탈|손절|시)");
                if (mStop.Success)
                {
                    double val = double.Parse(mStop.Groups[1].Value);
                    if (val > 0) val = -val; // 하락은 음수로 처리
                    results.Add(new ConditionCell($"B{condId++}", $"손절매 ({val}%)", "PROFIT_PCT", ComparisonOperator.LessThanOrEqual, null, val));
                }

                var mPctRange = Regex.Match(part, @"(\d+(\.\d+)?)\s*%?\s*(상승|하락)?\s*(하면|시)");
                if (mPctRange.Success)
                {
                    double val = double.Parse(mPctRange.Groups[1].Value);
                    if (mPctRange.Groups[3].Value == "하락")
                         results.Add(new ConditionCell($"B{condId++}", $"시가대비 {val}% 하락 매도", "CHG_OPEN_PCT", ComparisonOperator.LessThanOrEqual, null, -val));
                    else
                         results.Add(new ConditionCell($"B{condId++}", $"시가대비 {val}% 상승 매도", "CHG_OPEN_PCT", ComparisonOperator.GreaterThanOrEqual, null, val));
                }
            }

            // 패턴 5: 이평선 돌파/이탈
            var mMa = Regex.Match(part, @"(\d+)\s*(이평|MA|이동평균선)\s*(돌파|이탈|상향)");
            if (mMa.Success)
            {
                string period = mMa.Groups[1].Value;
                string maName = "MA_" + period;
                string act = mMa.Groups[3].Value;
                var op = (act == "이탈") ? ComparisonOperator.CrossDown : ComparisonOperator.CrossUp;
                results.Add(new ConditionCell($"B{condId++}", $"{period}이평 {act}", "Price", op, maName));
            }

            return results;
        }

        private static ComparisonOperator MapOperator(string text)
        {
            if (text.Contains("돌파") || text.Contains("상향")) return ComparisonOperator.CrossUp;
            if (text.Contains("이탈") || text.Contains("하향")) return ComparisonOperator.CrossDown;
            if (text.Contains("이상") || text.Contains("초과")) return ComparisonOperator.GreaterThanOrEqual;
            if (text.Contains("이하") || text.Contains("미만")) return ComparisonOperator.LessThanOrEqual;
            return ComparisonOperator.GreaterThan;
        }
    }
}
