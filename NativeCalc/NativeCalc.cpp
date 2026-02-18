#include "NativeCalc.h"
#include <cmath>
#include <algorithm>
#include <cstring>

// ═══════════════════════════════════════════
//  SMA (단순이동평균)
// ═══════════════════════════════════════════
NATIVECALC_API int CalcSMA(const CandleRaw* candles, int count, int period, double* outValues)
{
    if (!candles || !outValues || count <= 0 || period <= 0 || period > count)
        return -1;

    // 첫 period-1 개는 0
    for (int i = 0; i < period - 1; i++)
        outValues[i] = 0.0;

    double sum = 0.0;
    for (int i = 0; i < period; i++)
        sum += candles[i].close;
    outValues[period - 1] = sum / period;

    for (int i = period; i < count; i++)
    {
        sum += candles[i].close - candles[i - period].close;
        outValues[i] = sum / period;
    }
    return count;
}

// ═══════════════════════════════════════════
//  EMA (지수이동평균)
// ═══════════════════════════════════════════
NATIVECALC_API int CalcEMA(const CandleRaw* candles, int count, int period, double* outValues)
{
    if (!candles || !outValues || count <= 0 || period <= 0 || period > count)
        return -1;

    double k = 2.0 / (period + 1);

    // SMA로 초기값
    double sum = 0.0;
    for (int i = 0; i < period; i++)
    {
        sum += candles[i].close;
        outValues[i] = 0.0;
    }
    outValues[period - 1] = sum / period;

    for (int i = period; i < count; i++)
        outValues[i] = candles[i].close * k + outValues[i - 1] * (1.0 - k);

    return count;
}

// ═══════════════════════════════════════════
//  WMA (가중이동평균)
// ═══════════════════════════════════════════
NATIVECALC_API int CalcWMA(const CandleRaw* candles, int count, int period, double* outValues)
{
    if (!candles || !outValues || count <= 0 || period <= 0 || period > count)
        return -1;

    double denom = period * (period + 1) / 2.0;

    for (int i = 0; i < period - 1; i++)
        outValues[i] = 0.0;

    for (int i = period - 1; i < count; i++)
    {
        double wsum = 0.0;
        for (int j = 0; j < period; j++)
            wsum += candles[i - period + 1 + j].close * (j + 1);
        outValues[i] = wsum / denom;
    }
    return count;
}

// ═══════════════════════════════════════════
//  RSI
// ═══════════════════════════════════════════
NATIVECALC_API int CalcRSI(const CandleRaw* candles, int count, int period, double* outValues)
{
    if (!candles || !outValues || count <= 0 || period <= 0 || period >= count)
        return -1;

    for (int i = 0; i < period; i++)
        outValues[i] = 0.0;

    double avgGain = 0.0, avgLoss = 0.0;
    for (int i = 1; i <= period; i++)
    {
        double diff = (double)(candles[i].close - candles[i - 1].close);
        if (diff > 0) avgGain += diff;
        else avgLoss -= diff;
    }
    avgGain /= period;
    avgLoss /= period;

    if (avgLoss == 0.0)
        outValues[period] = 100.0;
    else
        outValues[period] = 100.0 - 100.0 / (1.0 + avgGain / avgLoss);

    for (int i = period + 1; i < count; i++)
    {
        double diff = (double)(candles[i].close - candles[i - 1].close);
        double gain = diff > 0 ? diff : 0.0;
        double loss = diff < 0 ? -diff : 0.0;
        avgGain = (avgGain * (period - 1) + gain) / period;
        avgLoss = (avgLoss * (period - 1) + loss) / period;

        if (avgLoss == 0.0)
            outValues[i] = 100.0;
        else
            outValues[i] = 100.0 - 100.0 / (1.0 + avgGain / avgLoss);
    }
    return count;
}

// ═══════════════════════════════════════════
//  Stochastic
// ═══════════════════════════════════════════
NATIVECALC_API int CalcStochastic(const CandleRaw* candles, int count, int kPeriod, int dPeriod,
                                   double* outK, double* outD)
{
    if (!candles || !outK || !outD || count <= 0 || kPeriod <= 0 || kPeriod > count)
        return -1;

    for (int i = 0; i < kPeriod - 1; i++)
        outK[i] = 0.0;

    for (int i = kPeriod - 1; i < count; i++)
    {
        int32_t highest = candles[i].high;
        int32_t lowest = candles[i].low;
        for (int j = 1; j < kPeriod; j++)
        {
            if (candles[i - j].high > highest) highest = candles[i - j].high;
            if (candles[i - j].low < lowest) lowest = candles[i - j].low;
        }
        double range = (double)(highest - lowest);
        outK[i] = range > 0 ? (candles[i].close - lowest) / range * 100.0 : 0.0;
    }

    // %D = %K의 SMA
    for (int i = 0; i < kPeriod - 1 + dPeriod - 1; i++)
        outD[i] = 0.0;

    for (int i = kPeriod - 1 + dPeriod - 1; i < count; i++)
    {
        double sum = 0.0;
        for (int j = 0; j < dPeriod; j++)
            sum += outK[i - j];
        outD[i] = sum / dPeriod;
    }
    return count;
}

