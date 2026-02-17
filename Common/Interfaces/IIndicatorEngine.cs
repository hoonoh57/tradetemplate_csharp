using System.Collections.Generic;
using Common.Models;

namespace Common.Interfaces
{
    /// <summary>
    /// 지표 연산 엔진 인터페이스
    /// 10% 변형 가능 영역 — C++ Native / C# Managed 구현 교체
    /// </summary>
    public interface IIndicatorEngine
    {
        double[] CalcSMA(IReadOnlyList<CandleData> candles, int period);
        double[] CalcEMA(IReadOnlyList<CandleData> candles, int period);
        (double[] macd, double[] signal, double[] histogram) CalcMACD(
            IReadOnlyList<CandleData> candles, int fast, int slow, int signal);
        double[] CalcRSI(IReadOnlyList<CandleData> candles, int period);
        (double[] upper, double[] middle, double[] lower) CalcBollinger(
            IReadOnlyList<CandleData> candles, int period, double deviation);
        double[] CalcVolumePower(IReadOnlyList<CandleData> candles, int period);
    }
}