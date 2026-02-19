using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common.Enums;
using Common.Models;

namespace Bridge
{
    /// <summary>
    /// 초고속 바이너리 직렬화 — JSON 대비 10~50배 빠름
    /// Common.Models의 실제 프로퍼티명/생성자에 정확히 매핑
    /// </summary>
    public static class BinarySerializer
    {
        // ═══════════════════════════════════════════
        //  MarketData (실시간 시세) — readonly struct
        // ═══════════════════════════════════════════

        public static byte[] SerializeMarketData(MarketData md)
        {
            using (var ms = new MemoryStream(128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(md.Code ?? "");
                bw.Write(md.Time.ToBinary());
                bw.Write(md.Price);
                bw.Write(md.Open);
                bw.Write(md.High);
                bw.Write(md.Low);
                bw.Write(md.PrevClose);
                bw.Write(md.Volume);
                bw.Write(md.AccVolume);
                bw.Write(md.AccTradingValue);
                bw.Write(md.BidPrice1);
                bw.Write(md.AskPrice1);
                bw.Write(md.BidQty1);
                bw.Write(md.AskQty1);
                bw.Write(md.StrengthRate);
                return ms.ToArray();
            }
        }

        public static MarketData DeserializeMarketData(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new MarketData(
                    code: br.ReadString(),
                    time: DateTime.FromBinary(br.ReadInt64()),
                    price: br.ReadInt32(),
                    open: br.ReadInt32(),
                    high: br.ReadInt32(),
                    low: br.ReadInt32(),
                    prevClose: br.ReadInt32(),
                    volume: br.ReadInt64(),
                    accVolume: br.ReadInt64(),
                    accTradingValue: br.ReadInt64(),
                    bidPrice1: br.ReadInt32(),
                    askPrice1: br.ReadInt32(),
                    bidQty1: br.ReadInt32(),
                    askQty1: br.ReadInt32(),
                    strengthRate: br.ReadDouble()
                );
            }
        }

        public static byte[] SerializeMarketDataBatch(IReadOnlyList<MarketData> list)
        {
            using (var ms = new MemoryStream(4 + list.Count * 128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var md = list[i];
                    bw.Write(md.Code ?? "");
                    bw.Write(md.Time.ToBinary());
                    bw.Write(md.Price);
                    bw.Write(md.Open);
                    bw.Write(md.High);
                    bw.Write(md.Low);
                    bw.Write(md.PrevClose);
                    bw.Write(md.Volume);
                    bw.Write(md.AccVolume);
                    bw.Write(md.AccTradingValue);
                    bw.Write(md.BidPrice1);
                    bw.Write(md.AskPrice1);
                    bw.Write(md.BidQty1);
                    bw.Write(md.AskQty1);
                    bw.Write(md.StrengthRate);
                }
                return ms.ToArray();
            }
        }

        public static IReadOnlyList<MarketData> DeserializeMarketDataBatch(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                int count = br.ReadInt32();
                var list = new List<MarketData>(count);
                for (int i = 0; i < count; i++)
                {
                    list.Add(new MarketData(
                        code: br.ReadString(),
                        time: DateTime.FromBinary(br.ReadInt64()),
                        price: br.ReadInt32(),
                        open: br.ReadInt32(),
                        high: br.ReadInt32(),
                        low: br.ReadInt32(),
                        prevClose: br.ReadInt32(),
                        volume: br.ReadInt64(),
                        accVolume: br.ReadInt64(),
                        accTradingValue: br.ReadInt64(),
                        bidPrice1: br.ReadInt32(),
                        askPrice1: br.ReadInt32(),
                        bidQty1: br.ReadInt32(),
                        askQty1: br.ReadInt32(),
                        strengthRate: br.ReadDouble()
                    ));
                }
                return list.AsReadOnly();
            }
        }

        // ═══════════════════════════════════════════
        //  CandleData 배치 — readonly struct
        // ═══════════════════════════════════════════

