using System;
using System.IO;
using ProtoBuf;

namespace Bridge.Protocol
{
    /// <summary>
    /// 메시지 직렬화/역직렬화 통합 관리
    /// 1계층: unsafe 바이너리 복사 (zero-copy에 가까운 성능)
    /// 2/3계층: Protobuf 직렬화
    /// </summary>
    public static class MessageSerializer
    {
        /// <summary>메시지를 바이트 배열로 직렬화 (헤더 + 페이로드)</summary>
        public static byte[] Serialize(MessageType type, object payload, ushort seq = 0)
        {
            byte[] payloadBytes;

            if ((byte)type < 0x20)
            {
                // 1계층: 바이너리 struct
                payloadBytes = SerializeBinary(type, payload);
            }
            else
            {
                // 2/3계층: Protobuf
                payloadBytes = SerializeProtobuf(payload);
            }

            var header = new MessageHeader(type, payloadBytes.Length, seq);
            byte[] headerBytes = MessageHeader.ToBytes(header);

            byte[] result = new byte[MessageHeader.SIZE + payloadBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, result, 0, MessageHeader.SIZE);
            Buffer.BlockCopy(payloadBytes, 0, result, MessageHeader.SIZE, payloadBytes.Length);

            return result;
        }

        /// <summary>바이트 배열에서 헤더 읽기</summary>
        public static MessageHeader ReadHeader(byte[] buf, int offset = 0)
        {
            return MessageHeader.FromBytes(buf, offset);
        }

        /// <summary>페이로드 역직렬화</summary>
        public static T Deserialize<T>(MessageHeader header, byte[] buf, int payloadOffset)
        {
            if (header.IsBinaryStruct)
            {
                return BinaryHelper.FromBytes<T>(buf, payloadOffset);
            }
            else
            {
                using (var ms = new MemoryStream(buf, payloadOffset, header.PayloadLength))
                {
                    return Serializer.Deserialize<T>(ms);
                }
            }
        }

        // ── Private helpers ──

        private static byte[] SerializeBinary(MessageType type, object payload)
        {
            switch (type)
            {
                case MessageType.RealtimeTick:
                    return BinaryHelper.ToBytes((TickStruct)payload);
                case MessageType.RealtimeHoga:
                    return BinaryHelper.ToBytes((HogaStruct)payload);
                case MessageType.RealtimeExec:
                case MessageType.RealtimeOrderResult:
                    return BinaryHelper.ToBytes((ExecStruct)payload);
                default:
                    throw new ArgumentException($"Unknown binary type: {type}");
            }
        }

        private static byte[] SerializeProtobuf(object payload)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.NonGeneric.Serialize(ms, payload);
                return ms.ToArray();
            }
        }
    }
}