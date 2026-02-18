using System;
using Common.Enums;
using Common.Models;

namespace Server32.Cybos
{
    public class CybosOrderExecutor
    {
        private readonly CybosConnector _connector;
        private dynamic _cpOrder;
        private dynamic _cpModify;
        private dynamic _cpCancel;
        private string _accountNo = "";

        public CybosOrderExecutor(CybosConnector connector)
        {
            _connector = connector;
        }

        public void Initialize()
        {
            try
            {
                _cpOrder = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0311"));
                _cpModify = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0313"));
                _cpCancel = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0314"));
            }
            catch { }
        }

        public void SetAccount(string accountNo)
        {
            _accountNo = accountNo ?? "";
        }

        public OrderInfo SendOrder(string code, OrderType orderType, int price, int qty)
        {
            if (_cpOrder == null || !_connector.IsConnected)
                return MakeErrorOrder(code, orderType, price, qty, "Cybos 미연결");

            try
            {
                string buySell = orderType == OrderType.Buy ? "2" : "1";

                _cpOrder.SetInputValue(0, buySell);
                _cpOrder.SetInputValue(1, _accountNo);
                _cpOrder.SetInputValue(2, "01");
                _cpOrder.SetInputValue(3, code);
                _cpOrder.SetInputValue(4, qty);
                _cpOrder.SetInputValue(5, price);

                _cpOrder.BlockRequest();

                bool success = (int)_cpOrder.GetDibStatus() == 0;
                string msg = (string)_cpOrder.GetDibMsg1();

                return new OrderInfo(
                    orderNo: success ? _cpOrder.GetHeaderValue(0)?.ToString() ?? "" : "",
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
        }

        public OrderInfo ModifyOrder(string origOrderNo, string code, int price, int qty)
        {
            if (_cpModify == null) return MakeErrorOrder(code, OrderType.Buy, price, qty, "정정 객체 없음");

            try
            {
                _cpModify.SetInputValue(0, origOrderNo);
                _cpModify.SetInputValue(1, _accountNo);
                _cpModify.SetInputValue(2, code);
                _cpModify.SetInputValue(3, qty);
                _cpModify.SetInputValue(4, price);
                _cpModify.BlockRequest();

                bool success = (int)_cpModify.GetDibStatus() == 0;
                string msg = (string)_cpModify.GetDibMsg1();

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
        }

        public OrderInfo CancelOrder(string origOrderNo, string code, int qty)
        {
            if (_cpCancel == null) return MakeErrorOrder(code, OrderType.Sell, 0, qty, "취소 객체 없음");

            try
            {
                _cpCancel.SetInputValue(0, origOrderNo);
                _cpCancel.SetInputValue(1, _accountNo);
                _cpCancel.SetInputValue(2, code);
                _cpCancel.SetInputValue(3, qty);
                _cpCancel.BlockRequest();

                bool success = (int)_cpCancel.GetDibStatus() == 0;
                string msg = (string)_cpCancel.GetDibMsg1();

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