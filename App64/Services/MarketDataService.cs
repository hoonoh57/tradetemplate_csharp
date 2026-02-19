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

        // 구독 추적용 HashSet (중복 구독 방지)
        private readonly HashSet<string> _subscribedCodes = new HashSet<string>();
        private readonly HashSet<string> _conditionAutoSubs = new HashSet<string>(); // 조건식 자동구독
        private readonly object _subLock = new object();



        public event Action<MarketData> OnMarketDataUpdated;
        public event Action<string> OnLog; // 로그 콜백

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
            if (string.IsNullOrEmpty(code)) return;

            lock (_subLock)
            {
                // 조건식 자동구독 종목이면 수동 구독 생략
                if (_conditionAutoSubs.Contains(code))
                {
                    OnLog?.Invoke($"[실시간] 조건식 자동구독 종목 → 수동 구독 스킵: {code}");
                    _subscribedCodes.Add(code); // 추적 목록에는 등록
                    return;
                }

                if (_subscribedCodes.Contains(code))
                {
                    OnLog?.Invoke($"[실시간] 중복 구독 스킵: {code} (이미 구독 중, 총 {_subscribedCodes.Count}개)");
                    return;
                }
                _subscribedCodes.Add(code);
            }

            OnLog?.Invoke($"[실시간] 신규 구독: {code} (총 {_subscribedCodes.Count}개)");
            var body = BinarySerializer.SerializeString(code);
            await _conn.SendAsync(MessageTypes.RealtimeSubscribe, body);
        }

        public async Task UnsubscribeAsync(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            lock (_subLock)
            {
                if (!_subscribedCodes.Remove(code))
                {
                    OnLog?.Invoke($"[실시간] 해제 스킵: {code} (구독 목록에 없음)");
                    return;
                }
            }

            OnLog?.Invoke($"[실시간] 구독 해제: {code} (잔여 {_subscribedCodes.Count}개)");
            var body = BinarySerializer.SerializeString(code);
            await _conn.SendAsync(MessageTypes.RealtimeUnsubscribe, body);
        }

        public void UnsubscribeAll()
        {
            int prevCount;
            lock (_subLock)
            {
                prevCount = _subscribedCodes.Count;
                _subscribedCodes.Clear();
                _conditionAutoSubs.Clear();
            }
            _lastData.Clear();
            OnLog?.Invoke($"[실시간] 전체 해제: {prevCount}개 구독 제거됨");
        }

        /// <summary>
        /// 조건검색으로 자동 구독된 종목을 등록 (수동 구독 방지용).
        /// 키움 조건식 SendCondition 호출 시 포착된 종목은 자동으로 실시간 등록되므로,
        /// 여기에 등록하면 SubscribeAsync에서 중복 구독을 방지합니다.
        /// </summary>
        public void MarkAsConditionAutoSubscribed(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            lock (_subLock)
            {
                _conditionAutoSubs.Add(code);
                _subscribedCodes.Add(code);
            }
            OnLog?.Invoke($"[실시간] 조건식 자동구독 등록: {code} (총 {_subscribedCodes.Count}개)");
        }

        public MarketData GetLastData(string code)
        {
            _lastData.TryGetValue(code, out var md);
            return md;
        }

        /// <summary>현재 구독 중인 종목코드 목록 (디버깅용)</summary>
        public List<string> GetSubscribedCodes()
        {
            lock (_subLock) { return new List<string>(_subscribedCodes); }
        }

        public int SubscribedCount
        {
            get { lock (_subLock) { return _subscribedCodes.Count; } }
        }
    }
}