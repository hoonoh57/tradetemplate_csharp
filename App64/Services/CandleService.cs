using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Common.Enums;
using Common.Models;

namespace App64.Services
{
    public class CandleService
    {
        private readonly ConnectionService _conn;

        public CandleService(ConnectionService conn)
        {
            _conn = conn;
        }

        public async Task<IReadOnlyList<CandleData>> GetCandlesAsync(
            string code, CandleType type, int interval, int count)
        {
            byte[] body;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(code);
                bw.Write((int)type);
                bw.Write(interval);
                bw.Write(count);
                body = ms.ToArray();
            }

            var resp = await _conn.RequestAsync(MessageTypes.CandleBatchRequest, body, 15000);
            return BinarySerializer.DeserializeCandleBatch(resp.respBody);
        }

        public async Task<Dictionary<string, IReadOnlyList<CandleData>>> GetMultipleCandlesAsync(
            string[] codes, CandleType type, int interval, int count)
        {
            var result = new Dictionary<string, IReadOnlyList<CandleData>>();
            foreach (var code in codes)
            {
                try
                {
                    var candles = await GetCandlesAsync(code, type, interval, count);
                    result[code] = candles;
                    await Task.Delay(300);
                }
                catch { }
            }
            return result;
        }
    }
}