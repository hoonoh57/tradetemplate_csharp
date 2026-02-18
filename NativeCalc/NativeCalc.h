#pragma once

#ifdef NATIVECALC_EXPORTS
#define NATIVECALC_API __declspec(dllexport)
#else
#define NATIVECALC_API __declspec(dllimport)
#endif

#include <cstdint>

// ═══════════════════════════════════════════
//  캔들 데이터 구조체 (C# CandleData와 1:1 매핑)
// ═══════════════════════════════════════════
#pragma pack(push, 1)
struct CandleRaw
{
    int64_t dateTimeBinary;  // DateTime.ToBinary()
    int32_t open;
    int32_t high;
    int32_t low;
    int32_t close;
    int64_t volume;
};
#pragma pack(pop)

// ═══════════════════════════════════════════
//  지표 계산 결과
// ═══════════════════════════════════════════
#pragma pack(push, 1)
struct IndicatorResult
{
    double value1;   // 주 지표값 (MA, RSI 등)
    double value2;   // 보조값 (Signal, Upper Band 등)
    double value3;   // 보조값 (Lower Band 등)
};
#pragma pack(pop)

extern "C"
{
    // ── 이동평균 ──
    NATIVECALC_API int CalcSMA(const CandleRaw* candles, int count, int period, double* outValues);
    NATIVECALC_API int CalcEMA(const CandleRaw* candles, int count, int period, double* outValues);
    NATIVECALC_API int CalcWMA(const CandleRaw* candles, int count, int period, double* outValues);

    // ── 오실레이터 ──
    NATIVECALC_API int CalcRSI(const CandleRaw* candles, int count, int period, double* outValues);
    NATIVECALC_API int CalcStochastic(const CandleRaw* candles, int count, int kPeriod, int dPeriod,
                                       double* outK, double* outD);
    NATIVECALC_API int CalcCCI(const CandleRaw* candles, int count, int period, double* outValues);

    // ── 볼린저 밴드 ──
    NATIVECALC_API int CalcBollinger(const CandleRaw* candles, int count, int period, double multiplier,
                                      double* outMiddle, double* outUpper, double* outLower);

    // ── MACD ──
    NATIVECALC_API int CalcMACD(const CandleRaw* candles, int count,
                                 int fastPeriod, int slowPeriod, int signalPeriod,
                                 double* outMACD, double* outSignal, double* outHistogram);

    // ── ATR ──
    NATIVECALC_API int CalcATR(const CandleRaw* candles, int count, int period, double* outValues);

    // ── 실시간 증분 계산 (1개 캔들 추가) ──
    NATIVECALC_API double CalcSMA_Incremental(double prevSum, double newClose, double oldClose, int period);
    NATIVECALC_API double CalcEMA_Incremental(double prevEMA, double newClose, int period);
    NATIVECALC_API double CalcRSI_Incremental(double prevAvgGain, double prevAvgLoss,
                                               double newClose, double prevClose, int period);

    // ── 버전 ──
    NATIVECALC_API const char* GetVersion();
}