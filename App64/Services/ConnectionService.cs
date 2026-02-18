using System;
using System.IO;
using System.Threading.Tasks;
using Bridge;

namespace App64.Services
{
    public class ConnectionService : IDisposable
    {
        private readonly PipeClient _client;

        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnLog;
        public event Action<ushort, uint, byte[]> OnPushReceived;

        public bool IsConnected => _client.IsConnected;

        public ConnectionService()
        {
            _client = new PipeClient();
            _client.OnConnectionChanged += connected =>
            {
                OnConnectionChanged?.Invoke(connected);
            };
            _client.OnPushReceived += (msgType, seqNo, body) =>
            {
                OnPushReceived?.Invoke(msgType, seqNo, body);
            };
            _client.OnError += msg =>
            {
                OnLog?.Invoke("[오류] " + msg);
            };
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            OnLog?.Invoke("서버 연결 시도...");
            await _client.ConnectAsync(timeoutMs);
            OnLog?.Invoke("서버 연결 성공");
        }

        public async Task<Tuple<bool, bool>> CheckLoginAsync()
        {
            var resp = await _client.RequestAsync(MessageTypes.LoginRequest);
            using (var ms = new MemoryStream(resp.respBody))
            using (var br = new BinaryReader(ms))
            {
                bool kiwoom = br.ReadBoolean();
                bool cybos = br.ReadBoolean();
                return Tuple.Create(kiwoom, cybos);
            }
        }

        public async Task<uint> SendAsync(ushort msgType, byte[] body = null)
        {
            return await _client.SendAsync(msgType, body);
        }

        public async Task<(ushort respType, byte[] respBody)> RequestAsync(
            ushort msgType, byte[] body = null, int timeoutMs = 10000)
        {
            return await _client.RequestAsync(msgType, body, timeoutMs);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}