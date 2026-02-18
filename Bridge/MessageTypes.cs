namespace Bridge
{
    /// <summary>
    /// Named Pipe 메시지 타입 상수 — 불변(Immutable)
    /// 프로토콜: [MsgType 2B LE][BodyLen 4B LE][SeqNo 4B LE][Body NB]
    /// </summary>
    public static class MessageTypes
    {
        // ── 로그인 ──
        public const ushort LoginRequest       = 0x0001;
        public const ushort LoginResponse      = 0x0002;

        // ── TR 조회 ──
        public const ushort TrRequest          = 0x0010;
        public const ushort TrResponse         = 0x0011;

        // ── 실시간 ──
        public const ushort RealtimeSubscribe  = 0x0020;
        public const ushort RealtimeUnsubscribe = 0x0021;
        public const ushort RealtimePush       = 0x0022;

        // ── 주문 ──
        public const ushort OrderRequest       = 0x0030;
        public const ushort OrderResponse      = 0x0031;
        public const ushort TradePush          = 0x0032;
        public const ushort BalancePush        = 0x0033;

        // ── 조건검색 ──
        public const ushort ConditionRequest   = 0x0040;
        public const ushort ConditionResult    = 0x0041;
        public const ushort ConditionRealtime  = 0x0042;

        // ── 캔들 배치 ──
        public const ushort CandleBatchRequest  = 0x0050;
        public const ushort CandleBatchResponse = 0x0051;

        // ── 종목정보 ──
        public const ushort StockInfoRequest   = 0x0060;
        public const ushort StockInfoResponse  = 0x0061;
        public const ushort StockListRequest   = 0x0062;
        public const ushort StockListResponse  = 0x0063;

        // ── 잔고/예수금 ──
        public const ushort BalanceRequest     = 0x0070;
        public const ushort BalanceResponse    = 0x0071;
        public const ushort DepositRequest     = 0x0072;
        public const ushort DepositResponse    = 0x0073;

        // ── 투자자/프로그램 ──
        public const ushort InvestorRequest    = 0x0080;
        public const ushort InvestorResponse   = 0x0081;
        public const ushort ProgramTradeRequest  = 0x0082;
        public const ushort ProgramTradeResponse = 0x0083;

        // ── 체결강도 ──
        public const ushort TradeStrengthRequest  = 0x0090;
        public const ushort TradeStrengthResponse = 0x0091;

        // ── 주문 극한테스트 ──
        public const ushort OrderTestRequest = 0x00A0;
        public const ushort OrderTestResponse = 0x00A1;

        // ── 시스템 ──
        public const ushort Heartbeat          = 0x00F0;
        public const ushort ErrorResponse      = 0x00FF;

        /// <summary>메시지 헤더 크기 (고정 10바이트)</summary>
        public const int HeaderSize = 10;

        /// <summary>메시지 타입 이름 반환 (디버깅용)</summary>
        public static string GetName(ushort msgType)
        {
            switch (msgType)
            {
                case LoginRequest:        return "LoginReq";
                case LoginResponse:       return "LoginResp";
                case TrRequest:           return "TrReq";
                case TrResponse:          return "TrResp";
                case RealtimeSubscribe:   return "RtSub";
                case RealtimeUnsubscribe: return "RtUnsub";
                case RealtimePush:        return "RtPush";
                case OrderRequest:        return "OrderReq";
                case OrderResponse:       return "OrderResp";
                case TradePush:           return "TradePush";
                case BalancePush:         return "BalPush";
                case ConditionRequest:    return "CondReq";
                case ConditionResult:     return "CondResult";
                case ConditionRealtime:   return "CondRt";
                case CandleBatchRequest:  return "CandleReq";
                case CandleBatchResponse: return "CandleResp";
                case StockInfoRequest:    return "StockReq";
                case StockInfoResponse:   return "StockResp";
                case StockListRequest:    return "ListReq";
                case StockListResponse:   return "ListResp";
                case BalanceRequest:      return "BalReq";
                case BalanceResponse:     return "BalResp";
                case DepositRequest:      return "DepReq";
                case DepositResponse:     return "DepResp";
                case InvestorRequest:     return "InvReq";
                case InvestorResponse:    return "InvResp";
                case ProgramTradeRequest: return "ProgReq";
                case ProgramTradeResponse:return "ProgResp";
                case TradeStrengthRequest:  return "StrReq";
                case TradeStrengthResponse: return "StrResp";
                case Heartbeat:           return "Heartbeat";
                case OrderTestRequest: return "OrdTestReq";
                case OrderTestResponse: return "OrdTestResp";
                case ErrorResponse:       return "Error";
                default:                  return $"Unknown(0x{msgType:X4})";
            }
        }
    }
}