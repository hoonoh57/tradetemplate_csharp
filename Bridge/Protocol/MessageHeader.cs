using System;
using System.Runtime.InteropServices;

namespace Bridge.Protocol
{
    /// <summary>
    /// 메시지 헤더 — 모든 메시지 앞에 고정 8바이트
    /// [Type:1][Flags:1][Length:4][Seq:2] = 8 bytes
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MessageHeader
    {
        public readonly MessageType Type;
        public readonly byte Flags;
        public readonly int PayloadLength;
        public readonly ushort Sequence;

        public const int SIZE = 8;

        public MessageHeader(MessageType type, int payloadLength, ushort sequence, byte flags = 0)
        {
            Type = type;
            Flags = flags;
            PayloadLength = payloadLength;
            Sequence = sequence;
        }

        /// <summary>1계층 메시지 여부 (고정 크기 바이너리)</summary>
        public bool IsBinaryStruct => (byte)Type < 0x20;

        /// <summary>2계층 메시지 여부 (Protobuf 배치)</summary>
        public bool IsProtobufBatch => (byte)Type >= 0x20 && (byte)Type < 0x80;

        /// <summary>3계층 메시지 여부 (Protobuf 제어)</summary>
        public bool IsProtobufControl => (byte)Type >= 0x80;

        public static unsafe byte[] ToBytes(MessageHeader header)
        {
            byte[] buf = new byte[SIZE];
            fixed (byte* p = buf)
                *(MessageHeader*)p = header;
            return buf;
        }

        public static unsafe MessageHeader FromBytes(byte[] buf, int offset = 0)
        {
            fixed (byte* p = &buf[offset])
                return *(MessageHeader*)p;
        }
    }
}