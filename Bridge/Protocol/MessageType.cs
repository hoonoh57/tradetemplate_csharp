namespace Bridge.Protocol
{
    /// <summary>
    /// 메시지 유형 — 불변 Enum
    /// 1계층: 0x01~0x1F (초고속 바이너리)
    /// 2계층: 0x20~0x7F (Protobuf 배치)
    /// 3계층: 0x80~0xFF (Protobuf 제어)
    /// </summary>
    public enum MessageType : byte
    {
        // ── 1계층: 초고속 실시간 (고정 크기 바이너리 struct) ──
        RealtimeTick        = 0x01,  // 실시간 틱
        RealtimeHoga        = 0x02,  // 실시간 호가
        RealtimeExec        = 0x03,  // 실시간 체결
        RealtimeBalance     = 0x04,  // 잔고 변동
        RealtimeOrderResult = 0x05,  // 주문 결과

        // ── 2계층: 배치 데이터 (Protobuf) ──
        BatchCandleRequest  = 0x20,  // 캔들 요청
        BatchCandleResponse = 0x21,  // 캔들 응답
        BatchProgramTrade   = 0x22,  // 프로그램 순매수
        BatchStrength       = 0x23,  // 체결강도
        BatchSector         = 0x24,  // 주도섹터
        BatchForeign        = 0x25,  // 외국인 매매
        BatchInstitution    = 0x26,  // 기관 매매
        BatchFinancial      = 0x27,  // 재무정보
        BatchNews           = 0x28,  // 뉴스
        BatchStockList      = 0x29,  // 종목 리스트
        BatchCondition      = 0x2A,  // 조건식 결과

        // ── 3계층: 제어 메시지 (Protobuf) ──
        OrderRequest        = 0x80,  // 주문 요청
        OrderModify         = 0x81,  // 주문 정정
        OrderCancel         = 0x82,  // 주문 취소
        SubscribeRequest    = 0x83,  // 실시간 구독 요청
        UnsubscribeRequest  = 0x84,  // 구독 해제
        ConditionSearch     = 0x85,  // 조건식 검색 요청
        ServerStatus        = 0x90,  // 서버 상태
        Heartbeat           = 0xFE,  // 하트비트
        Ack                 = 0xFF,  // 응답 확인
    }
}