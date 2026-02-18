using System;
using Common.Enums;
using Common.Interfaces;
using Common.Models;

namespace Server32.Cybos
{
    /// <summary>
    /// CybosPlus 주문 실행 — Skills §3.4 CpTd0311/0313/0314 준수
    /// </summary>
    public sealed class CybosOrderExecutor : IOrderExecutor
    {
        private readonly string _account;
        private readonly string _goodsCode;

        public CybosOrderExecutor(string account, string goodsCode)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _goodsCode = goodsCode ?? "";
        }

        public OrderInfo SendOrder(string code, OrderType type, OrderCondition cond, int price, int qty)
        {
            try
            {
                dynamic order = Activator.CreateInstance(
                    Type.GetTypeFromProgID("CpTrade.CpTd0311"));

                order.SetInputValue(0, type == OrderType.Sell ? "1" : "2");  // 매매구분
                order.SetInputValue(1, _account);                             // 계좌번호
                order.SetInputValue(2, _goodsCode);                           // 상품관리구분
                order.SetInputValue(3, "A" + code);                           // 종목코드
                order.SetInputValue(4, qty);                                  // 주문수량
                order.SetInputValue(5, cond == OrderCondition.Market ? 0 : price); // 주문단가
                order.SetInputValue(7, "0");                                  // 주문조건: 기본
                order.SetInputValue(8, cond == OrderCondition.Market ? "03" : "01"); // 호가구분

                order.BlockRequest();

                string orderNo = "";
                try { orderNo = order.GetHeaderValue(8)?.ToString() ?? ""; } catch { }

                return new OrderInfo(
                    orderNo: orderNo, stockCode: code,
                    orderType: type, condition: cond,
                    price: price, quantity: qty, filledQuantity: 0,
                    state: string.IsNullOrEmpty(orderNo) ? OrderState.Rejected : OrderState.Submitted,
                    orderTime: DateTime.Now,
                    message: string.IsNullOrEmpty(orderNo) ? "주문 실패" : "OK");
            }
            catch (Exception ex)
            {
                return new OrderInfo(
                    "", code, type, cond, price, qty, 0,
                    OrderState.Rejected, DateTime.Now, $"ERR: {ex.Message}");
            }
        }

        public OrderInfo ModifyOrder(string orderNo, int newPrice, int newQty)
        {
            try
            {
                dynamic modify = Activator.CreateInstance(
                    Type.GetTypeFromProgID("CpTrade.CpTd0313"));

                modify.SetInputValue(1, orderNo);
                modify.SetInputValue(2, _account);
                modify.SetInputValue(5, newQty);
                modify.SetInputValue(6, newPrice);

                modify.BlockRequest();

                return new OrderInfo(
                    orderNo, "", OrderType.Buy, OrderCondition.Limit,
                    newPrice, newQty, 0,
                    OrderState.Submitted, DateTime.Now, "OK");
            }
            catch (Exception ex)
            {
                return new OrderInfo(
                    orderNo, "", OrderType.Buy, OrderCondition.Limit,
                    newPrice, newQty, 0,
                    OrderState.Rejected, DateTime.Now, $"ERR: {ex.Message}");
            }
        }

        public OrderInfo CancelOrder(string orderNo)
        {
            try
            {
                dynamic cancel = Activator.CreateInstance(
                    Type.GetTypeFromProgID("CpTrade.CpTd0314"));

                cancel.SetInputValue(1, orderNo);
                cancel.SetInputValue(2, _account);

                cancel.BlockRequest();

                return new OrderInfo(
                    orderNo, "", OrderType.Buy, OrderCondition.Limit,
                    0, 0, 0,
                    OrderState.Cancelled, DateTime.Now, "OK");
            }
            catch (Exception ex)
            {
                return new OrderInfo(
                    orderNo, "", OrderType.Buy, OrderCondition.Limit,
                    0, 0, 0,
                    OrderState.Rejected, DateTime.Now, $"ERR: {ex.Message}");
            }
        }
    }
}