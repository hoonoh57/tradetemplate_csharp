using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Bridge.Protocol
{
    /// <summary>
    /// 2계층/3계층: Protobuf 메시지 정의
    /// 배치 데이터와 제어 메시지에 사용
    /// 모든 메시지는 불변 원칙을 따르되, Protobuf 직렬화를 위해
    /// private set 사용 (역직렬화 시에만 setter 호출)
    /// </summary>

    // ── 2계층: 배치 데이터 ──

    [ProtoContract]
    public sealed class ProtoCandleRequest
    {
        [ProtoMember(1)] public string Code { get; private set; }
        [ProtoMember(2)] public int CandleType { get; private set; }
        [ProtoMember(3)] public int Interval { get; private set; }
        [ProtoMember(4)] public int Count { get; private set; }
        [ProtoMember(5)] public long FromTicks { get; private set; }
        [ProtoMember(6)] public long ToTicks { get; private set; }

        private ProtoCandleRequest() { }

        public ProtoCandleRequest(string code, int candleType, int interval,
            int count, DateTime from, DateTime to)
        {
            Code = code; CandleType = candleType; Interval = interval;
            Count = count; FromTicks = from.Ticks; ToTicks = to.Ticks;
        }
    }

    [ProtoContract]
    public sealed class ProtoCandle
    {
        [ProtoMember(1)] public long TimeTicks { get; private set; }
        [ProtoMember(2)] public int Open { get; private set; }
        [ProtoMember(3)] public int High { get; private set; }
        [ProtoMember(4)] public int Low { get; private set; }
        [ProtoMember(5)] public int Close { get; private set; }
        [ProtoMember(6)] public long Volume { get; private set; }
        [ProtoMember(7)] public long TradingValue { get; private set; }

        private ProtoCandle() { }

        public ProtoCandle(DateTime time, int open, int high, int low,
            int close, long volume, long tradingValue)
        {
            TimeTicks = time.Ticks; Open = open; High = high; Low = low;
            Close = close; Volume = volume; TradingValue = tradingValue;
        }

        public DateTime GetTime() => new DateTime(TimeTicks);
    }

    [ProtoContract]
    public sealed class ProtoCandleResponse
    {
        [ProtoMember(1)] public string Code { get; private set; }
        [ProtoMember(2)] public int CandleType { get; private set; }
        [ProtoMember(3)] public int Interval { get; private set; }
        [ProtoMember(4)] public List<ProtoCandle> Candles { get; private set; }

        private ProtoCandleResponse() { Candles = new List<ProtoCandle>(); }

        public ProtoCandleResponse(string code, int candleType, int interval,
            List<ProtoCandle> candles)
        {
            Code = code; CandleType = candleType; Interval = interval;
            Candles = candles ?? new List<ProtoCandle>();
        }
    }

    [ProtoContract]
    public sealed class ProtoProgramTrade
    {
        [ProtoMember(1)] public string Code { get; private set; }
        [ProtoMember(2)] public long TimeTicks { get; private set; }
        [ProtoMember(3)] public long BuyAmount { get; private set; }
        [ProtoMember(4)] public long SellAmount { get; private set; }
        [ProtoMember(5)] public long NetAmount { get; private set; }

        private ProtoProgramTrade() { }

        public ProtoProgramTrade(string code, DateTime time,
            long buyAmount, long sellAmount, long netAmount)
        {
            Code = code; TimeTicks = time.Ticks;
            BuyAmount = buyAmount; SellAmount = sellAmount; NetAmount = netAmount;
        }
    }

    [ProtoContract]
    public sealed class ProtoStrength
    {
        [ProtoMember(1)] public string Code { get; private set; }
        [ProtoMember(2)] public long TimeTicks { get; private set; }
        [ProtoMember(3)] public double StrengthRate { get; private set; }
        [ProtoMember(4)] public long BuyVolume { get; private set; }
        [ProtoMember(5)] public long SellVolume { get; private set; }

        private ProtoStrength() { }

        public ProtoStrength(string code, DateTime time,
            double strengthRate, long buyVolume, long sellVolume)
        {
            Code = code; TimeTicks = time.Ticks;
            StrengthRate = strengthRate; BuyVolume = buyVolume; SellVolume = sellVolume;
        }
    }

    [ProtoContract]
    public sealed class ProtoBatchResponse
    {
        [ProtoMember(1)] public int RequestType { get; private set; }
        [ProtoMember(2)] public bool Success { get; private set; }
        [ProtoMember(3)] public string ErrorMessage { get; private set; }
        [ProtoMember(4)] public List<ProtoCandleResponse> CandleData { get; private set; }
        [ProtoMember(5)] public List<ProtoProgramTrade> ProgramData { get; private set; }
        [ProtoMember(6)] public List<ProtoStrength> StrengthData { get; private set; }

        private ProtoBatchResponse()
        {
            CandleData = new List<ProtoCandleResponse>();
            ProgramData = new List<ProtoProgramTrade>();
            StrengthData = new List<ProtoStrength>();
        }

        public ProtoBatchResponse(int requestType, bool success, string errorMessage = "")
        {
            RequestType = requestType; Success = success;
            ErrorMessage = errorMessage ?? "";
            CandleData = new List<ProtoCandleResponse>();
            ProgramData = new List<ProtoProgramTrade>();
            StrengthData = new List<ProtoStrength>();
        }
    }

    // ── 3계층: 제어 메시지 ──

    [ProtoContract]
    public sealed class ProtoOrderRequest
    {
        [ProtoMember(1)] public string Code { get; private set; }
        [ProtoMember(2)] public int OrderType { get; private set; }
        [ProtoMember(3)] public int Condition { get; private set; }
        [ProtoMember(4)] public int Price { get; private set; }
        [ProtoMember(5)] public int Qty { get; private set; }
        [ProtoMember(6)] public string AccountNo { get; private set; }

        private ProtoOrderRequest() { }

        public ProtoOrderRequest(string code, int orderType, int condition,
            int price, int qty, string accountNo)
        {
            Code = code; OrderType = orderType; Condition = condition;
            Price = price; Qty = qty; AccountNo = accountNo ?? "";
        }
    }

    [ProtoContract]
    public sealed class ProtoSubscribeRequest
    {
        [ProtoMember(1)] public List<string> Codes { get; private set; }
        [ProtoMember(2)] public bool Subscribe { get; private set; }
        [ProtoMember(3)] public int DataType { get; private set; }

        private ProtoSubscribeRequest() { Codes = new List<string>(); }

        public ProtoSubscribeRequest(List<string> codes, bool subscribe, int dataType)
        {
            Codes = codes ?? new List<string>();
            Subscribe = subscribe; DataType = dataType;
        }
    }

    [ProtoContract]
    public sealed class ProtoServerStatus
    {
        [ProtoMember(1)] public int ConnectionState { get; private set; }
        [ProtoMember(2)] public string ServerName { get; private set; }
        [ProtoMember(3)] public long TimeTicks { get; private set; }
        [ProtoMember(4)] public int ActiveSubscriptions { get; private set; }
        [ProtoMember(5)] public long MessageCount { get; private set; }

        private ProtoServerStatus() { }

        public ProtoServerStatus(int state, string name, DateTime time,
            int activeSubs, long msgCount)
        {
            ConnectionState = state; ServerName = name ?? "";
            TimeTicks = time.Ticks; ActiveSubscriptions = activeSubs;
            MessageCount = msgCount;
        }
    }
}