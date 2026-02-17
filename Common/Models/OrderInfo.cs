using System;
using Common.Enums;

namespace Common.Models
{
    /// <summary>
    /// 주문 정보 — 불변(Immutable)
    /// 주문 상태 변경 시 새 인스턴스 생성 (With 패턴)
    /// </summary>
    public sealed class OrderInfo
    {
        public string OrderNo { get; }
        public string OrigOrderNo { get; }
        public string Code { get; }
        public string Name { get; }
        public OrderType Type { get; }
        public OrderCondition Condition { get; }
        public OrderState State { get; }
        public int OrderPrice { get; }
        public int OrderQty { get; }
        public int ExecPrice { get; }
        public int ExecQty { get; }
        public int RemainQty { get; }
        public DateTime OrderTime { get; }
        public DateTime ExecTime { get; }
        public string AccountNo { get; }
        public string Message { get; }

        public OrderInfo(
            string orderNo, string origOrderNo, string code, string name,
            OrderType type, OrderCondition condition, OrderState state,
            int orderPrice, int orderQty, int execPrice, int execQty, int remainQty,
            DateTime orderTime, DateTime execTime, string accountNo, string message)
        {
            OrderNo = orderNo ?? "";
            OrigOrderNo = origOrderNo ?? "";
            Code = code ?? "";
            Name = name ?? "";
            Type = type;
            Condition = condition;
            State = state;
            OrderPrice = orderPrice;
            OrderQty = orderQty;
            ExecPrice = execPrice;
            ExecQty = execQty;
            RemainQty = remainQty;
            OrderTime = orderTime;
            ExecTime = execTime;
            AccountNo = accountNo ?? "";
            Message = message ?? "";
        }

        public OrderInfo WithState(OrderState newState, int execPrice = 0, int execQty = 0, DateTime? execTime = null)
        {
            return new OrderInfo(
                OrderNo, OrigOrderNo, Code, Name, Type, Condition, newState,
                OrderPrice, OrderQty,
                execPrice != 0 ? execPrice : ExecPrice,
                execQty != 0 ? ExecQty + execQty : ExecQty,
                OrderQty - (ExecQty + execQty),
                OrderTime, execTime ?? ExecTime, AccountNo, Message);
        }

        public override string ToString() =>
            $"[{OrderNo}] {Code} {Type} {State} P:{OrderPrice} Q:{OrderQty} E:{ExecQty}";
    }
}