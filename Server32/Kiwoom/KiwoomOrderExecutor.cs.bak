using System;
using Common.Enums;
using Common.Interfaces;
using Common.Models;

namespace Server32.Kiwoom
{
    /// <summary>
    /// 키움 주문 실행 — Skills §2.3 SendOrder 사전 준수
    /// </summary>
    public sealed class KiwoomOrderExecutor : IOrderExecutor
    {
        private readonly KiwoomConnector _conn;
        private string _account;

        public KiwoomOrderExecutor(KiwoomConnector conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _account = conn.GetFirstAccount();
        }

        public void SetAccount(string account) { _account = account; }

        public OrderInfo SendOrder(string code, OrderType type, OrderCondition cond, int price, int qty)
        {
            int orderType = type == OrderType.Buy ? 1 : 2;
            string hogaGb = GetHogaGubun(cond);

            int result = (int)_conn.Api.SendOrder(
                "주문", "3001", _account, orderType, code, qty, price, hogaGb, "");

            return new OrderInfo(
                orderNo: "", stockCode: code, orderType: type, condition: cond,
                price: price, quantity: qty, filledQuantity: 0,
                state: result == 0 ? OrderState.Submitted : OrderState.Rejected,
                orderTime: DateTime.Now,
                message: result == 0 ? "OK" : $"ERR:{result}");
        }

        public OrderInfo ModifyOrder(string orderNo, int newPrice, int newQty)
        {
            int result = (int)_conn.Api.SendOrder(
                "정정", "3002", _account, 5, "", newQty, newPrice, "00", orderNo);

            return new OrderInfo(
                orderNo, "", OrderType.Buy, OrderCondition.Limit,
                newPrice, newQty, 0,
                result == 0 ? OrderState.Submitted : OrderState.Rejected,
                DateTime.Now, result == 0 ? "OK" : $"ERR:{result}");
        }

        public OrderInfo CancelOrder(string orderNo)
        {
            int result = (int)_conn.Api.SendOrder(
                "취소", "3003", _account, 3, "", 0, 0, "00", orderNo);

            return new OrderInfo(
                orderNo, "", OrderType.Buy, OrderCondition.Limit,
                0, 0, 0,
                result == 0 ? OrderState.Cancelled : OrderState.Rejected,
                DateTime.Now, result == 0 ? "OK" : $"ERR:{result}");
        }

        private static string GetHogaGubun(OrderCondition cond)
        {
            switch (cond)
            {
                case OrderCondition.Market: return "03";
                case OrderCondition.Limit:  return "00";
                case OrderCondition.Best:   return "06";
                default: return "00";
            }
        }
    }
}