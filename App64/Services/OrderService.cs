using System;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Common.Models;

namespace App64.Services
{
    /// <summary>
    /// 주문 서비스 — Server32를 통한 주문 실행
    /// </summary>
    public sealed class OrderService
    {
        private readonly ConnectionService _conn;

        public event Action<OrderInfo> OnOrderResult;
        public event Action<TradeResult> OnTradeResult;

        public OrderService(ConnectionService conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _conn.OnPushReceived += OnPush;
        }

        /// <summary>주문 전송</summary>
        public async Task<OrderInfo> SendOrderAsync(string code, bool buy, int price, int qty,
            string source = "kiwoom")
        {
            if (!_conn.IsConnected)
                throw new InvalidOperationException("서버 미연결");

            string bodyStr = $"{code}|{(buy ? "B" : "S")}|{price}|{qty}|{source}";
            byte[] body = Encoding.UTF8.GetBytes(bodyStr);

            var (respType, respBody) = await _conn.Pipe.RequestAsync(
                MessageTypes.OrderRequest, body, 10000);

            if (respType == MessageTypes.OrderResponse)
            {
                var order = BinarySerializer.DeserializeOrder(respBody);
                OnOrderResult?.Invoke(order);
                return order;
            }
            else if (respType == MessageTypes.ErrorResponse)
            {
                string errMsg = Encoding.UTF8.GetString(respBody);
                throw new Exception($"주문 에러: {errMsg}");
            }

            throw new Exception("알 수 없는 응답");
        }

        /// <summary>주문 정정</summary>
        public async Task<OrderInfo> ModifyOrderAsync(string orderNo, int newPrice, int newQty)
        {
            // 추후 구현: 정정 메시지 타입 추가 필요
            await Task.CompletedTask;
            throw new NotImplementedException("주문 정정 메시지 미구현");
        }

        /// <summary>주문 취소</summary>
        public async Task<OrderInfo> CancelOrderAsync(string orderNo)
        {
            // 추후 구현
            await Task.CompletedTask;
            throw new NotImplementedException("주문 취소 메시지 미구현");
        }

        private void OnPush(ushort msgType, uint seqNo, byte[] body)
        {
            if (msgType == MessageTypes.TradePush)
            {
                try
                {
                    var trade = BinarySerializer.DeserializeTrade(body);
                    OnTradeResult?.Invoke(trade);
                }
                catch { }
            }
            else if (msgType == MessageTypes.OrderResponse)
            {
                try
                {
                    var order = BinarySerializer.DeserializeOrder(body);
                    OnOrderResult?.Invoke(order);
                }
                catch { }
            }
        }
    }
}