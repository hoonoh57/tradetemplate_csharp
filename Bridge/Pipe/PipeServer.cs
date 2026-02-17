using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Concurrent;
using Bridge.Protocol;
using Common.Modules;

namespace Bridge.Pipe
{
    /// <summary>
    /// Named Pipe 서버 — 32비트 Server32에서 실행
    /// 비동기 수신 + 콜백 기반 메시지 처리
    /// </summary>
    public sealed class PipeServer : IDisposable
    {
        private readonly PipeConfig _config;
        private readonly ConcurrentDictionary<int, NamedPipeServerStream> _connections;
        private readonly CancellationTokenSource _cts;
        private int _connectionCount;
        private bool _disposed;

        public event Action<MessageHeader, byte[]> OnMessageReceived;
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action<Exception> OnError;

        public bool IsRunning { get; private set; }
        public int ConnectionCount => _connectionCount;

        public PipeServer(PipeConfig config = null)
        {
            _config = config ?? PipeConfig.Default;
            _connections = new ConcurrentDictionary<int, NamedPipeServerStream>();
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            LogManager.Instance.Info($"PipeServer starting: {_config.PipeName}");

            for (int i = 0; i < _config.MaxConnections; i++)
            {
                StartListening(i);
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _cts.Cancel();

            foreach (var kvp in _connections)
            {
                try { kvp.Value.Close(); } catch { }
            }
            _connections.Clear();
            LogManager.Instance.Info("PipeServer stopped");
        }

        /// <summary>모든 연결된 클라이언트에 메시지 전송</summary>
        public void Broadcast(byte[] data)
        {
            foreach (var kvp in _connections)
            {
                try
                {
                    if (kvp.Value.IsConnected)
                        kvp.Value.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    LogManager.Instance.Error($"Broadcast error on conn {kvp.Key}", ex);
                }
            }
        }

        /// <summary>특정 연결에 메시지 전송</summary>
        public void Send(int connectionId, byte[] data)
        {
            if (_connections.TryGetValue(connectionId, out var pipe) && pipe.IsConnected)
            {
                try { pipe.Write(data, 0, data.Length); }
                catch (Exception ex)
                {
                    LogManager.Instance.Error($"Send error on conn {connectionId}", ex);
                }
            }
        }

        private void StartListening(int slotId)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (IsRunning && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var pipe = new NamedPipeServerStream(
                            _config.PipeName,
                            PipeDirection.InOut,
                            _config.MaxConnections,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous,
                            _config.BufferSize, _config.BufferSize);

                        pipe.WaitForConnection();

                        int connId = Interlocked.Increment(ref _connectionCount);
                        _connections[connId] = pipe;
                        OnClientConnected?.Invoke(connId);
                        LogManager.Instance.Info($"Client connected: {connId}");

                        HandleClient(connId, pipe);
                    }
                    catch (Exception ex)
                    {
                        if (IsRunning)
                            OnError?.Invoke(ex);
                    }
                }
            });
        }

        private void HandleClient(int connId, NamedPipeServerStream pipe)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] headerBuf = new byte[MessageHeader.SIZE];
                try
                {
                    while (pipe.IsConnected && IsRunning)
                    {
                        int read = ReadExact(pipe, headerBuf, 0, MessageHeader.SIZE);
                        if (read < MessageHeader.SIZE) break;

                        var header = MessageHeader.FromBytes(headerBuf);
                        byte[] payload = new byte[header.PayloadLength];

                        if (header.PayloadLength > 0)
                        {
                            read = ReadExact(pipe, payload, 0, header.PayloadLength);
                            if (read < header.PayloadLength) break;
                        }

                        OnMessageReceived?.Invoke(header, payload);
                    }
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                        LogManager.Instance.Error($"Client {connId} error", ex);
                }
                finally
                {
                    _connections.TryRemove(connId, out _);
                    try { pipe.Close(); } catch { }
                    OnClientDisconnected?.Invoke(connId);
                    LogManager.Instance.Info($"Client disconnected: {connId}");
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
            Stop();
            _cts.Dispose();
        }
    }
}