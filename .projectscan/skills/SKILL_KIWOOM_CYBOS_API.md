# SKILL: Kiwoom + CybosPlus 통합 API 불변 레퍼런스
> Version: 1.0.0 | Updated: 2026-02-18
> 이 파일은 AI가 코드를 생성할 때 참조하는 **불변(Immutable) API 사전**입니다.
> 모든 TR/COM 호출의 Input, Output, 반환 모델, 소켓 프로토콜이 정의되어 있습니다.

---

## 1. 아키텍처 개요

┌─────────────────────────────────────────────────────┐ │ App64 (x64) │ │ DockPanel UI / FastChart / Strategy Managers │ │ ↕ Named Pipe (Protobuf binary) │ ├─────────────────────────────────────────────────────┤ │ Server32 (x86) │ │ ┌──────────────┐ ┌──────────────────────────────┐ │ │ │ KiwoomModule │ │ CybosModule │ │ │ │ (OpenAPI+ COM)│ │ (CybosPlus COM) │ │ │ └──────────────┘ └──────────────────────────────┘ │ │ ↕ 공통 모델 (Common.dll) │ ├─────────────────────────────────────────────────────┤ │ Bridge (AnyCPU) │ │ Protobuf 메시지 정의 / Pipe 서버·클라이언트 │ ├─────────────────────────────────────────────────────┤ │ Common (AnyCPU) │ │ 불변 모델, Enum, Interface, Helper │ └─────────────────────────────────────────────────────┘


---

## 2. 키움 OpenAPI+ 불변 API 사전

### 2.1 접속 및 초기화

| 항목 | 값 |
|------|-----|
| COM ProgID | `KHOpenAPI.KHOpenAPICtrl.1` |
| 로그인 | `CommConnect()` → `OnEventConnect(errCode)` |
| 계좌목록 | `GetLoginInfo("ACCLIST")` → `"계좌1;계좌2;"` |
| 서버구분 | `GetLoginInfo("GetServerGubun")` → `"1"=모의, else=실거래` |
| 접속상태 | `GetConnectState()` → `1=접속, 0=미접속` |
| 조회제한 | 초당 5회 / 분당 같은 TR 1회 |

