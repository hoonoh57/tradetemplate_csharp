using System;
using System.Threading.Tasks;
using Bridge;

namespace App64.Services
{
    /// <summary>
    /// Server32와의 Named Pipe 연결 관리
    /// </summary>
    public sealed class ConnectionService : IDisposable
    {
        private PipeClient _pipe;

        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnLog;
        public event Action<ushort, uint, byte[]> OnPushReceived;

        public bool IsConnected => _pipe?.IsConnected ?? false;
        public PipeClient Pipe => _pipe;

        public async Task ConnectAsync(string serverName = ".", int timeoutMs = 5000)
        {
            _pipe = new PipeClient(serverName: serverName);
            _pipe.OnConnectionChanged += connected =>
            {
                OnConnectionChanged?.Invoke(connected);
                if (!connected) OnLog?.Invoke("[PIPE] 연결 끊김");
            };
            _pipe.OnPushReceived += (msgType, seqNo, body) =>
            {
                OnPushReceived?.Invoke(msgType, seqNo, body);
            };
            _pipe.OnError += err => OnLog?.Invoke($"[PIPE ERR] {err}");

            await _pipe.ConnectAsync(timeoutMs);
            OnLog?.Invoke("[PIPE] 서버 연결 성공");
        }

        public async Task<(bool kiwoom, bool cybos)> CheckLoginAsync()
        {
            var (respType, respBody) = await _pipe.RequestAsync(
                MessageTypes.LoginRequest, null, 5000);

            if (respType == MessageTypes.LoginResponse && respBody.Length >= 2)
            {
                return (respBody[0] == 1, respBody[1] == 1);
            }
            return (false, false);
        }

        public void Disconnect()
        {
            _pipe?.Disconnect();
            _pipe?.Dispose();
            _pipe = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}