// ═══════════════════════════════════════════
//  CCI
// ═══════════════════════════════════════════
NATIVECALC_API int CalcCCI(const CandleRaw* candles, int count, int period, double* outValues)
{
    if (!candles || !outValues || count <= 0 || period <= 0 || period > count)
        return -1;

    for (int i = 0; i < period - 1; i++)
        outValues[i] = 0.0;

    for (int i = period - 1; i < count; i++)
    {
        // TP = (H+L+C)/3
        double sumTP = 0.0;
        double tps[2048]; // 스택 버퍼
        int n = (std::min)(period, 2048);
        for (int j = 0; j < n; j++)
        {
            double tp = (candles[i - j].high + candles[i - j].low + candles[i - j].close) / 3.0;
            tps[j] = tp;
            sumTP += tp;
        }
        double meanTP = sumTP / n;
        double meanDev = 0.0;
        for (int j = 0; j < n; j++)
            meanDev += std::abs(tps[j] - meanTP);
        meanDev /= n;

        outValues[i] = meanDev > 0 ? (tps[0] - meanTP) / (0.015 * meanDev) : 0.0;
    }
    return count;
}

// ═══════════════════════════════════════════
//  Bollinger Bands
// ═══════════════════════════════════════════
NATIVECALC_API int CalcBollinger(const CandleRaw* candles, int count, int period, double multiplier,
                                  double* outMiddle, double* outUpper, double* outLower)
{
    if (!candles || !outMiddle || !outUpper || !outLower || count <= 0 || period <= 0 || period > count)
        return -1;

    CalcSMA(candles, count, period, outMiddle);

    for (int i = 0; i < period - 1; i++)
    {
        outUpper[i] = 0.0;
        outLower[i] = 0.0;
    }

    for (int i = period - 1; i < count; i++)
    {
        double sumSq = 0.0;
        for (int j = 0; j < period; j++)
        {
            double diff = candles[i - j].close - outMiddle[i];
            sumSq += diff * diff;
        }
        double stddev = std::sqrt(sumSq / period);
        outUpper[i] = outMiddle[i] + multiplier * stddev;
        outLower[i] = outMiddle[i] - multiplier * stddev;
    }
    return count;
}

// ═══════════════════════════════════════════
//  MACD
// ═══════════════════════════════════════════
NATIVECALC_API int CalcMACD(const CandleRaw* candles, int count,
                             int fastPeriod, int slowPeriod, int signalPeriod,
                             double* outMACD, double* outSignal, double* outHistogram)
{
    if (!candles || !outMACD || !outSignal || !outHistogram || count <= 0)
        return -1;
    if (slowPeriod > count || fastPeriod > count)
        return -1;

    double* fastEMA = new double[count];
    double* slowEMA = new double[count];
    CalcEMA(candles, count, fastPeriod, fastEMA);
    CalcEMA(candles, count, slowPeriod, slowEMA);

    for (int i = 0; i < count; i++)
        outMACD[i] = fastEMA[i] - slowEMA[i];

    // Signal = MACD의 EMA
    double sigK = 2.0 / (signalPeriod + 1);
    outSignal[0] = outMACD[slowPeriod - 1];
    for (int i = 0; i < slowPeriod - 1; i++)
        outSignal[i] = 0.0;

    for (int i = slowPeriod; i < count; i++)
        outSignal[i] = outMACD[i] * sigK + outSignal[i - 1] * (1.0 - sigK);

    for (int i = 0; i < count; i++)
        outHistogram[i] = outMACD[i] - outSignal[i];

    delete[] fastEMA;
    delete[] slowEMA;
    return count;
}

// ═══════════════════════════════════════════
//  ATR
// ═══════════════════════════════════════════
NATIVECALC_API int CalcATR(const CandleRaw* candles, int count, int period, double* outValues)
{
    if (!candles || !outValues || count <= 1 || period <= 0 || period >= count)
        return -1;

    outValues[0] = 0.0;

    double sum = 0.0;
    for (int i = 1; i <= period; i++)
    {
        double tr = (double)(candles[i].high - candles[i].low);
        double d1 = std::abs((double)(candles[i].high - candles[i - 1].close));
        double d2 = std::abs((double)(candles[i].low - candles[i - 1].close));
        tr = (std::max)(tr, (std::max)(d1, d2));
        sum += tr;
        outValues[i] = 0.0;
    }
    outValues[period] = sum / period;

    for (int i = period + 1; i < count; i++)
    {
        double tr = (double)(candles[i].high - candles[i].low);
        double d1 = std::abs((double)(candles[i].high - candles[i - 1].close));
        double d2 = std::abs((double)(candles[i].low - candles[i - 1].close));
        tr = (std::max)(tr, (std::max)(d1, d2));
        outValues[i] = (outValues[i - 1] * (period - 1) + tr) / period;
    }
    return count;
}

// ═══════════════════════════════════════════
//  실시간 증분 계산
// ═══════════════════════════════════════════
NATIVECALC_API double CalcSMA_Incremental(double prevSum, double newClose, double oldClose, int period)
{
    return (prevSum - oldClose + newClose) / period;
}

NATIVECALC_API double CalcEMA_Incremental(double prevEMA, double newClose, int period)
{
    double k = 2.0 / (period + 1);
    return newClose * k + prevEMA * (1.0 - k);
}

NATIVECALC_API double CalcRSI_Incremental(double prevAvgGain, double prevAvgLoss,
                                           double newClose, double prevClose, int period)
{
    double diff = newClose - prevClose;
    double gain = diff > 0 ? diff : 0.0;
    double loss = diff < 0 ? -diff : 0.0;
    double avgGain = (prevAvgGain * (period - 1) + gain) / period;
    double avgLoss = (prevAvgLoss * (period - 1) + loss) / period;
    if (avgLoss == 0.0) return 100.0;
    return 100.0 - 100.0 / (1.0 + avgGain / avgLoss);
}

// ═══════════════════════════════════════════
//  버전
// ═══════════════════════════════════════════
NATIVECALC_API const char* GetVersion()
{
    return "NativeCalc v1.0.0 — x64 C++ Indicator Engine";
}