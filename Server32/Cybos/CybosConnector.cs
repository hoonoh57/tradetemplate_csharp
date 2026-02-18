using System;
using System.Collections.Generic;

namespace Server32.Cybos
{
    /// <summary>
    /// CybosPlus 접속 관리자 — 불변 패턴
    /// COM: CpUtil.CpCybos, CpTrade.CpTdUtil
    /// 관리자 권한 필수
    /// </summary>
    public sealed class CybosConnector
    {
        private readonly dynamic _cpCybos;
        private readonly dynamic _cpTdUtil;

        public CybosConnector()
        {
            try
            {
                _cpCybos = Activator.CreateInstance(Type.GetTypeFromProgID("CpUtil.CpCybos"));
                _cpTdUtil = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTdUtil"));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("CybosPlus COM 초기화 실패. 관리자 권한으로 실행하세요.", ex);
            }
        }

        /// <summary>접속 상태</summary>
        public bool IsConnected
        {
            get
            {
                try { return (int)_cpCybos.IsConnect == 1; }
                catch { return false; }
            }
        }

        /// <summary>주문 초기화 (0=성공)</summary>
        public int InitTrade()
        {
            try { return (int)_cpTdUtil.TradeInit(0); }
            catch { return -1; }
        }

        /// <summary>계좌 목록</summary>
        public string[] GetAccounts()
        {
            try
            {
                object acc = _cpTdUtil.AccountNumber;
                if (acc is Array arr)
                {
                    var list = new List<string>();
                    foreach (var item in arr)
                        list.Add(item?.ToString() ?? "");
                    return list.ToArray();
                }
            }
            catch { }
            return Array.Empty<string>();
        }

        /// <summary>첫 번째 계좌</summary>
        public string GetFirstAccount()
        {
            var accounts = GetAccounts();
            return accounts.Length > 0 ? accounts[0] : "";
        }

        /// <summary>상품관리구분코드</summary>
        public string GetGoodsCode(string account)
        {
            try
            {
                object goods = _cpTdUtil.GoodsList(account, 1); // 1=주식
                if (goods is Array arr && arr.Length > 0)
                    return arr.GetValue(0)?.ToString() ?? "";
            }
            catch { }
            return "";
        }

        /// <summary>남은 조회 카운트 (15초에 60회)</summary>
        public int RemainingCount
        {
            get
            {
                try { return (int)_cpCybos.GetLimitRemainCount(1); }
                catch { return 0; }
            }
        }

        /// <summary>남은 실시간 요청 카운트</summary>
        public int RemainingRealTimeCount
        {
            get
            {
                try { return (int)_cpCybos.GetLimitRemainCount(2); }
                catch { return 0; }
            }
        }

        /// <summary>조회 제한 대기 (남은 카운트가 0이면 대기)</summary>
        public void WaitForRateLimit()
        {
            while (RemainingCount <= 0)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}