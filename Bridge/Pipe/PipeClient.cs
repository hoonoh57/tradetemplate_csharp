using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Bridge.Protocol;
using Common.Modules;

namespace Bridge.Pipe
{
    /// <summary>
    /// Named Pipe 클라이언트 — 64비트 App64에서 실행
    /// 자동 재연결 + 비동기 수신
    /// </summary>
    public sealed class PipeClient : IDisposable
    {
        private readonly PipeConfig _config;
        private NamedPipeClientStream _pipe;
        private readonly CancellationTokenSource _cts;
        private bool _disposed;

        public event Action<MessageHeader, byte[]> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<Exception> OnError;

        public bool IsConnected => _pipe != null && _pipe.IsConnected;

        public PipeClient(PipeConfig config = null)
        {
            _config = config ?? PipeConfig.Default;
            _cts = new CancellationTokenSource();
        }

        public bool Connect()
        {
            try
            {
                _pipe = new NamedPipeClientStream(
                    ".", _config.PipeName, PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                _pipe.Connect(_config.ConnectTimeout);

                LogManager.Instance.Info($"PipeClient connected: {_config.PipeName}");
                OnConnected?.Invoke();

                StartReceiving();
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error("PipeClient connect failed", ex);
                OnError?.Invoke(ex);
                return false;
            }
        }

        /// <summary>자동 재연결 시작 (백그라운드)</summary>
        public void ConnectWithRetry(int retryIntervalMs = 3000)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (!IsConnected)
                    {
                        if (Connect()) return;
                        Thread.Sleep(retryIntervalMs);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            });
        }

        public void Send(byte[] data)
        {
            if (!IsConnected) return;
            try
            {
                _pipe.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error("PipeClient send error", ex);
                OnError?.Invoke(ex);
            }
        }

        /// <summary>타입 안전 메시지 전송</summary>
        public void Send(MessageType type, object payload, ushort seq = 0)
        {
            byte[] data = MessageSerializer.Serialize(type, payload, seq);
            Send(data);
        }

        public void Disconnect()
        {
            try
            {
                _pipe?.Close();
                _pipe = null;
            }
            catch { }
            OnDisconnected?.Invoke();
        }

        private void StartReceiving()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] headerBuf = new byte[MessageHeader.SIZE];
                try
                {
                    while (IsConnected && !_cts.Token.IsCancellationRequested)
                    {
                        int read = ReadExact(_pipe, headerBuf, 0, MessageHeader.SIZE);
                        if (read < MessageHeader.SIZE) break;

                        var header = MessageHeader.FromBytes(headerBuf);
                        byte[] payload = new byte[header.PayloadLength];

                        if (header.PayloadLength > 0)
                        {
                            read = ReadExact(_pipe, payload, 0, header.PayloadLength);
                            if (read < header.PayloadLength) break;
                        }

                        OnMessageReceived?.Invoke(header, payload);
                    }
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        LogManager.Instance.Error("PipeClient receive error", ex);
                }
                finally
                {
                    OnDisconnected?.Invoke();
                    LogManager.Instance.Info("PipeClient disconnected");
                }
            });
        }

        private static int ReadExact(Stream stream, byte[] buf, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buf, offset + total, count - total);
                if (read == 0) break;
                total += read;
            }
            return total;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            Disconnect();
            _cts.Dispose();
        }
    }
}