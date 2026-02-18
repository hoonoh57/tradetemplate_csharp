using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 캔들 데이터 서비스 — Server32(Cybos)를 통한 배치 다운로드
    /// </summary>
    public sealed class CandleService
    {
        private readonly ConnectionService _conn;

        public CandleService(ConnectionService conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        }

        /// <summary>캔들 데이터 요청</summary>
        /// <param name="code">종목코드 (6자리)</param>
        /// <param name="chartType">'D'=일, 'W'=주, 'M'=월, 'm'=분, 'T'=틱</param>
        /// <param name="count">요청 개수</param>
        /// <param name="interval">분봉 주기 (분봉일 때만 사용)</param>
        public async Task<IReadOnlyList<CandleData>> GetCandlesAsync(
            string code, char chartType = 'D', int count = 100, int interval = 1)
        {
            if (!_conn.IsConnected)
                throw new InvalidOperationException("서버 미연결");

            string bodyStr = $"{code}|{chartType}|{count}|{interval}";
            byte[] body = Encoding.UTF8.GetBytes(bodyStr);

            var (respType, respBody) = await _conn.Pipe.RequestAsync(
                MessageTypes.CandleBatchRequest, body, 30000); // 배치는 타임아웃 길게

            if (respType == MessageTypes.CandleBatchResponse)
            {
                return BinarySerializer.DeserializeCandleBatch(respBody);
            }
            else if (respType == MessageTypes.ErrorResponse)
            {
                string errMsg = Encoding.UTF8.GetString(respBody);
                throw new Exception($"캔들 조회 에러: {errMsg}");
            }

            return new List<CandleData>().AsReadOnly();
        }

        /// <summary>복수 종목 일봉 일괄 다운로드</summary>
        public async Task<Dictionary<string, IReadOnlyList<CandleData>>> GetMultipleCandlesAsync(
            IEnumerable<string> codes, char chartType = 'D', int count = 100)
        {
            var result = new Dictionary<string, IReadOnlyList<CandleData>>();

            foreach (string code in codes)
            {
                try
                {
                    var candles = await GetCandlesAsync(code, chartType, count);
                    result[code] = candles;
                }
                catch
                {
                    result[code] = new List<CandleData>().AsReadOnly();
                }

                // Cybos 조회 제한 고려: 요청 간 300ms 간격
                await Task.Delay(300);
            }

            return result;
        }
    }
}