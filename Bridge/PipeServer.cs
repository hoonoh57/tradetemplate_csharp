using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge
{
    /// <summary>
    /// Named Pipe 서버 — Server32 (x86) 에서 실행
    /// 단일 클라이언트 접속, 비동기 읽기/쓰기, 자동 재접속
    /// </summary>
    public sealed class PipeServer : IDisposable
    {
        public const string DefaultPipeName = "TradingBridge";

        private readonly string _pipeName;
        private readonly CancellationTokenSource _cts;
        private NamedPipeServerStream _pipe;
        private readonly object _writeLock = new object();
        private volatile bool _isConnected;

        /// <summary>메시지 수신 이벤트: (msgType, seqNo, body)</summary>
        public event Action<ushort, uint, byte[]> OnMessageReceived;

        /// <summary>클라이언트 접속/해제 이벤트</summary>
        public event Action<bool> OnClientConnectionChanged;

        /// <summary>에러 이벤트</summary>
        public event Action<string> OnError;

        public bool IsClientConnected => _isConnected;

        public PipeServer(string pipeName = null)
        {
            _pipeName = pipeName ?? DefaultPipeName;
            _cts = new CancellationTokenSource();
        }

        /// <summary>서버 시작 (백그라운드 루프)</summary>
        public Task StartAsync()
        {
            return Task.Run(() => AcceptLoop(_cts.Token), _cts.Token);
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _pipe = new NamedPipeServerStream(_pipeName,
                        PipeDirection.InOut, 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await _pipe.WaitForConnectionAsync(ct).ConfigureAwait(false);
                    _isConnected = true;
                    OnClientConnectionChanged?.Invoke(true);

                    await ReadLoop(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"PipeServer: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                    OnClientConnectionChanged?.Invoke(false);
                    DisposePipe();
                }

                // 재접속 대기
                if (!ct.IsCancellationRequested)
                    await Task.Delay(500, ct).ConfigureAwait(false);
            }
        }

        private async Task ReadLoop(CancellationToken ct)
        {
            byte[] header = new byte[MessageTypes.HeaderSize];

            while (!ct.IsCancellationRequested && _pipe != null && _pipe.IsConnected)
            {
                int read = await ReadExactAsync(_pipe, header, MessageTypes.HeaderSize, ct)
                    .ConfigureAwait(false);
                if (read < MessageTypes.HeaderSize) break;

                ushort msgType = BitConverter.ToUInt16(header, 0);
                uint bodyLen = BitConverter.ToUInt32(header, 2);
                uint seqNo = BitConverter.ToUInt32(header, 6);

                // 안전장치: 최대 10MB
                if (bodyLen > 10 * 1024 * 1024)
                {
                    OnError?.Invoke($"Body too large: {bodyLen}");
                    break;
                }

                byte[] body = new byte[bodyLen];
                if (bodyLen > 0)
                {
                    int bodyRead = await ReadExactAsync(_pipe, body, (int)bodyLen, ct)
                        .ConfigureAwait(false);
                    if (bodyRead < (int)bodyLen) break;
                }

                try
                {
                    OnMessageReceived?.Invoke(msgType, seqNo, body);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Handler error: {ex.Message}");
                }
            }
        }

        /// <summary>메시지 전송</summary>
        public async Task SendAsync(ushort msgType, uint seqNo, byte[] body)
        {
            if (_pipe == null || !_pipe.IsConnected)
                throw new InvalidOperationException("Pipe not connected");

            byte[] header = new byte[MessageTypes.HeaderSize];
            BitConverter.GetBytes(msgType).CopyTo(header, 0);
            BitConverter.GetBytes((uint)(body?.Length ?? 0)).CopyTo(header, 2);
            BitConverter.GetBytes(seqNo).CopyTo(header, 6);

            // 동기화: 동시 쓰기 방지
            byte[] packet;
            if (body != null && body.Length > 0)
            {
                packet = new byte[MessageTypes.HeaderSize + body.Length];
                Buffer.BlockCopy(header, 0, packet, 0, MessageTypes.HeaderSize);
                Buffer.BlockCopy(body, 0, packet, MessageTypes.HeaderSize, body.Length);
            }
            else
            {
                packet = header;
            }

            await _pipe.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);
            await _pipe.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>에러 응답 전송 헬퍼</summary>
        public Task SendErrorAsync(uint seqNo, string message)
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(message ?? "");
            return SendAsync(MessageTypes.ErrorResponse, seqNo, body);
        }

        private static async Task<int> ReadExactAsync(Stream s, byte[] buf, int count, CancellationToken ct)
        {
            int total = 0;
            while (total < count)
            {
                int n = await s.ReadAsync(buf, total, count - total, ct).ConfigureAwait(false);
                if (n == 0) return total; // 연결 끊김
                total += n;
            }
            return total;
        }

        private void DisposePipe()
        {
            try { _pipe?.Dispose(); } catch { }
            _pipe = null;
        }

        public void Stop()
        {
            _cts.Cancel();
            DisposePipe();
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }
    }
}