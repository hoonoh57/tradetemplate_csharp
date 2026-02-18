using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Server32.Kiwoom
{
    /// <summary>
    /// 주문 시스템 테스트 프로세스
    /// 모의투자 환경에서 주문→접수→체결→잔고 전체 흐름을 검증
    /// </summary>
    public class OrderTestRunner
    {
        private readonly OrderManager _orderManager;
        private readonly KiwoomConnector _connector;
        private readonly string _accountNo;

        private int _testsPassed;
        private int _testsFailed;
        private readonly List<string> _testResults = new List<string>();

        public event Action<string> OnLog;

        public OrderTestRunner(OrderManager orderManager, KiwoomConnector connector, string accountNo)
        {
            _orderManager = orderManager;
            _connector = connector;
            _accountNo = accountNo;
        }

        /// <summary>전체 테스트 순차 실행</summary>
        public async Task RunAllTestsAsync()
        {
            OnLog?.Invoke("╔══════════════════════════════════════════════════════╗");
            OnLog?.Invoke("║       주문 시스템 테스트 시작 (모의투자)             ║");
            OnLog?.Invoke("╚══════════════════════════════════════════════════════╝");

            if (!_connector.IsSimulation)
            {
                OnLog?.Invoke("[테스트] ⚠ 실서버 감지! 모의투자에서만 실행합니다.");
                return;
            }

            var sw = Stopwatch.StartNew();

            // 테스트 1: 시장가 매수 단건
            await RunTest("T01_시장가_매수_단건", Test01_MarketBuySingle);

            // 테스트 2: 지정가 매수 단건
            await RunTest("T02_지정가_매수_단건", Test02_LimitBuySingle);

            // 테스트 3: 지정가 매수 → 취소
            await RunTest("T03_지정가_매수_후_취소", Test03_LimitBuyThenCancel);

            // 테스트 4: 연속 주문 부하 테스트 (5건 연속)
            await RunTest("T04_연속주문_5건", Test04_BurstOrders);

            // 테스트 5: 매도 테스트 (보유종목)
            await RunTest("T05_시장가_매도", Test05_MarketSell);

            // 테스트 6: 쓰로틀링 부하 테스트 (10건 빠른 연속)
            await RunTest("T06_쓰로틀링_부하_10건", Test06_ThrottleStress);

            sw.Stop();

            // 최종 리포트
            OnLog?.Invoke("");
            OnLog?.Invoke("╔══════════════════════════════════════════════════════╗");
            OnLog?.Invoke("║                  테스트 결과 리포트                  ║");
            OnLog?.Invoke("╠══════════════════════════════════════════════════════╣");
            foreach (var r in _testResults)
                OnLog?.Invoke($"║  {r,-50}║");
            OnLog?.Invoke("╠══════════════════════════════════════════════════════╣");
            OnLog?.Invoke($"║  통과: {_testsPassed}  실패: {_testsFailed}  소요: {sw.Elapsed.TotalSeconds:F1}초{"",-17}║");
            OnLog?.Invoke("╚══════════════════════════════════════════════════════╝");

            _orderManager.PrintSummary();
        }

        private async Task RunTest(string name, Func<Task<bool>> test)
        {
            OnLog?.Invoke($"\n▶ [{name}] 시작...");
            try
            {
                bool ok = await test();
                if (ok)
                {
                    _testsPassed++;
                    _testResults.Add($"✔ {name}: PASS");
                    OnLog?.Invoke($"  [{name}] ✔ PASS");
                }
                else
                {
                    _testsFailed++;
                    _testResults.Add($"✘ {name}: FAIL");
                    OnLog?.Invoke($"  [{name}] ✘ FAIL");
                }
            }
            catch (Exception ex)
            {
                _testsFailed++;
                _testResults.Add($"✘ {name}: ERROR — {ex.Message}");
                OnLog?.Invoke($"  [{name}] ✘ ERROR: {ex.Message}");
            }

            await Task.Delay(2000);  // 테스트 간 충분한 간격
        }

        // ══════════════════════════════════════════════
        //  테스트 케이스
        // ══════════════════════════════════════════════

        /// <summary>T01: 시장가 매수 1주</summary>
        private async Task<bool> Test01_MarketBuySingle()
        {
            // 삼성전자 시장가 매수 1주
            var order = await _orderManager.SubmitOrderAsync("005930", 1, 1, 0, "03");
            await WaitForCompletion(order, 15000);

            OnLog?.Invoke($"  결과: 상태={order.Status}, 체결={order.FilledQty}주, 평균가={order.AvgFillPrice:N0}");
            return order.Status == OrderStatus.Filled || order.Status == OrderStatus.Accepted;
        }

        /// <summary>T02: 지정가 매수 1주 (현재가 -5%로 지정 → 체결 안 될 가능성)</summary>
        private async Task<bool> Test02_LimitBuySingle()
        {
            // 현재가 조회
            string priceStr = _connector.GetMasterCodeName("005930");
            // 낮은 가격에 지정가 주문 (체결 안 될 수 있음, 접수만 확인)
            var order = await _orderManager.SubmitOrderAsync("005930", 1, 1, 50000, "00");
            await WaitForStatus(order, OrderStatus.Accepted, 10000);

            OnLog?.Invoke($"  결과: 상태={order.Status}");
            bool accepted = order.Status == OrderStatus.Accepted ||
                           order.Status == OrderStatus.PartialFilled ||
                           order.Status == OrderStatus.Filled;

            // 접수 확인 후 취소
            if (accepted && !string.IsNullOrWhiteSpace(order.KiwoomOrderNo))
            {
                OnLog?.Invoke($"  → 접수 확인, 취소 진행...");
                await Task.Delay(1000);
                await _orderManager.CancelOrderAsync(order.KiwoomOrderNo);
                await Task.Delay(3000);
            }

            return accepted;
        }

        /// <summary>T03: 지정가 매수 → 취소 전체 플로우</summary>
        private async Task<bool> Test03_LimitBuyThenCancel()
        {
            var buyOrder = await _orderManager.SubmitOrderAsync("005930", 1, 1, 50000, "00");
            await WaitForStatus(buyOrder, OrderStatus.Accepted, 10000);

            if (string.IsNullOrWhiteSpace(buyOrder.KiwoomOrderNo))
            {
                OnLog?.Invoke("  원주문번호 미수신");
                return false;
            }

            await Task.Delay(1500);

            var cancelOrder = await _orderManager.CancelOrderAsync(buyOrder.KiwoomOrderNo);
            if (cancelOrder == null) return false;

            await Task.Delay(5000);

            OnLog?.Invoke($"  매수주문 상태={buyOrder.Status}, 취소주문 상태={cancelOrder.Status}");
            return buyOrder.Status == OrderStatus.Cancelled || cancelOrder.Status == OrderStatus.Accepted;
        }

        /// <summary>T04: 5건 연속 주문 (쓰로틀링 동작 확인)</summary>
        private async Task<bool> Test04_BurstOrders()
        {
            var orders = new List<ManagedOrder>();
            var sw = Stopwatch.StartNew();

            // 5건을 빠르게 제출 (쓰로틀러가 자동으로 간격 조절)
            for (int i = 0; i < 5; i++)
            {
                var order = await _orderManager.SubmitOrderAsync("005930", 1, 1, 50000 + i * 100, "00");
                orders.Add(order);
            }

            sw.Stop();
            OnLog?.Invoke($"  5건 제출 소요: {sw.ElapsedMilliseconds}ms");

            await Task.Delay(5000);

            int accepted = 0;
            foreach (var o in orders)
            {
                if (o.Status >= OrderStatus.Accepted) accepted++;
                // 정리: 접수된 것 취소
                if (!string.IsNullOrWhiteSpace(o.KiwoomOrderNo) && o.Status == OrderStatus.Accepted)
                {
                    await Task.Delay(500);
                    await _orderManager.CancelOrderAsync(o.KiwoomOrderNo);
                }
            }

            OnLog?.Invoke($"  접수 성공: {accepted}/5");
            await Task.Delay(3000);
            return accepted >= 3;  // 최소 3건 이상 접수 성공
        }

        /// <summary>T05: 보유종목 시장가 매도 1주</summary>
        private async Task<bool> Test05_MarketSell()
        {
            // T01에서 매수한 삼성전자 매도
            var order = await _orderManager.SubmitOrderAsync("005930", 2, 1, 0, "03");
            await WaitForCompletion(order, 15000);

            OnLog?.Invoke($"  결과: 상태={order.Status}, 체결={order.FilledQty}주");
            return order.Status == OrderStatus.Filled || order.Status == OrderStatus.Accepted;
        }

        /// <summary>T06: 10건 초고속 부하 테스트</summary>
        private async Task<bool> Test06_ThrottleStress()
        {
            var orders = new List<ManagedOrder>();
            var sw = Stopwatch.StartNew();

            // 10건을 최대한 빠르게 (쓰로틀러가 4/초로 제한)
            for (int i = 0; i < 10; i++)
            {
                var order = await _orderManager.SubmitOrderAsync("005930", 1, 1, 48000 + i * 100, "00");
                orders.Add(order);
            }

            sw.Stop();
            OnLog?.Invoke($"  10건 제출 소요: {sw.ElapsedMilliseconds}ms (쓰로틀링 포함)");

            // 예상: 4건/초 → 10건에 약 2.5초 소요
            bool throttleWorked = sw.ElapsedMilliseconds > 1500;
            OnLog?.Invoke($"  쓰로틀링 동작: {(throttleWorked ? "정상" : "미동작")}");

            await Task.Delay(5000);

            int succeeded = 0, failed = 0;
            foreach (var o in orders)
            {
                if (o.Status >= OrderStatus.Accepted) succeeded++;
                else if (o.Status == OrderStatus.Failed || o.Status == OrderStatus.Rejected) failed++;

                // 정리
                if (!string.IsNullOrWhiteSpace(o.KiwoomOrderNo) &&
                    (o.Status == OrderStatus.Accepted || o.Status == OrderStatus.PartialFilled))
                {
                    await Task.Delay(300);
                    await _orderManager.CancelOrderAsync(o.KiwoomOrderNo);
                }
            }

            OnLog?.Invoke($"  성공: {succeeded}/10, 실패: {failed}/10");
            await Task.Delay(5000);
            return throttleWorked && succeeded >= 5;
        }

        // ── 대기 유틸 ──

        private async Task WaitForCompletion(ManagedOrder order, int timeoutMs)
        {
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                if (order.Status == OrderStatus.Filled ||
                    order.Status == OrderStatus.Cancelled ||
                    order.Status == OrderStatus.Rejected ||
                    order.Status == OrderStatus.Failed)
                    return;

                await Task.Delay(200);
                elapsed += 200;
            }
        }

        private async Task WaitForStatus(ManagedOrder order, OrderStatus target, int timeoutMs)
        {
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                if (order.Status >= target) return;
                await Task.Delay(200);
                elapsed += 200;
            }
        }
    }
}
