using System;
using System.Collections.Generic;
using Common.Models;

namespace Server32.Cybos
{
    public class CybosRealtimeReceiver
    {
        private readonly CybosConnector _connector;
        private dynamic _stockCur;
        private readonly HashSet<string> _subscribedCodes = new HashSet<string>();

        public event Action<MarketData> OnMarketDataReceived;

        public CybosRealtimeReceiver(CybosConnector connector)
        {
            _connector = connector;
        }

        public void Initialize()
        {
            try
            {
                _stockCur = Activator.CreateInstance(Type.GetTypeFromProgID("DsCbo1.StockCur"));
            }
            catch { }
        }

        public void Subscribe(string code)
        {
            if (_stockCur == null || !_connector.IsConnected) return;
            if (!_subscribedCodes.Add(code)) return;

            try
            {
                _stockCur.SetInputValue(0, code);
                _stockCur.Subscribe();
            }
            catch { }
        }

        public void Unsubscribe(string code)
        {
            if (_stockCur == null) return;
            if (!_subscribedCodes.Remove(code)) return;

            try
            {
                _stockCur.Unsubscribe();
            }
            catch { }
        }

        private void ProcessStockCurData()
        {
            try
            {
                string code = (string)_stockCur.GetHeaderValue(0);
                int price = Math.Abs((int)_stockCur.GetHeaderValue(13));
                int open = Math.Abs((int)_stockCur.GetHeaderValue(4));
                int high = Math.Abs((int)_stockCur.GetHeaderValue(5));
                int low = Math.Abs((int)_stockCur.GetHeaderValue(6));
                long volume = Convert.ToInt64(_stockCur.GetHeaderValue(9));
                long accVolume = Convert.ToInt64(_stockCur.GetHeaderValue(18));

                var md = new MarketData(
                    code: code,
                    time: DateTime.Now,
                    price: price,
                    open: open,
                    high: high,
                    low: low,
                    prevClose: 0,
                    volume: volume,
                    accVolume: accVolume,
                    accTradingValue: 0,
                    bidPrice1: 0,
                    askPrice1: 0,
                    bidQty1: 0,
                    askQty1: 0,
                    strengthRate: 0.0
                );

                OnMarketDataReceived?.Invoke(md);
            }
            catch { }
        }
    }
}