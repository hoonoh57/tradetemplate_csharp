using System;
using System.Runtime.InteropServices;

namespace Bridge.Protocol
{
    /// <summary>
    /// 1계층: 초고속 실시간 데이터용 고정 크기 구조체
    /// unsafe 메모리 복사로 직렬화 비용 = 0
    /// 모든 구조체는 불변(readonly)
    /// </summary>

    /// <summary>실시간 틱 데이터 (48 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct TickStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] Code;        // 종목코드 (8 bytes, ASCII)
        public readonly long TimeTicks;     // DateTime.Ticks
        public readonly int Price;
        public readonly int Open;
        public readonly int High;
        public readonly int Low;
        public readonly int PrevClose;
        public readonly long Volume;

        public const int SIZE = 48;

        public TickStruct(string code, DateTime time, int price,
            int open, int high, int low, int prevClose, long volume)
        {
            Code = new byte[8];
            if (code != null)
            {
                byte[] src = System.Text.Encoding.ASCII.GetBytes(code);
                Array.Copy(src, Code, Math.Min(src.Length, 8));
            }
            TimeTicks = time.Ticks;
            Price = price;
            Open = open;
            High = high;
            Low = low;
            PrevClose = prevClose;
            Volume = volume;
        }

        public string GetCode() => System.Text.Encoding.ASCII.GetString(Code).TrimEnd('\0');
        public DateTime GetTime() => new DateTime(TimeTicks);
    }

    /// <summary>실시간 호가 데이터 (104 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HogaStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] Code;
        public readonly long TimeTicks;

        // 매도호가 5단계
        public readonly int AskPrice1, AskPrice2, AskPrice3, AskPrice4, AskPrice5;
        public readonly int AskQty1, AskQty2, AskQty3, AskQty4, AskQty5;

        // 매수호가 5단계
        public readonly int BidPrice1, BidPrice2, BidPrice3, BidPrice4, BidPrice5;
        public readonly int BidQty1, BidQty2, BidQty3, BidQty4, BidQty5;

        public const int SIZE = 104;

        public string GetCode() => System.Text.Encoding.ASCII.GetString(Code).TrimEnd('\0');
        public DateTime GetTime() => new DateTime(TimeTicks);
    }

    /// <summary>실시간 체결 (40 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct ExecStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] OrderNo;     // 주문번호 (16 bytes, ASCII)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] Code;
        public readonly long TimeTicks;
        public readonly byte OrderType;     // OrderType enum
        public readonly byte State;         // OrderState enum
        public readonly int ExecPrice;
        public readonly int ExecQty;
        public readonly int RemainQty;

        public const int SIZE = 52;

        public string GetOrderNo() => System.Text.Encoding.ASCII.GetString(OrderNo).TrimEnd('\0');
        public string GetCode() => System.Text.Encoding.ASCII.GetString(Code).TrimEnd('\0');
        public DateTime GetTime() => new DateTime(TimeTicks);
    }

    /// <summary>
    /// 바이너리 struct 직렬화 헬퍼
    /// unsafe 포인터로 메모리 직접 복사 — GC 부담 최소
    /// </summary>
    public static class BinaryHelper
    {
        public static unsafe byte[] ToBytes<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            byte[] buf = new byte[size];
            fixed (byte* p = buf)
                *(T*)p = value;
            return buf;
        }

        public static unsafe T FromBytes<T>(byte[] buf, int offset = 0) where T : unmanaged
        {
            fixed (byte* p = &buf[offset])
                return *(T*)p;
        }

        public static unsafe void WriteToBuffer<T>(T value, byte[] buf, int offset) where T : unmanaged
        {
            fixed (byte* p = &buf[offset])
                *(T*)p = value;
        }

        public static unsafe T ReadFromBuffer<T>(byte[] buf, int offset) where T : unmanaged
        {
            fixed (byte* p = &buf[offset])
                return *(T*)p;
        }
    }
}