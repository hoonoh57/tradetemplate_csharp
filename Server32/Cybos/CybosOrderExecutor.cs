using System;
using Common.Enums;
using Common.Models;

namespace Server32.Cybos
{
    public class CybosOrderExecutor
    {
        private readonly CybosConnector _connector;
        private string _accountNo = "";

        public CybosOrderExecutor(CybosConnector connector)
        {
            _connector = connector;
        }

        public void SetAccount(string accountNo)
        {
            _accountNo = accountNo ?? "";
        }

        public OrderInfo SendOrder(string code, OrderType orderType, int price, int qty)
        {
            if (!_connector.IsConnected)
                return MakeErrorOrder(code, orderType, price, qty, "Cybos 미연결");

            dynamic cpOrder = null;
            try
            {
                cpOrder = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0311"));
                string buySell = orderType == OrderType.Buy ? "2" : "1";

                cpOrder.SetInputValue(0, buySell);
                cpOrder.SetInputValue(1, _accountNo);
                cpOrder.SetInputValue(2, "01");
                cpOrder.SetInputValue(3, code);
                cpOrder.SetInputValue(4, qty);
                cpOrder.SetInputValue(5, price);

                cpOrder.BlockRequest();

                bool success = (int)cpOrder.GetDibStatus() == 0;
                string msg = (string)cpOrder.GetDibMsg1();

                return new OrderInfo(
                    orderNo: success ? cpOrder.GetHeaderValue(0)?.ToString() ?? "" : "",
                    origOrderNo: "",
                    code: code,
                    name: "",
                    type: orderType,
                    condition: OrderCondition.Normal,
                    state: success ? OrderState.Submitted : OrderState.Rejected,
                    orderPrice: price,
                    orderQty: qty,
                    execPrice: 0,
                    execQty: 0,
                    remainQty: qty,
                    orderTime: DateTime.Now,
                    execTime: DateTime.MinValue,
                    accountNo: _accountNo,
                    message: msg ?? (success ? "주문 성공" : "주문 실패"));
            }
            catch (Exception ex)
            {
                return MakeErrorOrder(code, orderType, price, qty, ex.Message);
            }
            finally
            {
                if (cpOrder != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(cpOrder);
            }
        }

        public OrderInfo ModifyOrder(string origOrderNo, string code, int price, int qty)
        {
            dynamic cpModify = null;
            try
            {
                cpModify = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0313"));
                cpModify.SetInputValue(0, origOrderNo);
                cpModify.SetInputValue(1, _accountNo);
                cpModify.SetInputValue(2, code);
                cpModify.SetInputValue(3, qty);
                cpModify.SetInputValue(4, price);
                cpModify.BlockRequest();

                bool success = (int)cpModify.GetDibStatus() == 0;
                string msg = (string)cpModify.GetDibMsg1();

                return new OrderInfo(
                    orderNo: "", origOrderNo: origOrderNo, code: code, name: "",
                    type: OrderType.Buy, condition: OrderCondition.Normal,
                    state: success ? OrderState.Submitted : OrderState.Rejected,
                    orderPrice: price, orderQty: qty,
                    execPrice: 0, execQty: 0, remainQty: qty,
                    orderTime: DateTime.Now, execTime: DateTime.MinValue,
                    accountNo: _accountNo, message: msg ?? "정정 처리");
            }
            catch (Exception ex)
            {
                return MakeErrorOrder(code, OrderType.Buy, price, qty, "정정 오류: " + ex.Message);
            }
            finally
            {
                if (cpModify != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(cpModify);
            }
        }

        public OrderInfo CancelOrder(string origOrderNo, string code, int qty)
        {
            dynamic cpCancel = null;
            try
            {
                cpCancel = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0314"));
                cpCancel.SetInputValue(0, origOrderNo);
                cpCancel.SetInputValue(1, _accountNo);
                cpCancel.SetInputValue(2, code);
                cpCancel.SetInputValue(3, qty);
                cpCancel.BlockRequest();

                bool success = (int)cpCancel.GetDibStatus() == 0;
                string msg = (string)cpCancel.GetDibMsg1();

                return new OrderInfo(
                    orderNo: "", origOrderNo: origOrderNo, code: code, name: "",
                    type: OrderType.Sell, condition: OrderCondition.Normal,
                    state: success ? OrderState.Submitted : OrderState.Rejected,
                    orderPrice: 0, orderQty: qty,
                    execPrice: 0, execQty: 0, remainQty: qty,
                    orderTime: DateTime.Now, execTime: DateTime.MinValue,
                    accountNo: _accountNo, message: msg ?? "취소 처리");
            }
            catch (Exception ex)
            {
                return MakeErrorOrder(code, OrderType.Sell, 0, qty, "취소 오류: " + ex.Message);
            }
            finally
            {
                if (cpCancel != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(cpCancel);
            }
        }

        private OrderInfo MakeErrorOrder(string code, OrderType type, int price, int qty, string msg)
        {
            return new OrderInfo(
                orderNo: "", origOrderNo: "", code: code, name: "",
                type: type, condition: OrderCondition.Normal,
                state: OrderState.Rejected,
                orderPrice: price, orderQty: qty,
                execPrice: 0, execQty: 0, remainQty: qty,
                orderTime: DateTime.Now, execTime: DateTime.MinValue,
                accountNo: _accountNo, message: msg);
        }
    }
}