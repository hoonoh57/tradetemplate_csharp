using System;
using System.Collections.Generic;
using Common.Models;

namespace Server32.Kiwoom
{
    public class KiwoomRealtimeReceiver
    {
        private readonly KiwoomConnector _connector;
        private readonly HashSet<string> _subscribedCodes = new HashSet<string>();

        public event Action<MarketData> OnMarketDataReceived;

        public KiwoomRealtimeReceiver(KiwoomConnector connector)
        {
            _connector = connector;
        }

        public void Initialize()
        {
            _connector.OnReceiveRealData += OnReceiveRealData;
        }

        public void Subscribe(string code)
        {
            if (_subscribedCodes.Add(code))
            {
                string fids = "10;11;12;13;14;15;16;17;18;25;27;28;29;30;31;32;228;311;568";
                _connector.SetRealReg("0101", code, fids,
                    _subscribedCodes.Count == 1 ? "0" : "1");
            }
        }

        public void Unsubscribe(string code)
        {
            if (_subscribedCodes.Remove(code))
            {
                _connector.SetRealRemove("0101", code);
            }
        }

        private void OnReceiveRealData(string code, string realType, string realData)
        {
            if (realType == "주식체결" || realType == "주식시세")
            {
                try
                {
                    int rawPrice = GetIntField(10);  // 현재가 (부호 포함)
                    int rawChange = GetIntField(11); // 전일대비 (부호 포함) - 25번보다 11번이 표준

                    int currentPrice = Math.Abs(rawPrice);
                    int prevClose = currentPrice - rawChange; // 전일종가 = 현재가 - 전일대비

                    var md = new MarketData(
                        code: code,
                        time: DateTime.Now,
                        price: currentPrice,
                        open: Math.Abs(GetIntField(16)),
                        high: Math.Abs(GetIntField(17)),
                        low: Math.Abs(GetIntField(18)),
                        prevClose: prevClose,
                        volume: GetLongField(15),
                        accVolume: GetLongField(13),
                        accTradingValue: GetLongField(14) * 1000, // 키움은 누적거래대금이 천원단위일 수 있음 (확인 필요하나 보통 FID 14는 원단위)
                        bidPrice1: Math.Abs(GetIntField(28)),
                        askPrice1: Math.Abs(GetIntField(27)),
                        bidQty1: GetIntField(29),
                        askQty1: GetIntField(30),
                        strengthRate: 0.0
                    );
                    OnMarketDataReceived?.Invoke(md);
                }
                catch { }
            }
        }

        private int GetIntField(int fid)
        {
            string val = _connector.GetCommRealData("", fid);
            if (int.TryParse(val, out int result)) return result;
            return 0;
        }

        private long GetLongField(int fid)
        {
            string val = _connector.GetCommRealData("", fid);
            if (long.TryParse(val, out long result)) return result;
            return 0;
        }
    }
}