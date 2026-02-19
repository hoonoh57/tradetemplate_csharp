# 전략 엔진 고도화 및 멀티 타임프레임(MTF) 구현 현황 (2026-02-20)

이 문서는 최근 진행된 전략 엔진의 대규모 리팩토링 및 기능 추가 사항을 정리한 것입니다. 향후 개발 세션에서 이 문맥을 참조하여 작업을 이어가시기 바랍니다.

## 1. 개요 (Overview)
사용자의 자연어("10일 중 상승률 10% 이상인 날의 고가 돌파", "손절매 -2%")를 완벽하게 수행하기 위해, 시스템 전반에 걸쳐 **시계열 인텔리전스(Time-Series Intelligence)** 계층을 구축했습니다. 이제 분봉 차트에서도 일봉 데이터를 참조(MTF)할 수 있으며, 가상 포지션을 추적하여 정교한 익절/손절이 가능합니다.

## 2. 아키텍처 변경 사항 (Architecture Changes)

### 2.1 데이터 흐름 (Data Flow)
1.  **자연어 입력**: 사용자가 전략을 자연어로 입력.
2.  **파싱 및 분석 (`StrategyBridge`)**:
    *   Regex를 통해 조건 분석.
    *   MTF 조건("10일 중") 발견 시 `DAILY_HIGH_COND_{일수}_{등락폭}` 형태의 특수 지표 키 생성.
    *   전략 실행에 필요한 과거 데이터 일수(`RequiredDataDays`) 자동 계산.
3.  **데이터 준비 (`FastChart` & Event)**:
    *   전략 적용 시 `RequiredDataDays`가 존재하면 `RequiredDailyDataRequest` 이벤트 발생.
    *   앱(MainForm)에서 비동기로 일봉 데이터(`List<BarData>`) 다운로드 및 반환.
4.  **스냅샷 생성 (`SnapshotService`)**:
    *   외부 일봉 데이터(우선) 또는 분봉 집계 데이터(백업)를 사용하여 `DAILY_HIGH_COND` 지표값 계산.
    *   `VI_UP_99` (VI 발동 임박), `CHG_OPEN_PCT` (시가대비 등락) 등 가상 지표 주입.
5.  **전략 평가 (`StrategyEvaluator` & `StrategyEngine`)**:
    *   `RunHistorical`에서 가상 포지션(`hasPosition`, `entryPrice`)을 추적.
    *   포지션 보유 시 `PROFIT_PCT`(진입가 대비 수익률) 실시간 계산 및 주입.
    *   중복 신호(매수 후 또 매수) 필터링 및 매도 신호 정상화.

## 3. 핵심 컴포넌트 상세 (Component Details)

### 3.1 `StrategyBridge.cs`
- **역할**: 자연어를 전략 객체(`StrategyDefinition`)로 변환.
- **주요 로직**:
  - `ParseConditions`: 정규표현식으로 복합 패턴 인식.
  - **MTF 패턴**: `(\d+)일 중.*(\d+)% 이상.*고가` -> `DAILY_HIGH_COND_{d}_{p}` 지표 매핑.
  - **데이터 요구량**: 전체 조건 검색 후 최대 Lookback 기간을 `RequiredDataDays`에 설정.

### 3.2 `SnapshotService.cs`
- **역할**: `OHLCV` 리스트를 전략 평가용 `MarketSnapshot` 리스트로 변환.
- **주요 로직**:
  - `CreateSnapshots`: `externalDailyContext`(외부 일봉)를 인자로 받아 처리.
  - `ComputeAllDailyValues`:
    - **Case A (외부 데이터)**: 정확한 일봉 데이터를 사용하여 조건 만족 일자 탐색.
    - **Case B (분봉 집계)**: 데이터 부족 시, 현재 분봉을 날짜별로 GroupBy하여 일봉 근사치 생성.
    - **Sliding Window**: 각 시점(Index)마다 과거 N일간의 조건을 만족하는 날들의 고가 중 최댓값(Max High)을 계산하여 현재 봉의 지표로 주입.

### 3.3 `StrategyEvaluator.cs`
- **역할**: 타임라인을 순회하며 전략 평가 및 신호 관리.
- **주요 로직**:
  - **Virtual Position Tracking**:
    - 매수 신호 발생 시: `hasPosition = true`, `entryPrice = Close`.
    - 매도 신호 발생 시: `hasPosition = false`, `entryPrice = 0`.
    - 포지션 보유 중일 때만 `MarketSnapshot`에 `PROFIT_PCT` 값을 계산하여 넣어줌. (이게 있어야 "수익률 -2% 손절" 조건이 작동)
  - **Signal Filtering**:
    - **중복 진입 방지**: `IsBuySignal`이 떠도 이미 `hasPosition`이면 무시.
    - **공매도 방지**: `IsSellSignal`이 떠도 `!hasPosition`이면 무시.

### 3.4 `StrategyEngine.cs`
- **역할**: 단일 시점 또는 범위에 대한 조건 판단.
- **주요 로직**:
  - `EvaluateCell`: `Lookback` 속성 지원. `GetTargetValue`를 통해 N봉 간의 최대값/최소값 등을 조회 가능해짐.

### 3.5 `FastChart.cs`
- **역할**: UI 및 이벤트 중계.
- **주요 로직**:
  - `RequiredDailyDataRequest` 이벤트 정의 (`Func<string, int, Task<List<BarData>>>`).
  - `ApplyStrategy`: `async void`로 변경하여 데이터 로딩 대기(`await`).

## 4. 가상 지표 정의 (Virtual Indicators)
시스템 내부적으로 자동 생성/관리되는 지표들입니다.

| 지표 키(Key) | 설명 | 생성 주체 |
| :--- | :--- | :--- |
| `VI_UP_99` | 당일 시가(또는 지정가) 기준 상방 VI 발동가의 99% 가격 | `SnapshotService` |
| `CHG_OPEN_PCT` | 당일 시가 대비 현재가 등락률 (%) | `SnapshotService` |
| `PROFIT_PCT` | 진입 가격 대비 현재가 수익률 (%) (포지션 보유 시에만 유효) | `StrategyEvaluator` |
| `DAILY_HIGH_COND_{d}_{p}` | 최근 `d`일 중 전일대비 `p`% 이상 상승한 날들의 고가 중 최대값 | `SnapshotService` (MTF 로직) |

## 5. 향후 연동 필요 사항 (Action Items)
다음 세션에서 아래 사항을 진행해야 합니다.

1.  **메인 폼(`MainForm`) 이벤트 핸들러 구현**:
    *   `FastChart` 컨트롤 생성 시 `RequiredDailyDataRequest` 이벤트를 구독해야 함.
    *   핸들러 내부에서 Kiwoom API(또는 DB)를 통해 해당 종목의 일봉 데이터를 `strategy.RequiredDataDays + 여유분` 만큼 요청하여 반환하는 로직 구현 필요.
2.  **실시간(Real-time) 업데이트 검증**:
    *   장중 실시간 수신 시(`UpdateData`)에도 `SnapshotService`가 호출되어 `PROFIT_PCT` 등이 갱신되는지 확인.
    *   단, 실시간 모드에서는 일봉 데이터가 변하지 않으므로 캐싱된 `DAILY_HIGH_COND` 값을 효율적으로 재사용하도록 최적화 고려.

---
**작성일**: 2026-02-20
**작성자**: Antigravity (Agent)