        public static byte[] SerializeCandleBatch(IReadOnlyList<CandleData> candles)
        {
            using (var ms = new MemoryStream(4 + candles.Count * 60))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(candles.Count);
                for (int i = 0; i < candles.Count; i++)
                {
                    var c = candles[i];
                    bw.Write(c.Code ?? "");
                    bw.Write(c.DateTime.ToBinary());
                    bw.Write((int)c.Type);
                    bw.Write(c.Open);
                    bw.Write(c.High);
                    bw.Write(c.Low);
                    bw.Write(c.Close);
                    bw.Write(c.Volume);
                    bw.Write(c.TradingValue);
                    bw.Write(c.TickCount); // [추가]
                }
                return ms.ToArray();
            }
        }

        public static IReadOnlyList<CandleData> DeserializeCandleBatch(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                int count = br.ReadInt32();
                var list = new List<CandleData>(count);
                for (int i = 0; i < count; i++)
                {
                    list.Add(new CandleData(
                        code: br.ReadString(),
                        dateTime: DateTime.FromBinary(br.ReadInt64()),
                        type: (CandleType)br.ReadInt32(),
                        open: br.ReadInt32(),
                        high: br.ReadInt32(),
                        low: br.ReadInt32(),
                        close: br.ReadInt32(),
                        volume: br.ReadInt64(),
                        tradingValue: br.ReadInt64(),
                        tickCount: br.ReadInt32() // [추가]
                    ));
                }
                return list.AsReadOnly();
            }
        }

        // ═══════════════════════════════════════════
        //  OrderInfo — sealed class
        // ═══════════════════════════════════════════

        public static byte[] SerializeOrder(OrderInfo o)
        {
            using (var ms = new MemoryStream(256))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(o.OrderNo ?? "");
                bw.Write(o.OrigOrderNo ?? "");
                bw.Write(o.Code ?? "");
                bw.Write(o.Name ?? "");
                bw.Write((int)o.Type);
                bw.Write((int)o.Condition);
                bw.Write((int)o.State);
                bw.Write(o.OrderPrice);
                bw.Write(o.OrderQty);
                bw.Write(o.ExecPrice);
                bw.Write(o.ExecQty);
                bw.Write(o.RemainQty);
                bw.Write(o.OrderTime.ToBinary());
                bw.Write(o.ExecTime.ToBinary());
                bw.Write(o.AccountNo ?? "");
                bw.Write(o.Message ?? "");
                return ms.ToArray();
            }
        }

        public static OrderInfo DeserializeOrder(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new OrderInfo(
                    orderNo: br.ReadString(),
                    origOrderNo: br.ReadString(),
                    code: br.ReadString(),
                    name: br.ReadString(),
                    type: (OrderType)br.ReadInt32(),
                    condition: (OrderCondition)br.ReadInt32(),
                    state: (OrderState)br.ReadInt32(),
                    orderPrice: br.ReadInt32(),
                    orderQty: br.ReadInt32(),
                    execPrice: br.ReadInt32(),
                    execQty: br.ReadInt32(),
                    remainQty: br.ReadInt32(),
                    orderTime: DateTime.FromBinary(br.ReadInt64()),
                    execTime: DateTime.FromBinary(br.ReadInt64()),
                    accountNo: br.ReadString(),
                    message: br.ReadString()
                );
            }
        }

        // ═══════════════════════════════════════════
        //  TradeResult — sealed class
        // ═══════════════════════════════════════════

        public static byte[] SerializeTrade(TradeResult t)
        {
            using (var ms = new MemoryStream(128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(t.TradeNo ?? "");
                bw.Write(t.OrderNo ?? "");
                bw.Write(t.Code ?? "");
                bw.Write(t.Name ?? "");
                bw.Write((int)t.Type);
                bw.Write(t.Price);
                bw.Write(t.Qty);
                bw.Write(t.Amount);
                bw.Write(t.TradeTime.ToBinary());
                return ms.ToArray();
            }
        }

        public static TradeResult DeserializeTrade(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new TradeResult(
                    tradeNo: br.ReadString(),
                    orderNo: br.ReadString(),
                    code: br.ReadString(),
                    name: br.ReadString(),
                    type: (OrderType)br.ReadInt32(),
                    price: br.ReadInt32(),
                    qty: br.ReadInt32(),
                    amount: br.ReadInt64(),
                    tradeTime: DateTime.FromBinary(br.ReadInt64())
                );
            }
        }

        // ═══════════════════════════════════════════
        //  BalanceInfo — sealed class
        // ═══════════════════════════════════════════

        public static byte[] SerializeBalance(BalanceInfo b)
        {
            using (var ms = new MemoryStream(128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(b.Code ?? "");
                bw.Write(b.Name ?? "");
                bw.Write(b.Qty);
                bw.Write(b.AvgPrice);
                bw.Write(b.CurrentPrice);
                bw.Write(b.PurchaseAmount);
                bw.Write(b.EvalAmount);
                bw.Write(b.ProfitLoss);
                bw.Write(b.ProfitRate);
                bw.Write((int)b.Market);
                bw.Write(b.UpdateTime.ToBinary());
                return ms.ToArray();
            }
        }

        public static BalanceInfo DeserializeBalance(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new BalanceInfo(
                    code: br.ReadString(),
                    name: br.ReadString(),
                    qty: br.ReadInt32(),
                    avgPrice: br.ReadInt32(),
                    currentPrice: br.ReadInt32(),
                    purchaseAmount: br.ReadInt64(),
                    evalAmount: br.ReadInt64(),
                    profitLoss: br.ReadInt64(),
                    profitRate: br.ReadDouble(),
                    market: (MarketType)br.ReadInt32(),
                    updateTime: DateTime.FromBinary(br.ReadInt64())
                );
            }
        }

        // ═══════════════════════════════════════════
        //  BalanceInfo 배치
        // ═══════════════════════════════════════════

        public static byte[] SerializeBalanceBatch(IReadOnlyList<BalanceInfo> list)
        {
            using (var ms = new MemoryStream(4 + list.Count * 128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var b = list[i];
                    bw.Write(b.Code ?? "");
                    bw.Write(b.Name ?? "");
                    bw.Write(b.Qty);
                    bw.Write(b.AvgPrice);
                    bw.Write(b.CurrentPrice);
                    bw.Write(b.PurchaseAmount);
                    bw.Write(b.EvalAmount);
                    bw.Write(b.ProfitLoss);
                    bw.Write(b.ProfitRate);
                    bw.Write((int)b.Market);
                    bw.Write(b.UpdateTime.ToBinary());
                }
                return ms.ToArray();
            }
        }

        public static IReadOnlyList<BalanceInfo> DeserializeBalanceBatch(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                int count = br.ReadInt32();
                var list = new List<BalanceInfo>(count);
                for (int i = 0; i < count; i++)
                {
                    list.Add(new BalanceInfo(
                        br.ReadString(), br.ReadString(),
                        br.ReadInt32(), br.ReadInt32(), br.ReadInt32(),
                        br.ReadInt64(), br.ReadInt64(), br.ReadInt64(),
                        br.ReadDouble(), (MarketType)br.ReadInt32(),
                        DateTime.FromBinary(br.ReadInt64())));
                }
                return list.AsReadOnly();
            }
        }

        // ═══════════════════════════════════════════
        //  StockInfo (종목 정보)
        // ═══════════════════════════════════════════

        public static byte[] SerializeStockInfo(StockInfo s)
        {
            using (var ms = new MemoryStream(128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(s.Code ?? "");
                bw.Write(s.Name ?? "");
                bw.Write((int)s.Market);
                bw.Write(s.FaceValue);
                bw.Write(s.ListedShares);
                bw.Write(s.MarketCap);
                bw.Write(s.SectorCode ?? "");
                bw.Write(s.SectorName ?? "");
                bw.Write(s.PriceUnit);
                bw.Write(s.IsSuspended);
                bw.Write(s.IsAdminIssue);
                bw.Write(s.ListedDate.ToBinary());
                return ms.ToArray();
            }
        }

        public static StockInfo DeserializeStockInfo(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new StockInfo(
                    code: br.ReadString(),
                    name: br.ReadString(),
                    market: (MarketType)br.ReadInt32(),
                    faceValue: br.ReadInt32(),
                    listedShares: br.ReadInt64(),
                    marketCap: br.ReadInt64(),
                    sectorCode: br.ReadString(),
                    sectorName: br.ReadString(),
                    priceUnit: br.ReadInt32(),
                    isSuspended: br.ReadBoolean(),
                    isAdminIssue: br.ReadBoolean(),
                    listedDate: DateTime.FromBinary(br.ReadInt64())
                );
            }
        }

        // ═══════════════════════════════════════════
        //  문자열 헬퍼
        // ═══════════════════════════════════════════

        public static byte[] SerializeString(string s)
        {
            return Encoding.UTF8.GetBytes(s ?? "");
        }

        public static string DeserializeString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] SerializeStringArray(string[] arr)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(arr.Length);
                for (int i = 0; i < arr.Length; i++)
                    bw.Write(arr[i] ?? "");
                return ms.ToArray();
            }
        }

        public static string[] DeserializeStringArray(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                int count = br.ReadInt32();
                var arr = new string[count];
                for (int i = 0; i < count; i++)
                    arr[i] = br.ReadString();
                return arr;
            }
        }
    }
}