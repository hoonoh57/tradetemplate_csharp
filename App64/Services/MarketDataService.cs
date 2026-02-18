using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Common.Models;

namespace App64.Services
{
    public class MarketDataService
    {
        private readonly ConnectionService _conn;
        private readonly ConcurrentDictionary<string, MarketData> _lastData
            = new ConcurrentDictionary<string, MarketData>();

        public event Action<MarketData> OnMarketDataUpdated;

        public MarketDataService(ConnectionService conn)
        {
            _conn = conn;
            _conn.OnPushReceived += HandlePush;
        }

        private void HandlePush(ushort msgType, uint seqNo, byte[] body)
        {
            if (msgType == MessageTypes.RealtimePush && body != null && body.Length > 0)
            {
                try
                {
                    var md = BinarySerializer.DeserializeMarketData(body);
                    _lastData[md.Code] = md;
                    OnMarketDataUpdated?.Invoke(md);
                }
                catch { }
            }
        }

        public async Task SubscribeAsync(string code)
        {
            var body = BinarySerializer.SerializeString(code);
            await _conn.SendAsync(MessageTypes.RealtimeSubscribe, body);
        }

        public async Task UnsubscribeAsync(string code)
        {
            var body = BinarySerializer.SerializeString(code);
            await _conn.SendAsync(MessageTypes.RealtimeUnsubscribe, body);
        }

        public void UnsubscribeAll()
        {
            _lastData.Clear();
        }

        public MarketData GetLastData(string code)
        {
            _lastData.TryGetValue(code, out var md);
            return md;
        }

        public int SubscribedCount => _lastData.Count;
    }
}