#### C# 접속 코드 (불변)
```csharp
// === IMMUTABLE: Kiwoom Connection ===
public sealed class KiwoomConnector
{
    private readonly AxKHOpenAPI _api;
    private TaskCompletionSource<int> _loginTcs;

    public KiwoomConnector(AxKHOpenAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _api.OnEventConnect += OnEventConnect;
    }

    public Task<int> LoginAsync()
    {
        _loginTcs = new TaskCompletionSource<int>();
        _api.CommConnect();
        return _loginTcs.Task;
    }

    private void OnEventConnect(object sender, _DKHOpenAPIEvents_OnEventConnectEvent e)
    {
        _loginTcs?.TrySetResult(e.nErrCode);
    }

    public string[] GetAccounts()
    {
        string raw = _api.GetLoginInfo("ACCLIST");
        return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public bool IsConnected => _api.GetConnectState() == 1;
    public bool IsSimulation => _api.GetLoginInfo("GetServerGubun") == "1";
}
VB.NET 접속 코드 (불변)
Copy' === IMMUTABLE: Kiwoom Connection ===
Public NotInheritable Class KiwoomConnector
    Private ReadOnly _api As AxKHOpenAPI
    Private _loginTcs As TaskCompletionSource(Of Integer)

    Public Sub New(api As AxKHOpenAPI)
        If api Is Nothing Then Throw New ArgumentNullException(NameOf(api))
        _api = api
        AddHandler _api.OnEventConnect, AddressOf OnEventConnect
    End Sub

    Public Function LoginAsync() As Task(Of Integer)
        _loginTcs = New TaskCompletionSource(Of Integer)()
        _api.CommConnect()
        Return _loginTcs.Task
    End Function

    Private Sub OnEventConnect(sender As Object, e As _DKHOpenAPIEvents_OnEventConnectEvent)
        _loginTcs?.TrySetResult(e.nErrCode)
    End Sub

    Public ReadOnly Property Accounts As String()
        Get
            Dim raw As String = _api.GetLoginInfo("ACCLIST")
            Return raw.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
        End Get
    End Property

    Public ReadOnly Property IsConnected As Boolean
        Get
            Return _api.GetConnectState() = 1
        End Get
    End Property
End Class
Copy
2.2 주요 TR 사전 — 데이터 조회
opt10001: 주식기본정보요청
구분	항목
Input	종목코드 (6자리)
Output (싱글)	종목코드, 종목명, 결산월, 액면가, 자본금, 상장주식, 신용비율, 연중최고, 연중최저, 시가총액, 시가총액비중, 외인소진률, 대용가, PER, EPS, ROE, PBR, EV, BPS, 매출액, 영업이익, 당기순이익, 250최고, 250최저, 시가, 고가, 저가, 상한가, 하한가, 기준가, 예상체결가, 예상체결수량, 250최고가일, 250최저가일, 현재가, 대비기호, 전일대비, 등락율, 거래량, 거래대비, 액면가단위
반환 모델	Common.Models.StockInfo
Copy// === IMMUTABLE: opt10001 요청/수신 ===
public sealed class KiwoomTR_opt10001
{
    private readonly AxKHOpenAPI _api;

    public KiwoomTR_opt10001(AxKHOpenAPI api) { _api = api; }

    public void Request(string stockCode, string screenNo = "1001")
    {
        _api.SetInputValue("종목코드", stockCode);
        _api.CommRqData("주식기본정보", "opt10001", 0, screenNo);
    }

    public StockInfo Parse(string trCode, string recordName)
    {
        string code = _api.GetCommData(trCode, recordName, 0, "종목코드").Trim();
        string name = _api.GetCommData(trCode, recordName, 0, "종목명").Trim();
        int faceValue = ParseInt(_api.GetCommData(trCode, recordName, 0, "액면가"));
        long listedShares = ParseLong(_api.GetCommData(trCode, recordName, 0, "상장주식"));
        long marketCap = ParseLong(_api.GetCommData(trCode, recordName, 0, "시가총액"));
        // ... 나머지 필드 동일 패턴

        return new StockInfo(
            code: code, name: name,
            market: code.StartsWith("A") ? MarketType.Kospi : MarketType.Kosdaq,
            faceValue: faceValue, listedShares: listedShares, marketCap: marketCap,
            sectorCode: "", sectorName: "", priceUnit: 0,
            isSuspended: false, isAdminIssue: false, listedDate: DateTime.MinValue);
    }

    private static int ParseInt(string s) =>
        int.TryParse(s?.Trim().Replace("+", "").Replace("-", ""), out int v) ? v : 0;
    private static long ParseLong(string s) =>
        long.TryParse(s?.Trim().Replace("+", "").Replace("-", ""), out long v) ? v : 0;
}
Copy
opt10081: 주식일봉차트조회요청
구분	항목
Input	종목코드, 기준일자(YYYYMMDD), 수정주가구분(0/1)
Output (멀티, 최대600)	종목코드, 현재가, 거래량, 거래대금, 일자, 시가, 고가, 저가, 수정주가구분, 수정비율, 대업종구분, 소업종구분, 종목정보, 수정주가이벤트, 전일종가
반환 모델	IReadOnlyList<Common.Models.CandleData>
연속조회	CommRqData 세번째 인자 2 (sPrevNext)
Copy// === IMMUTABLE: opt10081 일봉 배치 다운로드 ===
public sealed class KiwoomTR_opt10081
{
    private readonly AxKHOpenAPI _api;

    public KiwoomTR_opt10081(AxKHOpenAPI api) { _api = api; }

    public void Request(string stockCode, string baseDate, string screenNo = "2001", int prevNext = 0)
    {
        _api.SetInputValue("종목코드", stockCode);
        _api.SetInputValue("기준일자", baseDate);
        _api.SetInputValue("수정주가구분", "1");
        _api.CommRqData("주식일봉차트", "opt10081", prevNext, screenNo);
    }

    public IReadOnlyList<CandleData> Parse(string trCode, string recordName)
    {
        int count = _api.GetRepeatCnt(trCode, recordName);
        var list = new List<CandleData>(count);
        for (int i = 0; i < count; i++)
        {
            string dateStr = _api.GetCommData(trCode, recordName, i, "일자").Trim();
            list.Add(new CandleData(
                dateTime: DateTime.ParseExact(dateStr, "yyyyMMdd", null),
                open:   Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "시가"))),
                high:   Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "고가"))),
                low:    Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "저가"))),
                close:  Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "현재가"))),
                volume: ParseLong(_api.GetCommData(trCode, recordName, i, "거래량"))
            ));
        }
        return list.AsReadOnly();
    }

    private static int ParseInt(string s) =>
        int.TryParse(s?.Trim().Replace("+","").Replace("-",""), out int v) ? v : 0;
    private static long ParseLong(string s) =>
        long.TryParse(s?.Trim().Replace("+","").Replace("-",""), out long v) ? v : 0;
}
Copy
opt10080: 주식분봉차트조회요청
구분	항목
Input	종목코드, 틱범위(1/3/5/10/15/30/45/60), 수정주가구분
Output (멀티, 최대900)	현재가, 거래량, 체결시간, 시가, 고가, 저가, 수정주가구분, 수정비율, 대업종구분, 소업종구분, 종목정보, 수정주가이벤트, 전일종가
반환 모델	IReadOnlyList<CandleData>
Copy// === IMMUTABLE: opt10080 분봉 ===
public sealed class KiwoomTR_opt10080
{
    private readonly AxKHOpenAPI _api;
    public KiwoomTR_opt10080(AxKHOpenAPI api) { _api = api; }

    public void Request(string stockCode, int tickRange = 1, string screenNo = "2002", int prevNext = 0)
    {
        _api.SetInputValue("종목코드", stockCode);
        _api.SetInputValue("틱범위", tickRange.ToString());
        _api.SetInputValue("수정주가구분", "1");
        _api.CommRqData("주식분봉차트", "opt10080", prevNext, screenNo);
    }

    public IReadOnlyList<CandleData> Parse(string trCode, string recordName)
    {
        int count = _api.GetRepeatCnt(trCode, recordName);
        var list = new List<CandleData>(count);
        for (int i = 0; i < count; i++)
        {
            string timeStr = _api.GetCommData(trCode, recordName, i, "체결시간").Trim();
            list.Add(new CandleData(
                dateTime: DateTime.ParseExact(timeStr, "yyyyMMddHHmmss", null),
                open:   Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "시가"))),
                high:   Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "고가"))),
                low:    Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "저가"))),
                close:  Math.Abs(ParseInt(_api.GetCommData(trCode, recordName, i, "현재가"))),
                volume: ParseLong(_api.GetCommData(trCode, recordName, i, "거래량"))
            ));
        }
        return list.AsReadOnly();
    }

    private static int ParseInt(string s) => int.TryParse(s?.Trim().Replace("+","").Replace("-",""), out int v) ? v : 0;
    private static long ParseLong(string s) => long.TryParse(s?.Trim().Replace("+","").Replace("-",""), out long v) ? v : 0;
}
Copy
opt10079: 주식틱차트조회요청
구분	항목
Input	종목코드, 틱범위(1/3/5/10), 수정주가구분
Output	체결시간, 현재가, 전일대비, 거래량, 시가, 고가, 저가, 체결량
반환 모델	IReadOnlyList<CandleData>
2.3 주요 TR 사전 — 주문/잔고
SendOrder: 주문발송
구분	항목
메서드	SendOrder(sRQName, sScreenNo, sAccNo, nOrderType, sCode, nQty, nPrice, sHogaGb, sOrgOrderNo)
nOrderType	1=신규매수, 2=신규매도, 3=매수취소, 4=매도취소, 5=매수정정, 6=매도정정
sHogaGb	"00"=지정가, "03"=시장가, "05"=조건부지정가, "06"=최유리지정가, "07"=최우선지정가, "10"=지정가IOC, "13"=시장가IOC, "16"=최유리IOC, "20"=지정가FOK, "23"=시장가FOK, "26"=최유리FOK, "61"=장전시간외종가, "62"=시간외단일가, "81"=장후시간외종가
반환	int (0=성공, 음수=실패)
이벤트	OnReceiveChejanData(sGubun, nItemCnt, sFIdList) — sGubun "0"=주문/체결, "1"=잔고, "3"=특이신호
Copy// === IMMUTABLE: 키움 주문 ===
public sealed class KiwoomOrderExecutor : IOrderExecutor
{
    private readonly AxKHOpenAPI _api;
    private readonly string _account;

    public KiwoomOrderExecutor(AxKHOpenAPI api, string account)
    {
        _api = api; _account = account;
    }

    public OrderInfo SendOrder(string code, OrderType type, OrderCondition cond, int price, int qty)
    {
        int orderType = type == OrderType.Buy ? 1 : 2;
        string hogaGb = cond == OrderCondition.Market ? "03" : "00";
        int result = _api.SendOrder(
            "주문", "3001", _account, orderType, code, qty, price, hogaGb, "");

        return new OrderInfo(
            orderNo: "", stockCode: code, orderType: type, condition: cond,
            price: price, quantity: qty, filledQuantity: 0,
            state: result == 0 ? OrderState.Submitted : OrderState.Rejected,
            orderTime: DateTime.Now, message: result == 0 ? "OK" : $"ERR:{result}");
    }

    public OrderInfo ModifyOrder(string orderNo, int newPrice, int newQty)
    {
        int result = _api.SendOrder("정정", "3002", _account, 5, "", newQty, newPrice, "00", orderNo);
        return new OrderInfo("", "", OrderType.Buy, OrderCondition.Limit,
            newPrice, newQty, 0,
            result == 0 ? OrderState.Submitted : OrderState.Rejected,
            DateTime.Now, result == 0 ? "OK" : $"ERR:{result}");
    }

    public OrderInfo CancelOrder(string orderNo)
    {
        int result = _api.SendOrder("취소", "3003", _account, 3, "", 0, 0, "00", orderNo);
        return new OrderInfo("", "", OrderType.Buy, OrderCondition.Limit,
            0, 0, 0,
            result == 0 ? OrderState.Cancelled : OrderState.Rejected,
            DateTime.Now, result == 0 ? "OK" : $"ERR:{result}");
    }
}
Copy
체결/잔고 실시간 (OnReceiveChejanData)
FID	항목	타입
9201	계좌번호	string
9203	주문번호	string
9001	종목코드	string
302	종목명	string
900	주문수량	int
901	주문가격	int
902	미체결수량	int
903	체결누계금액	long
904	원주문번호	string
905	주문구분	string
906	매매구분	string
907	매도수구분	string
908	주문/체결시간	string(HHMMSS)
909	체결번호	string
910	체결가	int
911	체결량	int
10	현재가	int
Copy// === IMMUTABLE: 체결/잔고 이벤트 파서 ===
public sealed class ChejanParser
{
    private readonly AxKHOpenAPI _api;

    public ChejanParser(AxKHOpenAPI api) { _api = api; }

    public TradeResult ParseTrade(string sFidList)
    {
        return new TradeResult(
            orderNo:      _api.GetChejanData(9203).Trim(),
            stockCode:    _api.GetChejanData(9001).Trim().Replace("A", ""),
            tradeType:    _api.GetChejanData(905).Trim().Contains("매수")
                          ? OrderType.Buy : OrderType.Sell,
            price:        Math.Abs(ParseInt(_api.GetChejanData(910))),
            quantity:     Math.Abs(ParseInt(_api.GetChejanData(911))),
            totalAmount:  ParseLong(_api.GetChejanData(903)),
            tradeTime:    ParseTime(_api.GetChejanData(908)),
            fee:          0, tax: 0
        );
    }

    public BalanceInfo ParseBalance(string sFidList)
    {
        return new BalanceInfo(
            stockCode:    _api.GetChejanData(9001).Trim().Replace("A", ""),
            stockName:    _api.GetChejanData(302).Trim(),
            quantity:     Math.Abs(ParseInt(_api.GetChejanData(930))),
            avgPrice:     Math.Abs(ParseInt(_api.GetChejanData(931))),
            currentPrice: Math.Abs(ParseInt(_api.GetChejanData(10))),
            profitRate:   0, profitAmount: 0, totalBuyAmount: 0
        );
    }

    private static int ParseInt(string s) => int.TryParse(s?.Trim().Replace("+","").Replace("-",""), out int v) ? v : 0;
    private static long ParseLong(string s) => long.TryParse(s?.Trim().Replace("+","").Replace("-",""), out long v) ? v : 0;
    private static DateTime ParseTime(string s)
    {
        if (s != null && s.Trim().Length >= 6)
            return DateTime.Today.Add(TimeSpan.ParseExact(s.Trim().Substring(0, 6), "HHmmss", null));
        return DateTime.Now;
    }
}
Copy
2.4 실시간 시세 (SetRealReg)
실시간타입	FID 목록	설명
주식시세	10,11,12,27,28,13,14,15,16,17,18,25,26,29,30,31,32,311	현재가~하한가
주식체결	20,10,11,12,27,28,15,13,14,16,25,26,29,30,31,32	체결시간, 현재가, 등
주식호가잔량	41~50(매도호가), 51~60(매수호가), 61~70(매도잔량), 71~80(매수잔량), 121~128	10차 호가
종목프로그램매매	261,262,263,264,267,268	장중 프로그램 순매수
Copy// === IMMUTABLE: 실시간 등록/해제/수신 ===
public sealed class KiwoomRealtimeReceiver : IMarketDataReceiver
{
    private readonly AxKHOpenAPI _api;
    private int _screenCounter = 5000;

    public event Action<MarketData> OnMarketDataReceived;
    public event Action<TradeResult> OnTradeResultReceived;
    public event Action<OrderInfo> OnOrderUpdateReceived;

    public KiwoomRealtimeReceiver(AxKHOpenAPI api)
    {
        _api = api;
        _api.OnReceiveRealData += OnReceiveRealData;
    }

    public void Subscribe(string code)
    {
        string fids = "10;11;12;13;14;15;16;17;18;20;25;26;27;28;29;30;31;32";
        string screen = (++_screenCounter).ToString();
        _api.SetRealReg(screen, code, fids, "1"); // "1" = 추가등록
    }

    public void Unsubscribe(string code)
    {
        _api.SetRealRemove("ALL", code);
    }

    public void SubscribeAll() { /* 조건검색 결과 종목 일괄 등록 */ }
    public void UnsubscribeAll() { _api.SetRealRemove("ALL", "ALL"); }

    private void OnReceiveRealData(object sender, _DKHOpenAPIEvents_OnReceiveRealDataEvent e)
    {
        string realType = e.sRealType;
        if (realType == "주식체결" || realType == "주식시세")
        {
            var md = new MarketData(
                stockCode:  e.sRealKey,
                price:      Math.Abs(ParseInt(_api.GetCommRealData(e.sRealKey, 10))),
                change:     ParseInt(_api.GetCommRealData(e.sRealKey, 11)),
                changeRate: ParseFloat(_api.GetCommRealData(e.sRealKey, 12)),
                volume:     ParseLong(_api.GetCommRealData(e.sRealKey, 13)),
                high:       Math.Abs(ParseInt(_api.GetCommRealData(e.sRealKey, 17))),
                low:        Math.Abs(ParseInt(_api.GetCommRealData(e.sRealKey, 18))),
                open:       Math.Abs(ParseInt(_api.GetCommRealData(e.sRealKey, 16))),
                tradeTime:  DateTime.Now,
                askPrice:   Math.Abs(ParseInt(_api.GetCommRealData(e.sRealKey, 27))),
                bidPrice:   Math.Abs(ParseInt(_api.GetCommRealData(e.sRealKey, 28)))
            );
            OnMarketDataReceived?.Invoke(md);
        }
    }

    private static int ParseInt(string s) => int.TryParse(s?.Trim().Replace("+","").Replace("-",""), out int v) ? v : 0;
    private static long ParseLong(string s) => long.TryParse(s?.Trim().Replace("+","").Replace("-",""), out long v) ? v : 0;
    private static float ParseFloat(string s) => float.TryParse(s?.Trim(), out float v) ? v : 0f;
}
Copy
2.5 조건검색식
메서드	설명
GetConditionLoad()	조건식 목록 로드 → OnReceiveConditionVer
GetConditionNameList()	"인덱스^이름;..."
SendCondition(screen, name, idx, search)	search=0 일반, 1 실시간
OnReceiveTrCondition	조건 충족 종목 수신
OnReceiveRealCondition	실시간 편입/이탈
Copy// === IMMUTABLE: 조건검색 ===
public sealed class KiwoomConditionSearch
{
    private readonly AxKHOpenAPI _api;
    public event Action<string, List<string>> OnConditionResult;
    public event Action<string, string, string> OnRealCondition; // type, code, condName

    public KiwoomConditionSearch(AxKHOpenAPI api)
    {
        _api = api;
        _api.OnReceiveConditionVer += (s, e) => { /* 조건식 로드 완료 */ };
        _api.OnReceiveTrCondition += OnTrCondition;
        _api.OnReceiveRealCondition += OnRealCond;
    }

    public void LoadConditions() => _api.GetConditionLoad();

    public Dictionary<int, string> GetConditionList()
    {
        var dict = new Dictionary<int, string>();
        string raw = _api.GetConditionNameList();
        foreach (string item in raw.Split(';'))
        {
            if (string.IsNullOrEmpty(item)) continue;
            string[] parts = item.Split('^');
            if (parts.Length == 2 && int.TryParse(parts[0], out int idx))
                dict[idx] = parts[1];
        }
        return dict;
    }

    public void Search(string condName, int condIdx, bool realtime = true)
    {
        _api.SendCondition("6000", condName, condIdx, realtime ? 1 : 0);
    }

    private void OnTrCondition(object sender, _DKHOpenAPIEvents_OnReceiveTrConditionEvent e)
    {
        var codes = e.strCodeList.Split(';').Where(c => !string.IsNullOrEmpty(c)).ToList();
        OnConditionResult?.Invoke(e.strConditionName, codes);
    }

    private void OnRealCond(object sender, _DKHOpenAPIEvents_OnReceiveRealConditionEvent e)
    {
        OnRealCondition?.Invoke(e.strType, e.strCode, e.strConditionName);
    }
}
Copy
2.6 추가 주요 TR 레퍼런스 (Input/Output만)
TR코드	이름	Input	Output 주요필드
opt10002	거래원요청	종목코드	매도거래원1~5, 매수거래원1~5, 매도수량1~5, 매수수량1~5
opt10003	체결정보	종목코드	체결시간, 현재가, 전일대비, 대비율, 체결량, 누적거래량
opt10004	호가요청	종목코드	매도호가1~10, 매수호가1~10, 매도잔량1~10, 매수잔량1~10
opt10014	공매도추이	종목코드, 시작일자, 종료일자	일자, 종가, 전일대비, 공매도량, 매매비중, 거래량
opt10046	체결강도추이(시간별)	종목코드	시간, 체결강도, 매수비율, 매도비율
opt10047	체결강도추이(일별)	종목코드	일자, 체결강도
opt10059	종목별투자자기관별	종목코드, 시작일자, 종료일자	일자, 외국인순매수, 기관순매수, 개인순매수
opt90003	프로그램순매수상위50	시장구분(코스피/코스닥)	종목코드, 종목명, 현재가, 프로그램순매수
opt90004	종목별프로그램매매현황	종목코드	시간, 프로그램매수, 프로그램매도, 차익매수, 비차익매수
opw00001	예수금상세	계좌번호, 비밀번호, 비밀번호입력매체구분, 조회구분	예수금, D+1추정예수금, D+2추정예수금, 주문가능금액
opw00018	계좌평가잔고내역	계좌번호, 비밀번호, 비밀번호입력매체구분, 조회구분	(싱글) 총매입금액, 총평가금액, 총평가손익금액, 총수익률 (멀티) 종목코드, 종목명, 보유수량, 매입가, 현재가, 평가손익, 수익률
opt10075	미체결요청	계좌번호, 전체종목구분, 매매구분, 종목코드	주문번호, 종목코드, 종목명, 주문수량, 주문가격, 미체결수량, 주문구분
opt10076	체결요청	계좌번호, 전체종목구분, 매매구분, 종목코드	주문번호, 체결번호, 종목코드, 체결가, 체결량, 주문구분
3. CybosPlus 불변 API 사전
3.1 접속 및 초기화
항목	값
연결확인 COM	CpUtil.CpCybos → .IsConnect (1=접속)
계좌정보 COM	CpTrade.CpTdUtil → .TradeInit(0)=0 성공
계좌목록	CpTdUtil.AccountNumber (배열)
주의사항	CybosPlus는 관리자 권한으로 실행 필요
Copy// === IMMUTABLE: CybosPlus Connection ===
public sealed class CybosConnector
{
    private readonly dynamic _cpCybos;
    private readonly dynamic _cpTdUtil;

    public CybosConnector()
    {
        _cpCybos = Activator.CreateInstance(Type.GetTypeFromProgID("CpUtil.CpCybos"));
        _cpTdUtil = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTdUtil"));
    }

    public bool IsConnected => (int)_cpCybos.IsConnect == 1;

    public int InitTrade()
    {
        return (int)_cpTdUtil.TradeInit(0); // 0=성공
    }

    public string[] GetAccounts()
    {
        object acc = _cpTdUtil.AccountNumber;
        if (acc is Array arr)
        {
            var list = new List<string>();
            foreach (var item in arr) list.Add(item?.ToString() ?? "");
            return list.ToArray();
        }
        return Array.Empty<string>();
    }

    /// <summary>남은 조회 카운트 (15초에 60회 제한)</summary>
    public int RemainingCount => (int)_cpCybos.GetLimitRemainCount(1); // 1=LT_NONTRADE_REQUEST
}
Copy
VB.NET 접속 코드 (불변)
Copy' === IMMUTABLE: CybosPlus Connection ===
Public NotInheritable Class CybosConnector
    Private ReadOnly _cpCybos As Object
    Private ReadOnly _cpTdUtil As Object

    Public Sub New()
        _cpCybos = CreateObject("CpUtil.CpCybos")
        _cpTdUtil = CreateObject("CpTrade.CpTdUtil")
    End Sub

    Public ReadOnly Property IsConnected As Boolean
        Get
            Return CInt(_cpCybos.IsConnect) = 1
        End Get
    End Property

    Public Function InitTrade() As Integer
        Return CInt(_cpTdUtil.TradeInit(0))
    End Function

    Public ReadOnly Property Accounts As String()
        Get
            Dim acc As Object = _cpTdUtil.AccountNumber
            If TypeOf acc Is Array Then
                Dim arr = DirectCast(acc, Array)
                Dim list As New List(Of String)
                For Each item In arr
                    list.Add(If(item?.ToString(), ""))
                Next
                Return list.ToArray()
            End If
            Return Array.Empty(Of String)()
        End Get
    End Property

    Public ReadOnly Property RemainingCount As Integer
        Get
            Return CInt(_cpCybos.GetLimitRemainCount(1))
        End Get
    End Property
End Class
Copy
3.2 차트 데이터 — CpSysDib.StockChart
구분	항목
COM ProgID	CpSysDib.StockChart
SetInputValue(0)	종목코드 (앞에 "A" 붙임)
SetInputValue(1)	조회구분: 1=기간, 2=개수
SetInputValue(2)	종료일 (YYYYMMDD, 기간조회시)
SetInputValue(3)	시작일 (YYYYMMDD, 기간조회시)
SetInputValue(4)	요청개수 (개수조회시)
SetInputValue(5)	필드배열: 0=날짜, 1=시간, 2=시가, 3=고가, 4=저가, 5=종가, 8=거래량, 9=거래대금, 12=상장주식수, 13=시가총액
SetInputValue(6)	차트구분: 'D'=일, 'W'=주, 'M'=월, 'm'=분, 'T'=틱
SetInputValue(7)	주기 (분봉시 1,3,5,10...)
SetInputValue(9)	수정주가: 1=수정주가
GetHeaderValue(3)	수신 개수
GetDataValue(field, idx)	데이터 접근
제한	1분봉 2년, 5분봉 5년, 틱 20일, 일봉 제한없음
조회제한	15초에 60회
Copy// === IMMUTABLE: CybosPlus StockChart 배치 다운로드 ===
public sealed class CybosStockChart : ICandleProvider
{
    public IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, int count)
    {
        dynamic chart = Activator.CreateInstance(Type.GetTypeFromProgID("CpSysDib.StockChart"));
        chart.SetInputValue(0, "A" + code);                     // 종목코드
        chart.SetInputValue(1, (short)2);                        // 개수 기준
        chart.SetInputValue(4, count);                           // 요청 개수
        chart.SetInputValue(5, new short[] { 0, 1, 2, 3, 4, 5, 8 }); // 날짜,시간,OHLCV
        chart.SetInputValue(6, ToChartGubun(type));              // 차트구분
        if (type == CandleType.Minute)
            chart.SetInputValue(7, interval);                    // 분봉 주기
        chart.SetInputValue(9, (short)1);                        // 수정주가

        chart.BlockRequest();

        int received = (int)chart.GetHeaderValue(3);
        var list = new List<CandleData>(received);
        for (int i = 0; i < received; i++)
        {
            int dateVal = (int)chart.GetDataValue(0, i);         // YYYYMMDD
            int timeVal = (int)chart.GetDataValue(1, i);         // HHMM
            DateTime dt = ParseCybosDateTime(dateVal, timeVal);
            list.Add(new CandleData(
                dateTime: dt,
                open:  (int)chart.GetDataValue(2, i),
                high:  (int)chart.GetDataValue(3, i),
                low:   (int)chart.GetDataValue(4, i),
                close: (int)chart.GetDataValue(5, i),
                volume: (long)chart.GetDataValue(6, i)
            ));
        }
        return list.AsReadOnly();
    }

    public IReadOnlyList<CandleData> GetCandles(string code, CandleType type, int interval, DateTime from, DateTime to)
    {
        dynamic chart = Activator.CreateInstance(Type.GetTypeFromProgID("CpSysDib.StockChart"));
        chart.SetInputValue(0, "A" + code);
        chart.SetInputValue(1, (short)1);                        // 기간 기준
        chart.SetInputValue(2, to.ToString("yyyyMMdd"));
        chart.SetInputValue(3, from.ToString("yyyyMMdd"));
        chart.SetInputValue(5, new short[] { 0, 1, 2, 3, 4, 5, 8 });
        chart.SetInputValue(6, ToChartGubun(type));
        if (type == CandleType.Minute)
            chart.SetInputValue(7, interval);
        chart.SetInputValue(9, (short)1);

        chart.BlockRequest();

        int received = (int)chart.GetHeaderValue(3);
        var list = new List<CandleData>(received);
        for (int i = 0; i < received; i++)
        {
            int dateVal = (int)chart.GetDataValue(0, i);
            int timeVal = (int)chart.GetDataValue(1, i);
            DateTime dt = ParseCybosDateTime(dateVal, timeVal);
            list.Add(new CandleData(dt,
                (int)chart.GetDataValue(2, i), (int)chart.GetDataValue(3, i),
                (int)chart.GetDataValue(4, i), (int)chart.GetDataValue(5, i),
                (long)chart.GetDataValue(6, i)));
        }
        return list.AsReadOnly();
    }

    public void Subscribe(string code, CandleType type, int interval) { /* 실시간은 DsCbo1.StockCur 사용 */ }
    public void Unsubscribe(string code, CandleType type, int interval) { }

    private static char ToChartGubun(CandleType t) => t switch
    {
        CandleType.Day => 'D', CandleType.Week => 'W',
        CandleType.Month => 'M', CandleType.Minute => 'm',
        CandleType.Tick => 'T', _ => 'D'
    };

    private static DateTime ParseCybosDateTime(int date, int time)
    {
        int y = date / 10000, m = (date % 10000) / 100, d = date % 100;
        int h = time / 100, mm = time % 100;
        return new DateTime(y, m, d, h, mm, 0);
    }
}
Copy
3.3 실시간 시세 — DsCbo1.StockCur
구분	항목
COM ProgID	DsCbo1.StockCur
SetInputValue(0)	종목코드 ("A" + code)
Subscribe()	실시간 구독
Unsubscribe()	구독 해제
수신 필드	GetHeaderValue(0)=종목코드, (1)=시간, (13)=현재가, (14)=전일대비, (15)=거래량, (18)=매도호가, (19)=매수호가
이벤트	Received 이벤트
Copy// === IMMUTABLE: CybosPlus 실시간 시세 ===
public sealed class CybosRealtimeReceiver : IMarketDataReceiver
{
    private readonly Dictionary<string, dynamic> _subscribers = new Dictionary<string, dynamic>();

    public event Action<MarketData> OnMarketDataReceived;
    public event Action<TradeResult> OnTradeResultReceived;
    public event Action<OrderInfo> OnOrderUpdateReceived;

    public void Subscribe(string code)
    {
        if (_subscribers.ContainsKey(code)) return;
        dynamic stockCur = Activator.CreateInstance(Type.GetTypeFromProgID("DsCbo1.StockCur"));
        stockCur.SetInputValue(0, "A" + code);
        stockCur.Subscribe();
        // 이벤트 핸들러 등록 (late-binding)
        // stockCur.Received += delegate { OnStockCurReceived(code, stockCur); };
        _subscribers[code] = stockCur;
    }

    public void Unsubscribe(string code)
    {
        if (_subscribers.TryGetValue(code, out dynamic sub))
        {
            sub.Unsubscribe();
            _subscribers.Remove(code);
        }
    }

    public void SubscribeAll() { }
    public void UnsubscribeAll()
    {
        foreach (var kv in _subscribers) kv.Value.Unsubscribe();
        _subscribers.Clear();
    }

    private void OnStockCurReceived(string code, dynamic stockCur)
    {
        var md = new MarketData(
            stockCode:  code,
            price:      (int)stockCur.GetHeaderValue(13),
            change:     (int)stockCur.GetHeaderValue(14),
            changeRate: 0f,
            volume:     (long)stockCur.GetHeaderValue(15),
            high:       0, low: 0, open: 0,
            tradeTime:  DateTime.Now,
            askPrice:   (int)stockCur.GetHeaderValue(18),
            bidPrice:   (int)stockCur.GetHeaderValue(19)
        );
        OnMarketDataReceived?.Invoke(md);
    }
}
Copy
3.4 주문 — CpTrade.CpTd0311
구분	항목
COM ProgID	CpTrade.CpTd0311
SetInputValue(0)	매매구분: 1=매도, 2=매수
SetInputValue(1)	계좌번호
SetInputValue(2)	상품관리구분코드
SetInputValue(3)	종목코드 ("A" + code)
SetInputValue(4)	주문수량
SetInputValue(5)	주문단가 (0=시장가)
SetInputValue(7)	주문조건구분: "0"=기본, "1"=IOC, "2"=FOK
SetInputValue(8)	주문호가구분: "01"=보통, "03"=시장가, "05"=조건부지정가
BlockRequest()	주문 실행
GetHeaderValue(8)	주문번호
Copy// === IMMUTABLE: CybosPlus 주문 ===
public sealed class CybosOrderExecutor : IOrderExecutor
{
    private readonly string _account;
    private readonly string _goodsCode; // 상품관리구분코드

    public CybosOrderExecutor(string account, string goodsCode)
    {
        _account = account;
        _goodsCode = goodsCode;
    }

    public OrderInfo SendOrder(string code, OrderType type, OrderCondition cond, int price, int qty)
    {
        dynamic order = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0311"));
        order.SetInputValue(0, type == OrderType.Sell ? "1" : "2");
        order.SetInputValue(1, _account);
        order.SetInputValue(2, _goodsCode);
        order.SetInputValue(3, "A" + code);
        order.SetInputValue(4, qty);
        order.SetInputValue(5, cond == OrderCondition.Market ? 0 : price);
        order.SetInputValue(7, "0"); // 기본
        order.SetInputValue(8, cond == OrderCondition.Market ? "03" : "01");
        order.BlockRequest();

        string orderNo = order.GetHeaderValue(8)?.ToString() ?? "";
        return new OrderInfo(orderNo, code, type, cond, price, qty, 0,
            string.IsNullOrEmpty(orderNo) ? OrderState.Rejected : OrderState.Submitted,
            DateTime.Now, "OK");
    }

    public OrderInfo ModifyOrder(string orderNo, int newPrice, int newQty)
    {
        // CpTd0313 정정주문 사용
        dynamic modify = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0313"));
        modify.SetInputValue(1, orderNo);
        modify.SetInputValue(2, _account);
        modify.SetInputValue(5, newQty);
        modify.SetInputValue(6, newPrice);
        modify.BlockRequest();
        return new OrderInfo(orderNo, "", OrderType.Buy, OrderCondition.Limit,
            newPrice, newQty, 0, OrderState.Submitted, DateTime.Now, "OK");
    }

    public OrderInfo CancelOrder(string orderNo)
    {
        // CpTd0314 취소주문 사용
        dynamic cancel = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd0314"));
        cancel.SetInputValue(1, orderNo);
        cancel.SetInputValue(2, _account);
        cancel.BlockRequest();
        return new OrderInfo(orderNo, "", OrderType.Buy, OrderCondition.Limit,
            0, 0, 0, OrderState.Cancelled, DateTime.Now, "OK");
    }
}
Copy
3.5 잔고조회 — CpTrade.CpTd6033
구분	항목
COM ProgID	CpTrade.CpTd6033
SetInputValue(0)	계좌번호
SetInputValue(1)	상품관리구분코드
SetInputValue(2)	50 (요청건수)
GetHeaderValue(0)	계좌명
GetHeaderValue(3)	수신개수
GetDataValue(12, i)	종목코드
GetDataValue(0, i)	종목명
GetDataValue(7, i)	체결잔고수량
GetDataValue(17, i)	매입단가
GetDataValue(9, i)	현재가
GetDataValue(11, i)	평가손익
Copy// === IMMUTABLE: CybosPlus 잔고 조회 ===
public sealed class CybosBalanceQuery
{
    private readonly string _account;
    private readonly string _goodsCode;

    public CybosBalanceQuery(string account, string goodsCode)
    {
        _account = account;
        _goodsCode = goodsCode;
    }

    public IReadOnlyList<BalanceInfo> Query()
    {
        dynamic td6033 = Activator.CreateInstance(Type.GetTypeFromProgID("CpTrade.CpTd6033"));
        td6033.SetInputValue(0, _account);
        td6033.SetInputValue(1, _goodsCode);
        td6033.SetInputValue(2, 50);
        td6033.BlockRequest();

        int count = (int)td6033.GetHeaderValue(7);
        var list = new List<BalanceInfo>(count);
        for (int i = 0; i < count; i++)
        {
            string stockCode = td6033.GetDataValue(12, i)?.ToString().Replace("A", "") ?? "";
            list.Add(new BalanceInfo(
                stockCode:    stockCode,
                stockName:    td6033.GetDataValue(0, i)?.ToString() ?? "",
                quantity:     (int)td6033.GetDataValue(7, i),
                avgPrice:     (int)td6033.GetDataValue(17, i),
                currentPrice: (int)td6033.GetDataValue(9, i),
                profitRate:   0,
                profitAmount: (long)td6033.GetDataValue(11, i),
                totalBuyAmount: 0
            ));
        }
        return list.AsReadOnly();
    }
}
Copy
3.6 추가 CybosPlus 주요 COM 레퍼런스
COM ProgID	설명	주요 Input	주요 Output
Dscbo1.StockMst	현재가, 호가	0=종목코드	11=현재가, 12=전일대비, 13=거래량, 16=시가, 17=고가, 18=저가
CpSysDib.MarketEye	복수종목 시세일괄	0=필드배열, 1=종목배열	멀티: 종목코드, 현재가, 거래량, 시가총액 등
CpSysDib.CpSvrNew7221	일자별 기관매매추이	0=종목코드, 1=기관구분, 2=시작일, 3=종료일	일자, 순매수량, 순매수금액
CpSysDib.CpSvrNew7222	시간대별 투자자매매추이	0=종목코드	시간, 개인, 외국인, 기관
Dscbo1.CpSvr8111	프로그램매매 종합	0=시장구분	프로그램매수, 프로그램매도, 차익매수, 비차익매수
Dscbo1.CpSvr8561	테마별 데이터	0=테마코드	종목코드, 종목명, 현재가, 등락률
CpSysDib.CpSvr7254	매매입체분석	0=종목코드, 1=시작일, 2=종료일	일자, 체결강도, 매수비율, 매도비율
DsCbo1.CpConclusion	실시간 체결	Subscribe()	2=주문번호, 3=종목코드, 5=체결가, 6=체결량
Dscbo1.CpSvr8092S	특징주 포착(실시간)	Subscribe()	종목코드, 포착사유, 현재가
4. 통합 서버 — Named Pipe 프로토콜
4.1 메시지 포맷 (Protobuf 대안: 고정헤더 + 바이너리)
┌──────────────┬──────────────┬──────────────┬───────────────────┐
│ MsgType (2B) │ BodyLen (4B) │ SeqNo (4B)   │ Body (N bytes)    │
│ ushort LE    │ uint LE      │ uint LE      │ 가변              │
└──────────────┴──────────────┴──────────────┴───────────────────┘
4.2 MsgType 정의 (불변)
Code	방향	설명
0x0001	App→Server	로그인 요청
0x0002	Server→App	로그인 응답
0x0010	App→Server	TR 조회 요청
0x0011	Server→App	TR 조회 응답
0x0020	App→Server	실시간 구독
0x0021	App→Server	실시간 해제
0x0022	Server→App	실시간 데이터 Push
0x0030	App→Server	주문 요청
0x0031	Server→App	주문 응답
0x0032	Server→App	체결 Push
0x0033	Server→App	잔고 Push
0x0040	App→Server	조건검색 요청
0x0041	Server→App	조건검색 결과
0x0042	Server→App	실시간 조건 편입/이탈
0x0050	App→Server	캔들 배치 요청
0x0051	Server→App	캔들 배치 응답
0x00F0	Both	Heartbeat
0x00FF	Server→App	에러 응답
4.3 Named Pipe 서버 (C# — Server32에서 사용)
Copy// === IMMUTABLE: Named Pipe Server (runs in Server32, x86) ===
public sealed class PipeServer : IDisposable
{
    private const string PIPE_NAME = "TradingBridge";
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private NamedPipeServerStream _pipe;

    public event Action<ushort, uint, byte[]> OnMessageReceived;

    public async Task StartAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            _pipe = new NamedPipeServerStream(PIPE_NAME,
                PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await _pipe.WaitForConnectionAsync(_cts.Token);
            _ = Task.Run(() => ReadLoop(_cts.Token));
        }
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        byte[] header = new byte[10]; // 2 + 4 + 4
        while (!ct.IsCancellationRequested && _pipe.IsConnected)
        {
            int read = await ReadExactAsync(_pipe, header, 10, ct);
            if (read < 10) break;

            ushort msgType = BitConverter.ToUInt16(header, 0);
            uint bodyLen = BitConverter.ToUInt32(header, 2);
            uint seqNo = BitConverter.ToUInt32(header, 6);

            byte[] body = new byte[bodyLen];
            if (bodyLen > 0)
                await ReadExactAsync(_pipe, body, (int)bodyLen, ct);

            OnMessageReceived?.Invoke(msgType, seqNo, body);
        }
    }

    public async Task SendAsync(ushort msgType, uint seqNo, byte[] body)
    {
        byte[] header = new byte[10];
        BitConverter.GetBytes(msgType).CopyTo(header, 0);
        BitConverter.GetBytes((uint)(body?.Length ?? 0)).CopyTo(header, 2);
        BitConverter.GetBytes(seqNo).CopyTo(header, 6);

        await _pipe.WriteAsync(header, 0, 10);
        if (body != null && body.Length > 0)
            await _pipe.WriteAsync(body, 0, body.Length);
        await _pipe.FlushAsync();
    }

    private static async Task<int> ReadExactAsync(Stream s, byte[] buf, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int n = await s.ReadAsync(buf, total, count - total, ct);
            if (n == 0) return total;
            total += n;
        }
        return total;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _pipe?.Dispose();
    }
}
Copy
4.4 Named Pipe 클라이언트 (C# — App64에서 사용)
Copy// === IMMUTABLE: Named Pipe Client (runs in App64, x64) ===
public sealed class PipeClient : IDisposable
{
    private const string PIPE_NAME = "TradingBridge";
    private NamedPipeClientStream _pipe;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private uint _seqNo;

    public event Action<ushort, uint, byte[]> OnMessageReceived;

    public async Task ConnectAsync(string serverName = ".")
    {
        _pipe = new NamedPipeClientStream(serverName, PIPE_NAME,
            PipeDirection.InOut, PipeOptions.Asynchronous);
        await _pipe.ConnectAsync(5000);
        _ = Task.Run(() => ReadLoop(_cts.Token));
    }

    public async Task<uint> SendAsync(ushort msgType, byte[] body)
    {
        uint seq = Interlocked.Increment(ref _seqNo);
        byte[] header = new byte[10];
        BitConverter.GetBytes(msgType).CopyTo(header, 0);
        BitConverter.GetBytes((uint)(body?.Length ?? 0)).CopyTo(header, 2);
        BitConverter.GetBytes(seq).CopyTo(header, 6);

        await _pipe.WriteAsync(header, 0, 10);
        if (body != null && body.Length > 0)
            await _pipe.WriteAsync(body, 0, body.Length);
        await _pipe.FlushAsync();
        return seq;
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        byte[] header = new byte[10];
        while (!ct.IsCancellationRequested && _pipe.IsConnected)
        {
            int read = await ReadExactAsync(_pipe, header, 10, ct);
            if (read < 10) break;
            ushort msgType = BitConverter.ToUInt16(header, 0);
            uint bodyLen = BitConverter.ToUInt32(header, 2);
            uint seqNo = BitConverter.ToUInt32(header, 6);
            byte[] body = new byte[bodyLen];
            if (bodyLen > 0) await ReadExactAsync(_pipe, body, (int)bodyLen, ct);
            OnMessageReceived?.Invoke(msgType, seqNo, body);
        }
    }

    private static async Task<int> ReadExactAsync(Stream s, byte[] buf, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int n = await s.ReadAsync(buf, total, count - total, ct);
            if (n == 0) return total;
            total += n;
        }
        return total;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _pipe?.Dispose();
    }
}
Copy
5. 바이너리 직렬화 (JSON 대체 — 고속)
Copy// === IMMUTABLE: 초고속 바이너리 직렬화 ===
public static class BinarySerializer
{
    // MarketData → byte[]
    public static byte[] SerializeMarketData(MarketData md)
    {
        using (var ms = new MemoryStream(64))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(md.StockCode ?? "");
            bw.Write(md.Price);
            bw.Write(md.Change);
            bw.Write(md.ChangeRate);
            bw.Write(md.Volume);
            bw.Write(md.High);
            bw.Write(md.Low);
            bw.Write(md.Open);
            bw.Write(md.TradeTime.ToBinary());
            bw.Write(md.AskPrice);
            bw.Write(md.BidPrice);
            return ms.ToArray();
        }
    }

    public static MarketData DeserializeMarketData(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var br = new BinaryReader(ms))
        {
            return new MarketData(
                stockCode:  br.ReadString(),
                price:      br.ReadInt32(),
                change:     br.ReadInt32(),
                changeRate: br.ReadSingle(),
                volume:     br.ReadInt64(),
                high:       br.ReadInt32(),
                low:        br.ReadInt32(),
                open:       br.ReadInt32(),
                tradeTime:  DateTime.FromBinary(br.ReadInt64()),
                askPrice:   br.ReadInt32(),
                bidPrice:   br.ReadInt32()
            );
        }
    }

    // CandleData → byte[] (배치용)
    public static byte[] SerializeCandleBatch(IReadOnlyList<CandleData> candles)
    {
        using (var ms = new MemoryStream(candles.Count * 40))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(candles.Count);
            foreach (var c in candles)
            {
                bw.Write(c.DateTime.ToBinary());
                bw.Write(c.Open);
                bw.Write(c.High);
                bw.Write(c.Low);
                bw.Write(c.Close);
                bw.Write(c.Volume);
            }
            return ms.ToArray();
        }
    }

    public static IReadOnlyList<CandleData> DeserializeCandleBatch(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var br = new BinaryReader(ms))
        {
            int count = br.ReadInt32();
            var list = new List<CandleData>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new CandleData(
                    DateTime.FromBinary(br.ReadInt64()),
                    br.ReadInt32(), br.ReadInt32(),
                    br.ReadInt32(), br.ReadInt32(),
                    br.ReadInt64()));
            }
            return list.AsReadOnly();
        }
    }

    // OrderInfo → byte[]
    public static byte[] SerializeOrder(OrderInfo o)
    {
        using (var ms = new MemoryStream(128))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(o.OrderNo ?? "");
            bw.Write(o.StockCode ?? "");
            bw.Write((int)o.OrderType);
            bw.Write((int)o.Condition);
            bw.Write(o.Price);
            bw.Write(o.Quantity);
            bw.Write(o.FilledQuantity);
            bw.Write((int)o.State);
            bw.Write(o.OrderTime.ToBinary());
            bw.Write(o.Message ?? "");
            return ms.ToArray();
        }
    }

    public static OrderInfo DeserializeOrder(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var br = new BinaryReader(ms))
        {
            return new OrderInfo(
                br.ReadString(), br.ReadString(),
                (OrderType)br.ReadInt32(), (OrderCondition)br.ReadInt32(),
                br.ReadInt32(), br.ReadInt32(), br.ReadInt32(),
                (OrderState)br.ReadInt32(),
                DateTime.FromBinary(br.ReadInt64()),
                br.ReadString());
        }
    }
}
Copy
6. Python 소켓 클라이언트 (외부 연동용)
Copy# === IMMUTABLE: Python Named Pipe Client (Windows) ===
import struct
import win32pipe
import win32file

PIPE_NAME = r'\\.\pipe\TradingBridge'

class TradingPipeClient:
    """Python에서 32비트 서버에 접속하는 Named Pipe 클라이언트"""

    MSG_LOGIN_REQ = 0x0001
    MSG_TR_REQ    = 0x0010
    MSG_REALTIME_SUB = 0x0020
    MSG_ORDER_REQ = 0x0030
    MSG_CANDLE_REQ = 0x0050

    def __init__(self):
        self._handle = None
        self._seq = 0

    def connect(self):
        self._handle = win32file.CreateFile(
            PIPE_NAME,
            win32file.GENERIC_READ | win32file.GENERIC_WRITE,
            0, None,
            win32file.OPEN_EXISTING,
            0, None
        )

    def send(self, msg_type: int, body: bytes = b'') -> int:
        self._seq += 1
        header = struct.pack('<HII', msg_type, len(body), self._seq)
        win32file.WriteFile(self._handle, header + body)
        return self._seq

    def recv(self):
        _, header = win32file.ReadFile(self._handle, 10)
        msg_type, body_len, seq_no = struct.unpack('<HII', header)
        body = b''
        if body_len > 0:
            _, body = win32file.ReadFile(self._handle, body_len)
        return msg_type, seq_no, body

    def close(self):
        if self._handle:
            win32file.CloseHandle(self._handle)

    # 편의 메서드
    def request_candles(self, code: str, chart_type: str = 'D', count: int = 100):
        body = f'{code}|{chart_type}|{count}'.encode('utf-8')
        seq = self.send(self.MSG_CANDLE_REQ, body)
        msg_type, _, resp = self.recv()
        return resp  # 바이너리 CandleData 배열

    def subscribe_realtime(self, code: str):
        body = code.encode('utf-8')
        self.send(self.MSG_REALTIME_SUB, body)

    def send_order(self, code: str, buy: bool, price: int, qty: int):
        body = f'{code}|{"B" if buy else "S"}|{price}|{qty}'.encode('utf-8')
        seq = self.send(self.MSG_ORDER_REQ, body)
        msg_type, _, resp = self.recv()
        return resp
Copy
7. 통합 서버 디스패처 (Server32 메인 로직)
Copy// === IMMUTABLE: 통합 메시지 디스패처 ===
public sealed class ServerDispatcher
{
    private readonly PipeServer _pipe;
    private readonly KiwoomConnector _kiwoom;
    private readonly CybosConnector _cybos;
    private readonly KiwoomOrderExecutor _kiwoomOrder;
    private readonly CybosOrderExecutor _cybosOrder;
    private readonly CybosStockChart _cybosChart;

    public ServerDispatcher(PipeServer pipe,
        KiwoomConnector kiwoom, CybosConnector cybos,
        KiwoomOrderExecutor kiwoomOrder, CybosOrderExecutor cybosOrder,
        CybosStockChart cybosChart)
    {
        _pipe = pipe;
        _kiwoom = kiwoom;
        _cybos = cybos;
        _kiwoomOrder = kiwoomOrder;
        _cybosOrder = cybosOrder;
        _cybosChart = cybosChart;

        _pipe.OnMessageReceived += OnMessage;
    }

    private async void OnMessage(ushort msgType, uint seqNo, byte[] body)
    {
        switch (msgType)
        {
            case 0x0001: // 로그인
                await HandleLogin(seqNo);
                break;
            case 0x0050: // 캔들 배치
                await HandleCandleBatch(seqNo, body);
                break;
            case 0x0030: // 주문
                await HandleOrder(seqNo, body);
                break;
            case 0x0020: // 실시간 구독
                HandleRealtimeSubscribe(body);
                break;
            case 0x0021: // 실시간 해제
                HandleRealtimeUnsubscribe(body);
                break;
            case 0x00F0: // Heartbeat
                await _pipe.SendAsync(0x00F0, seqNo, Array.Empty<byte>());
                break;
        }
    }

    private async Task HandleLogin(uint seqNo)
    {
        bool kOk = _kiwoom.IsConnected;
        bool cOk = _cybos.IsConnected;
        byte[] resp = new byte[] { (byte)(kOk ? 1 : 0), (byte)(cOk ? 1 : 0) };
        await _pipe.SendAsync(0x0002, seqNo, resp);
    }

    private async Task HandleCandleBatch(uint seqNo, byte[] body)
    {
        // body: "종목코드|차트구분|개수" UTF8
        string[] parts = System.Text.Encoding.UTF8.GetString(body).Split('|');
        string code = parts[0];
        CandleType ct = parts[1] switch
        {
            "D" => CandleType.Day, "W" => CandleType.Week,
            "M" => CandleType.Month, "m" => CandleType.Minute,
            "T" => CandleType.Tick, _ => CandleType.Day
        };
        int count = int.Parse(parts[2]);
        // Cybos는 배치 다운로드에 최적 (조회제한 15초 60회)
        var candles = _cybosChart.GetCandles(code, ct, 1, count);
        byte[] resp = BinarySerializer.SerializeCandleBatch(candles);
        await _pipe.SendAsync(0x0051, seqNo, resp);
    }

    private async Task HandleOrder(uint seqNo, byte[] body)
    {
        // body: "종목코드|B/S|가격|수량|소스" UTF8
        string[] parts = System.Text.Encoding.UTF8.GetString(body).Split('|');
        string code = parts[0];
        OrderType ot = parts[1] == "B" ? OrderType.Buy : OrderType.Sell;
        int price = int.Parse(parts[2]);
        int qty = int.Parse(parts[3]);
        string source = parts.Length > 4 ? parts[4] : "kiwoom";
        OrderCondition cond = price == 0 ? OrderCondition.Market : OrderCondition.Limit;

        OrderInfo result;
        if (source == "cybos")
            result = _cybosOrder.SendOrder(code, ot, cond, price, qty);
        else
            result = _kiwoomOrder.SendOrder(code, ot, cond, price, qty);

        byte[] resp = BinarySerializer.SerializeOrder(result);
        await _pipe.SendAsync(0x0031, seqNo, resp);
    }

    private void HandleRealtimeSubscribe(byte[] body)
    {
        string code = System.Text.Encoding.UTF8.GetString(body);
        // 키움 실시간: 조건검색 + 체결
        // Cybos 실시간: StockCur + CpConclusion
        // 양쪽 모두 구독하면 데이터 이중 수신 → 키움 우선
    }

    private void HandleRealtimeUnsubscribe(byte[] body)
    {
        string code = System.Text.Encoding.UTF8.GetString(body);
    }
}
Copy
8. 데이터소스 선택 전략 (불변 규칙)
기능	주 데이터소스	이유
일/주/월봉 배치 다운로드	Cybos (StockChart)	제한 없음, 연속조회 편리
분봉 배치 다운로드	Cybos (StockChart)	2년 데이터
틱봉 배치 다운로드	Cybos (StockChart)	20일 데이터
실시간 체결	키움 (SetRealReg)	FID 풍부, 이벤트 기반
실시간 호가	키움 (주식호가잔량)	10차 호가
조건검색	키움 (SendCondition)	실시간 편입/이탈
주문/체결	키움 (SendOrder)	안정적, 모의투자 지원
프로그램 순매수	Cybos (CpSvr8111)	실시간 지원
투자자별 매매	Cybos (CpSvrNew7221)	일자별/시간대별
체결강도	키움 (opt10046) 또는 Cybos (CpSvr7254)	둘 다 가능
섹터/테마	Cybos (CpSvr8561/8563)	전용 API
뉴스	Cybos (CpSvr8092S 특징주)	실시간
예수금/잔고	키움 (opw00001/opw00018)	주 계좌
9. 사용 가이드 (AI 코드 생성시)
이 Skills 파일을 참조할 때 AI는 다음 규칙을 준수합니다:

모든 모델은 불변 (Immutable): Common.Models 네임스페이스의 클래스만 사용. 새 모델이 필요하면 같은 불변 패턴으로 생성.
인터페이스 기반: IStockProvider, ICandleProvider, IOrderExecutor, IMarketDataReceiver 구현으로 소스 교체 가능.
데이터소스 선택: 위 §8 표에 따라 최적 소스 자동 선택.
직렬화: JSON 사용 금지. BinarySerializer 또는 Protobuf 사용.
통신: Named Pipe + §4.2 MsgType 프로토콜 준수.
TR 요청: 키움 TR은 반드시 SetInputValue → CommRqData → OnReceiveTrData 패턴.
COM 요청: Cybos COM은 반드시 SetInputValue → BlockRequest → GetHeaderValue/GetDataValue 패턴.
조회 제한: 키움 초당 5회 / Cybos 15초 60회 준수. 초과 시 자동 대기.
에러 처리: COM 호출은 반드시 try-catch로 래핑. 연결 해제시 재접속 로직 포함.
코드 언어: 요청에 따라 C#, VB.NET, Python 코드를 이 파일의 패턴대로 생성.
10. 파일 맵 (프로젝트 구조)
TradingSystem.sln
├── Common/                          ← 불변 모델 (AnyCPU)
│   ├── Enums/
│   ├── Models/
│   ├── Interfaces/
│   └── Modules/
├── Bridge/                          ← 통신 라이브러리 (AnyCPU)
│   ├── PipeServer.cs
│   ├── PipeClient.cs
│   ├── BinarySerializer.cs
│   └── MessageTypes.cs
├── Server32/                        ← 32비트 서버 (x86)
│   ├── Kiwoom/
│   │   ├── KiwoomConnector.cs
│   │   ├── KiwoomTR_opt10001.cs
│   │   ├── KiwoomTR_opt10080.cs
│   │   ├── KiwoomTR_opt10081.cs
│   │   ├── KiwoomOrderExecutor.cs
│   │   ├── KiwoomRealtimeReceiver.cs
│   │   ├── KiwoomConditionSearch.cs
│   │   └── ChejanParser.cs
│   ├── Cybos/
│   │   ├── CybosConnector.cs
│   │   ├── CybosStockChart.cs
│   │   ├── CybosRealtimeReceiver.cs
│   │   ├── CybosOrderExecutor.cs
│   │   └── CybosBalanceQuery.cs
│   ├── ServerDispatcher.cs
│   └── MainForm.cs
├── App64/                           ← 64비트 앱 (x64)
│   ├── MainForm.cs
│   └── ...
└── NativeCalc/                      ← C++ 지표 엔진 (x64)
    └── ...