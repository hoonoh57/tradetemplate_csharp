using Common.Models;
using Common.Enums;

namespace Common.Interfaces
{
    /// <summary>
    /// 주문 실행 인터페이스
    /// 10% 변형 가능 영역 — 키움/이베스트 등 구현 교체
    /// </summary>
    public interface IOrderExecutor
    {
        OrderInfo SendOrder(string code, OrderType type, OrderCondition condition, int price, int qty);
        OrderInfo ModifyOrder(string orderNo, int newPrice, int newQty);
        OrderInfo CancelOrder(string orderNo);
    }
}