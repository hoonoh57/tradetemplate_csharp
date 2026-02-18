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
    /// 모든 메서드는 순수 함수 (상태 없음, 불변)
    /// </summary>
    public static class BinarySerializer
    {
        // ═══════════════════════════════════════════
        //  MarketData (실시간 시세)
        // ═══════════════════════════════════════════

        public static byte[] SerializeMarketData(MarketData md)
        {
            using (var ms = new MemoryStream(80))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(md.StockCode ?? "");
                bw.Write(md.Price);
                bw.Write(md.Change);
                bw.Write(md.ChangeRate);
                bw.Write(md.Volume);
                bw.Write(md.High);
                bw.Write(md.Low);
                bw.Write(md.Open);
                bw.Write(md.TradeTime.ToBinary());
                bw.Write(md.AskPrice);
                bw.Write(md.BidPrice);
                return ms.ToArray();
            }
        }

        public static MarketData DeserializeMarketData(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new MarketData(
                    stockCode:  br.ReadString(),
                    price:      br.ReadInt32(),
                    change:     br.ReadInt32(),
                    changeRate: br.ReadSingle(),
                    volume:     br.ReadInt64(),
                    high:       br.ReadInt32(),
                    low:        br.ReadInt32(),
                    open:       br.ReadInt32(),
                    tradeTime:  DateTime.FromBinary(br.ReadInt64()),
                    askPrice:   br.ReadInt32(),
                    bidPrice:   br.ReadInt32()
                );
            }
        }

        // ═══════════════════════════════════════════
        //  CandleData 배치 (차트 데이터)
        // ═══════════════════════════════════════════

        /// <summary>캔들 1개당 36바이트 (DateTime 8 + OHLC 4*4 + Volume 8)</summary>
        public static byte[] SerializeCandleBatch(IReadOnlyList<CandleData> candles)
        {
            // 4 (count) + 36 * N
            using (var ms = new MemoryStream(4 + candles.Count * 36))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(candles.Count);
                for (int i = 0; i < candles.Count; i++)
                {
                    var c = candles[i];
                    bw.Write(c.DateTime.ToBinary());
                    bw.Write(c.Open);
                    bw.Write(c.High);
                    bw.Write(c.Low);
                    bw.Write(c.Close);
                    bw.Write(c.Volume);
                }
                return ms.ToArray();
            }
        }

        public static IReadOnlyList<CandleData> DeserializeCandleBatch(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                int count = br.ReadInt32();
                var list = new List<CandleData>(count);
                for (int i = 0; i < count; i++)
                {
                    list.Add(new CandleData(
                        DateTime.FromBinary(br.ReadInt64()),
                        br.ReadInt32(), br.ReadInt32(),
                        br.ReadInt32(), br.ReadInt32(),
                        br.ReadInt64()));
                }
                return list.AsReadOnly();
            }
        }

        // ═══════════════════════════════════════════
        //  OrderInfo (주문 정보)
        // ═══════════════════════════════════════════

        public static byte[] SerializeOrder(OrderInfo o)
        {
            using (var ms = new MemoryStream(128))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(o.OrderNo ?? "");
                bw.Write(o.StockCode ?? "");
                bw.Write((int)o.OrderType);
                bw.Write((int)o.Condition);
                bw.Write(o.Price);
                bw.Write(o.Quantity);
                bw.Write(o.FilledQuantity);
                bw.Write((int)o.State);
                bw.Write(o.OrderTime.ToBinary());
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
                    orderNo:        br.ReadString(),
                    stockCode:      br.ReadString(),
                    orderType:      (OrderType)br.ReadInt32(),
                    condition:      (OrderCondition)br.ReadInt32(),
                    price:          br.ReadInt32(),
                    quantity:       br.ReadInt32(),
                    filledQuantity: br.ReadInt32(),
                    state:          (OrderState)br.ReadInt32(),
                    orderTime:      DateTime.FromBinary(br.ReadInt64()),
                    message:        br.ReadString()
                );
            }
        }

        // ═══════════════════════════════════════════
        //  TradeResult (체결 정보)
        // ═══════════════════════════════════════════

        public static byte[] SerializeTrade(TradeResult t)
        {
            using (var ms = new MemoryStream(96))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(t.OrderNo ?? "");
                bw.Write(t.StockCode ?? "");
                bw.Write((int)t.TradeType);
                bw.Write(t.Price);
                bw.Write(t.Quantity);
                bw.Write(t.TotalAmount);
                bw.Write(t.TradeTime.ToBinary());
                bw.Write(t.Fee);
                bw.Write(t.Tax);
                return ms.ToArray();
            }
        }

        public static TradeResult DeserializeTrade(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new TradeResult(
                    orderNo:     br.ReadString(),
                    stockCode:   br.ReadString(),
                    tradeType:   (OrderType)br.ReadInt32(),
                    price:       br.ReadInt32(),
                    quantity:    br.ReadInt32(),
                    totalAmount: br.ReadInt64(),
                    tradeTime:   DateTime.FromBinary(br.ReadInt64()),
                    fee:         br.ReadInt64(),
                    tax:         br.ReadInt64()
                );
            }
        }

        // ═══════════════════════════════════════════
        //  BalanceInfo (잔고 정보)
        // ═══════════════════════════════════════════

        public static byte[] SerializeBalance(BalanceInfo b)
        {
            using (var ms = new MemoryStream(96))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(b.StockCode ?? "");
                bw.Write(b.StockName ?? "");
                bw.Write(b.Quantity);
                bw.Write(b.AvgPrice);
                bw.Write(b.CurrentPrice);
                bw.Write(b.ProfitRate);
                bw.Write(b.ProfitAmount);
                bw.Write(b.TotalBuyAmount);
                return ms.ToArray();
            }
        }

        public static BalanceInfo DeserializeBalance(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                return new BalanceInfo(
                    stockCode:      br.ReadString(),
                    stockName:      br.ReadString(),
                    quantity:       br.ReadInt32(),
                    avgPrice:       br.ReadInt32(),
                    currentPrice:   br.ReadInt32(),
                    profitRate:     br.ReadDouble(),
                    profitAmount:   br.ReadInt64(),
                    totalBuyAmount: br.ReadInt64()
                );
            }
        }

        // ═══════════════════════════════════════════
        //  BalanceInfo 배치 (잔고 목록)
        // ═══════════════════════════════════════════

        public static byte[] SerializeBalanceBatch(IReadOnlyList<BalanceInfo> list)
        {
            using (var ms = new MemoryStream(4 + list.Count * 96))
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var b = list[i];
                    bw.Write(b.StockCode ?? "");
                    bw.Write(b.StockName ?? "");
                    bw.Write(b.Quantity);
                    bw.Write(b.AvgPrice);
                    bw.Write(b.CurrentPrice);
                    bw.Write(b.ProfitRate);
                    bw.Write(b.ProfitAmount);
                    bw.Write(b.TotalBuyAmount);
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
                        br.ReadDouble(), br.ReadInt64(), br.ReadInt64()));
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
                    code:          br.ReadString(),
                    name:          br.ReadString(),
                    market:        (MarketType)br.ReadInt32(),
                    faceValue:     br.ReadInt32(),
                    listedShares:  br.ReadInt64(),
                    marketCap:     br.ReadInt64(),
                    sectorCode:    br.ReadString(),
                    sectorName:    br.ReadString(),
                    priceUnit:     br.ReadInt32(),
                    isSuspended:   br.ReadBoolean(),
                    isAdminIssue:  br.ReadBoolean(),
                    listedDate:    DateTime.FromBinary(br.ReadInt64())
                );
            }
        }

        // ═══════════════════════════════════════════
        //  단순 문자열 / 문자열 배열 헬퍼
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