using System;
using System.Collections.Generic;
using Common.Models;
using Common.Enums;

namespace Common.Interfaces
{
    /// <summary>
    /// 캔들 데이터 제공자 인터페이스
    /// 10% 변형 가능 영역 — 배치/실시간 구현 교체
    /// </summary>
    public interface ICandleProvider
    {
        IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, int count);
        IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, DateTime from, DateTime to);
        void Subscribe(string code, CandleType type, int interval);
        void Unsubscribe(string code, CandleType type, int interval);
    }
}