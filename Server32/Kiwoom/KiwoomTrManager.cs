using System;
using System.Collections.Generic;
using Common.Models;

namespace Server32.Kiwoom
{
    /// <summary>
    /// 키움 TR 요청 관리자 — 조회 제한(초당 5회) 자동 관리
    /// </summary>
    public sealed class KiwoomTrManager
    {
        private readonly KiwoomConnector _conn;
        private readonly Queue<DateTime> _requestTimes = new Queue<DateTime>();
        private const int MaxPerSecond = 5;

        public KiwoomTrManager(KiwoomConnector conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        }

        /// <summary>조회 제한 체크 후 대기</summary>
        private void WaitForRateLimit()
        {
            while (_requestTimes.Count >= MaxPerSecond)
            {
                var oldest = _requestTimes.Peek();
                double elapsed = (DateTime.Now - oldest).TotalMilliseconds;
                if (elapsed < 1000)
                {
                    System.Threading.Thread.Sleep((int)(1000 - elapsed) + 50);
                }
                else
                {
                    _requestTimes.Dequeue();
                }
            }
            _requestTimes.Enqueue(DateTime.Now);
        }

        /// <summary>opt10001 주식기본정보 요청</summary>
        public void RequestStockInfo(string stockCode, string screenNo = "1001")
        {
            WaitForRateLimit();
            _conn.Api.SetInputValue("종목코드", stockCode);
            _conn.Api.CommRqData("주식기본정보", "opt10001", 0, screenNo);
        }

        /// <summary>opt10081 주식일봉차트 요청</summary>
        public void RequestDailyCandle(string stockCode, string baseDate, string screenNo = "2001", int prevNext = 0)
        {
            WaitForRateLimit();
            _conn.Api.SetInputValue("종목코드", stockCode);
            _conn.Api.SetInputValue("기준일자", baseDate);
            _conn.Api.SetInputValue("수정주가구분", "1");
            _conn.Api.CommRqData("주식일봉차트", "opt10081", prevNext, screenNo);
        }

        /// <summary>opt10080 주식분봉차트 요청</summary>
        public void RequestMinuteCandle(string stockCode, int tickRange = 1, string screenNo = "2002", int prevNext = 0)
        {
            WaitForRateLimit();
            _conn.Api.SetInputValue("종목코드", stockCode);
            _conn.Api.SetInputValue("틱범위", tickRange.ToString());
            _conn.Api.SetInputValue("수정주가구분", "1");
            _conn.Api.CommRqData("주식분봉차트", "opt10080", prevNext, screenNo);
        }

        /// <summary>opw00018 계좌평가잔고내역 요청</summary>
        public void RequestBalance(string account, string password = "", string screenNo = "4001")
        {
            WaitForRateLimit();
            _conn.Api.SetInputValue("계좌번호", account);
            _conn.Api.SetInputValue("비밀번호", password);
            _conn.Api.SetInputValue("비밀번호입력매체구분", "00");
            _conn.Api.SetInputValue("조회구분", "1");
            _conn.Api.CommRqData("계좌평가잔고", "opw00018", 0, screenNo);
        }

        /// <summary>GetCommData 래퍼</summary>
        public string GetData(string trCode, string recordName, int index, string fieldName)
        {
            return _conn.Api.GetCommData(trCode, recordName, index, fieldName)?.ToString().Trim() ?? "";
        }

        /// <summary>GetRepeatCnt 래퍼</summary>
        public int GetRepeatCount(string trCode, string recordName)
        {
            return (int)_conn.Api.GetRepeatCnt(trCode, recordName);
        }

        /// <summary>안전한 정수 파싱</summary>
        public static int ParseInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            string clean = s.Trim().Replace("+", "").Replace("-", "").Replace(",", "");
            return int.TryParse(clean, out int v) ? (s.Trim().StartsWith("-") ? -v : v) : 0;
        }

        /// <summary>안전한 long 파싱</summary>
        public static long ParseLong(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            string clean = s.Trim().Replace("+", "").Replace("-", "").Replace(",", "");
            return long.TryParse(clean, out long v) ? (s.Trim().StartsWith("-") ? -v : v) : 0;
        }
    }
}