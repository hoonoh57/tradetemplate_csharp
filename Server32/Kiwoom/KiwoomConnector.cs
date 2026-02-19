using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server32.Kiwoom
{
    // ══════════════════════════════════════════════
    //  이벤트 인자 클래스
    // ══════════════════════════════════════════════

    public class _DKHOpenAPIEvents_OnEventConnectEvent : EventArgs
    {
        public int nErrCode { get; }
        public _DKHOpenAPIEvents_OnEventConnectEvent(int errCode) { nErrCode = errCode; }
    }

    public class _DKHOpenAPIEvents_OnReceiveTrDataEvent : EventArgs
    {
        public string sScrNo { get; }
        public string sRQName { get; }
        public string sTrCode { get; }
        public string sRecordName { get; }
        public string sPrevNext { get; }
        public int nDataLength { get; }
        public string sErrorCode { get; }
        public string sMessage { get; }
        public string sSplmMsg { get; }

        public _DKHOpenAPIEvents_OnReceiveTrDataEvent(string scrNo, string rqName,
            string trCode, string recordName, string prevNext, int dataLen,
            string errCode, string msg, string splmMsg)
        {
            sScrNo = scrNo; sRQName = rqName; sTrCode = trCode;
            sRecordName = recordName; sPrevNext = prevNext; nDataLength = dataLen;
            sErrorCode = errCode; sMessage = msg; sSplmMsg = splmMsg;
        }
    }

    public class _DKHOpenAPIEvents_OnReceiveRealDataEvent : EventArgs
    {
        public string sRealKey { get; }
        public string sRealType { get; }
        public string sRealData { get; }

        public _DKHOpenAPIEvents_OnReceiveRealDataEvent(string realKey, string realType, string realData)
        {
            sRealKey = realKey; sRealType = realType; sRealData = realData;
        }
    }

    public class _DKHOpenAPIEvents_OnReceiveChejanDataEvent : EventArgs
    {
        public string sGubun { get; }
        public int nItemCnt { get; }
        public string sFIdList { get; }

        public _DKHOpenAPIEvents_OnReceiveChejanDataEvent(string gubun, int itemCnt, string fidList)
        {
            sGubun = gubun; nItemCnt = itemCnt; sFIdList = fidList;
        }
    }

    public class _DKHOpenAPIEvents_OnReceiveMsgEvent : EventArgs
    {
        public string sScrNo { get; }
        public string sRQName { get; }
        public string sTrCode { get; }
        public string sMsg { get; }

        public _DKHOpenAPIEvents_OnReceiveMsgEvent(string scrNo, string rqName, string trCode, string msg)
        {
            sScrNo = scrNo; sRQName = rqName; sTrCode = trCode; sMsg = msg;
        }
    }

    // 조건검색 이벤트 인자
    public class _DKHOpenAPIEvents_OnReceiveConditionVerEvent : EventArgs
    {
        public int lRet { get; }
        public string sMsg { get; }
        public _DKHOpenAPIEvents_OnReceiveConditionVerEvent(int ret, string msg)
        { lRet = ret; sMsg = msg; }
    }

    public class _DKHOpenAPIEvents_OnReceiveTrConditionEvent : EventArgs
    {
        public string sScrNo { get; }
        public string strCodeList { get; }
        public string strConditionName { get; }
        public int nIndex { get; }
        public int nNext { get; }
        public _DKHOpenAPIEvents_OnReceiveTrConditionEvent(
            string scrNo, string codeList, string condName, int index, int next)
        {
            sScrNo = scrNo; strCodeList = codeList; strConditionName = condName;
            nIndex = index; nNext = next;
        }
    }

    public class _DKHOpenAPIEvents_OnReceiveRealConditionEvent : EventArgs
    {
        public string strCode { get; }
        public string strType { get; }
        public string strConditionName { get; }
        public string strConditionIndex { get; }
        public _DKHOpenAPIEvents_OnReceiveRealConditionEvent(
            string code, string type, string condName, string condIndex)
        {
            strCode = code; strType = type; strConditionName = condName;
            strConditionIndex = condIndex;
        }
    }

    // ══════════════════════════════════════════════
    //  AxKHOpenAPI — AxHost 파생 ActiveX 래퍼
    // ══════════════════════════════════════════════

    [ComVisible(true)]
    public class AxKHOpenAPI : AxHost
    {
        private dynamic _ocx;
        private bool _sinkConnected;

        public static event Action<string> DiagLog;

        public AxKHOpenAPI() : base("A1574A0D-6BFA-4BD7-9020-DED88711818D") { }

        protected override void AttachInterfaces()
        {
            _ocx = this.GetOcx();
            DiagLog?.Invoke("[AxKH] AttachInterfaces 완료, _ocx=" + (_ocx != null));
        }

        protected override void CreateSink()
        {
            try
            {
                _sink = new KHOpenAPIEventSink(this);
                _cookie = new AxHost.ConnectionPointCookie(
                    _ocx, _sink, typeof(_DKHOpenAPIEvents));
                _sinkConnected = true;
                DiagLog?.Invoke("[AxKH] CreateSink 성공 (ConnectionPoint)");
            }
            catch (Exception ex)
            {
                _sinkConnected = false;
                DiagLog?.Invoke("[AxKH] CreateSink 실패: " + ex.GetType().Name + " — " + ex.Message);

                try
                {
                    _ocx.OnEventConnect += new Action<int>(Dynamic_OnEventConnect);
                    _ocx.OnReceiveTrData += new Action<string, string, string, string, string, int, string, string, string>(Dynamic_OnReceiveTrData);
                    _ocx.OnReceiveRealData += new Action<string, string, string>(Dynamic_OnReceiveRealData);
                    _ocx.OnReceiveMsg += new Action<string, string, string, string>(Dynamic_OnReceiveMsg);
                    _ocx.OnReceiveChejanData += new Action<string, int, string>(Dynamic_OnReceiveChejanData);
                    _ocx.OnReceiveConditionVer += new Action<int, string>(Dynamic_OnReceiveConditionVer);
                    _ocx.OnReceiveTrCondition += new Action<string, string, string, int, int>(Dynamic_OnReceiveTrCondition);
                    _ocx.OnReceiveRealCondition += new Action<string, string, string, string>(Dynamic_OnReceiveRealCondition);
                    _sinkConnected = true;
                    DiagLog?.Invoke("[AxKH] Dynamic 이벤트 바인딩 성공 (조건검색 포함)");
                }
                catch (Exception ex2)
                {
                    DiagLog?.Invoke("[AxKH] Dynamic 바인딩도 실패: " + ex2.Message);
                }
            }
        }

        // dynamic 이벤트 핸들러
        private void Dynamic_OnEventConnect(int nErrCode)
            => RaiseOnEventConnect(nErrCode);

        private void Dynamic_OnReceiveTrData(string scrNo, string rqName, string trCode,
            string recordName, string prevNext, int dataLen, string errCode, string msg, string splmMsg)
            => RaiseOnReceiveTrData(scrNo, rqName, trCode, recordName, prevNext, dataLen, errCode, msg, splmMsg);

        private void Dynamic_OnReceiveRealData(string realKey, string realType, string realData)
            => RaiseOnReceiveRealData(realKey, realType, realData);

        private void Dynamic_OnReceiveMsg(string scrNo, string rqName, string trCode, string msg)
            => RaiseOnReceiveMsg(scrNo, rqName, trCode, msg);

        private void Dynamic_OnReceiveChejanData(string gubun, int itemCnt, string fidList)
            => RaiseOnReceiveChejanData(gubun, itemCnt, fidList);

        private void Dynamic_OnReceiveConditionVer(int lRet, string sMsg)
            => RaiseOnReceiveConditionVer(lRet, sMsg);

        private void Dynamic_OnReceiveTrCondition(string scrNo, string codeList, string condName, int index, int next)
            => RaiseOnReceiveTrCondition(scrNo, codeList, condName, index, next);

        private void Dynamic_OnReceiveRealCondition(string code, string type, string condName, string condIndex)
            => RaiseOnReceiveRealCondition(code, type, condName, condIndex);

        protected override void DetachSink()
        {
            if (_cookie != null)
            {
                _cookie.Disconnect();
                _cookie = null;
            }
        }

        private KHOpenAPIEventSink _sink;
        private AxHost.ConnectionPointCookie _cookie;

        public bool IsSinkConnected => _sinkConnected;

        // ── 이벤트 ──
        public event EventHandler<_DKHOpenAPIEvents_OnEventConnectEvent> OnEventConnect;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveTrDataEvent> OnReceiveTrData;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveRealDataEvent> OnReceiveRealData;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveChejanDataEvent> OnReceiveChejanData;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveMsgEvent> OnReceiveMsg;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveConditionVerEvent> OnReceiveConditionVer;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveTrConditionEvent> OnReceiveTrCondition;
        public event EventHandler<_DKHOpenAPIEvents_OnReceiveRealConditionEvent> OnReceiveRealCondition;

        internal void RaiseOnEventConnect(int errCode)
        {
            DiagLog?.Invoke($"[AxKH] ★ RaiseOnEventConnect({errCode})");
            OnEventConnect?.Invoke(this, new _DKHOpenAPIEvents_OnEventConnectEvent(errCode));
        }

        internal void RaiseOnReceiveTrData(string scrNo, string rqName, string trCode,
            string recordName, string prevNext, int dataLen, string errCode,
            string msg, string splmMsg)
        {
            DiagLog?.Invoke($"[AxKH] ★ RaiseOnReceiveTrData rqName=\"{rqName}\" trCode=\"{trCode}\"");
            OnReceiveTrData?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveTrDataEvent(
                scrNo, rqName, trCode, recordName, prevNext, dataLen, errCode, msg, splmMsg));
        }

        internal void RaiseOnReceiveRealData(string realKey, string realType, string realData)
        {
            OnReceiveRealData?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveRealDataEvent(
                realKey, realType, realData));
        }

        internal void RaiseOnReceiveChejanData(string gubun, int itemCnt, string fidList)
        {
            OnReceiveChejanData?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveChejanDataEvent(
                gubun, itemCnt, fidList));
        }

        internal void RaiseOnReceiveMsg(string scrNo, string rqName, string trCode, string msg)
        {
            OnReceiveMsg?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveMsgEvent(
                scrNo, rqName, trCode, msg));
        }

        internal void RaiseOnReceiveConditionVer(int lRet, string sMsg)
        {
            DiagLog?.Invoke($"[AxKH] ★ RaiseOnReceiveConditionVer ret={lRet} msg=\"{sMsg}\"");
            OnReceiveConditionVer?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveConditionVerEvent(lRet, sMsg));
        }

        internal void RaiseOnReceiveTrCondition(string scrNo, string codeList, string condName, int index, int next)
        {
            DiagLog?.Invoke($"[AxKH] ★ RaiseOnReceiveTrCondition cond=\"{condName}\" codes={codeList?.Length ?? 0}자");
            OnReceiveTrCondition?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveTrConditionEvent(
                scrNo, codeList, condName, index, next));
        }

        internal void RaiseOnReceiveRealCondition(string code, string type, string condName, string condIndex)
        {
            //DiagLog?.Invoke($"[AxKH] ★ RaiseOnReceiveRealCondition code={code} type={type} cond=\"{condName}\"");
            OnReceiveRealCondition?.Invoke(this, new _DKHOpenAPIEvents_OnReceiveRealConditionEvent(
                code, type, condName, condIndex));
        }

        // ── OCX 메서드 래퍼 ──

        public int CommConnect() => (int)_ocx.CommConnect();
        public int GetConnectState() => (int)_ocx.GetConnectState();
        public string GetLoginInfo(string tag) => _ocx.GetLoginInfo(tag)?.ToString() ?? "";
        public void SetInputValue(string id, string value) => _ocx.SetInputValue(id, value);

        public int CommRqData(string rqName, string trCode, int prevNext, string screenNo)
            => (int)_ocx.CommRqData(rqName, trCode, prevNext, screenNo);

        public string GetCommData(string trCode, string rqName, int index, string itemName)
            => _ocx.GetCommData(trCode, rqName, index, itemName)?.ToString() ?? "";

        public int GetRepeatCnt(string trCode, string rqName)
            => (int)_ocx.GetRepeatCnt(trCode, rqName);

        public object GetCommDataEx(string trCode, string recordName)
            => _ocx.GetCommDataEx(trCode, recordName);

        public string GetCommRealData(string code, int fid)
            => _ocx.GetCommRealData(code, fid)?.ToString() ?? "";

        public int SetRealReg(string screenNo, string codeList, string fidList, string optType)
            => (int)_ocx.SetRealReg(screenNo, codeList, fidList, optType);

        public void SetRealRemove(string screenNo, string code)
            => _ocx.SetRealRemove(screenNo, code);

        public int SendOrder(string rqName, string screenNo, string accNo,
            int orderType, string code, int qty, int price, string hogaGb, string orgOrderNo)
            => (int)_ocx.SendOrder(rqName, screenNo, accNo, orderType, code, qty, price, hogaGb, orgOrderNo);

        public string GetChejanData(int fid) => _ocx.GetChejanData(fid)?.ToString() ?? "";

        public string GetCodeListByMarket(string marketCode)
            => _ocx.GetCodeListByMarket(marketCode)?.ToString() ?? "";

        public string GetMasterCodeName(string code)
            => _ocx.GetMasterCodeName(code)?.ToString() ?? "";

        // ── 조건검색 OCX 메서드 ──

        public int GetConditionLoad()
            => (int)_ocx.GetConditionLoad();


        public string GetConditionNameList()
            => _ocx.GetConditionNameList()?.ToString() ?? "";

        public int SendCondition(string screenNo, string conditionName, int conditionIndex, int nSearch)
            => (int)_ocx.SendCondition(screenNo, conditionName, conditionIndex, nSearch);

        public void SendConditionStop(string screenNo, string conditionName, int conditionIndex)
            => _ocx.SendConditionStop(screenNo, conditionName, conditionIndex);

        public int CommKwRqData(string arrCode, bool next, int codeCount, int typeFlag, string rqName, string screenNo)
        => (int)_ocx.CommKwRqData(arrCode, next ? 1 : 0, codeCount, typeFlag, rqName, screenNo);

    }  //<============================




    // ══════════════════════════════════════════════
    //  COM 이벤트 싱크 (ConnectionPoint용 — 현재 실패하지만 유지)
    // ══════════════════════════════════════════════

    [ComImport]
    [Guid("6D9B7B37-B34C-4479-B87E-3C29C0EA5A8D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface _DKHOpenAPIEvents
    {
        [DispId(1)] void OnEventConnect(int nErrCode);
        [DispId(2)]
        void OnReceiveTrData(string sScrNo, string sRQName, string sTrCode,
            string sRecordName, string sPrevNext, int nDataLength,
            string sErrorCode, string sMessage, string sSplmMsg);
        [DispId(3)] void OnReceiveRealData(string sRealKey, string sRealType, string sRealData);
        [DispId(4)] void OnReceiveMsg(string sScrNo, string sRQName, string sTrCode, string sMsg);
        [DispId(5)] void OnReceiveChejanData(string sGubun, int nItemCnt, string sFIdList);
    }

    [ClassInterface(ClassInterfaceType.None)]
    public class KHOpenAPIEventSink : _DKHOpenAPIEvents
    {
        private readonly AxKHOpenAPI _host;
        public KHOpenAPIEventSink(AxKHOpenAPI host) { _host = host; }

        public void OnEventConnect(int nErrCode) => _host.RaiseOnEventConnect(nErrCode);
        public void OnReceiveTrData(string sScrNo, string sRQName, string sTrCode,
            string sRecordName, string sPrevNext, int nDataLength,
            string sErrorCode, string sMessage, string sSplmMsg)
            => _host.RaiseOnReceiveTrData(sScrNo, sRQName, sTrCode, sRecordName, sPrevNext, nDataLength, sErrorCode, sMessage, sSplmMsg);
        public void OnReceiveRealData(string sRealKey, string sRealType, string sRealData)
            => _host.RaiseOnReceiveRealData(sRealKey, sRealType, sRealData);
        public void OnReceiveMsg(string sScrNo, string sRQName, string sTrCode, string sMsg)
            => _host.RaiseOnReceiveMsg(sScrNo, sRQName, sTrCode, sMsg);
        public void OnReceiveChejanData(string sGubun, int nItemCnt, string sFIdList)
            => _host.RaiseOnReceiveChejanData(sGubun, nItemCnt, sFIdList);
    }

    // ══════════════════════════════════════════════
    //  KiwoomConnector — 상위 비즈니스 로직
    // ══════════════════════════════════════════════

    public sealed class KiwoomConnector
    {
        private AxKHOpenAPI _api;
        private bool _connected;
        private TaskCompletionSource<int> _loginTcs;

        public AxKHOpenAPI Api => _api;
        public bool IsConnected => _connected;
        public bool IsSimulation { get; private set; }
        public string[] Accounts { get; private set; } = Array.Empty<string>();

        // ── 이벤트 (외부 소비자) ──
        public event Action<bool> OnLoginCompleted;
        public event Action<string, string, string> OnReceiveRealData;
        public event Action<_DKHOpenAPIEvents_OnReceiveTrDataEvent> OnReceiveTrData;
        public event Action<_DKHOpenAPIEvents_OnReceiveChejanDataEvent> OnReceiveChejanData;
        public event Action<string, string, string, string> OnReceiveMsg;
        public event Action<string> OnLog;

        // 조건검색 이벤트
        public event Action<int, string> OnReceiveConditionVer;
        public event Action<string, string, string, int, int> OnReceiveTrCondition;
        public event Action<string, string, string, string> OnReceiveRealCondition;

        public bool Initialize(Form hostForm)
        {
            try
            {
                AxKHOpenAPI.DiagLog += msg => OnLog?.Invoke(msg);

                _api = new AxKHOpenAPI();
                ((System.ComponentModel.ISupportInitialize)_api).BeginInit();
                _api.Visible = false;
                _api.Dock = DockStyle.None;
                _api.Size = new System.Drawing.Size(0, 0);
                _api.Location = new System.Drawing.Point(0, 0);
                hostForm.Controls.Add(_api);
                ((System.ComponentModel.ISupportInitialize)_api).EndInit();

                OnLog?.Invoke($"[키움] AxKHOpenAPI 초기화 완료, SinkConnected={_api.IsSinkConnected}");

                // 이벤트 연결
                _api.OnEventConnect += Api_OnEventConnect;
                _api.OnReceiveTrData += Api_OnReceiveTrData;
                _api.OnReceiveRealData += Api_OnReceiveRealData;
                _api.OnReceiveChejanData += Api_OnReceiveChejanData;
                _api.OnReceiveMsg += Api_OnReceiveMsg;
                _api.OnReceiveConditionVer += Api_OnReceiveConditionVer;
                _api.OnReceiveTrCondition += Api_OnReceiveTrCondition;
                _api.OnReceiveRealCondition += Api_OnReceiveRealCondition;

                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[키움] Initialize 실패: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginAsync(int timeoutMs = 60000)
        {
            if (_api == null) return false;

            _loginTcs = new TaskCompletionSource<int>();
            int ret = _api.CommConnect();
            OnLog?.Invoke($"[키움] CommConnect() 반환값: {ret}");
            if (ret != 0) { _connected = false; return false; }

            var evtCompleted = await Task.WhenAny(_loginTcs.Task, Task.Delay(3000));
            if (evtCompleted == _loginTcs.Task)
            {
                int errCode = _loginTcs.Task.Result;
                _connected = (errCode == 0);
                OnLog?.Invoke($"[키움] 이벤트 수신: errCode={errCode}, connected={_connected}");
            }
            else
            {
                OnLog?.Invoke("[키움] 이벤트 미수신, 폴링 전환...");
                int elapsed = 3000;
                while (elapsed < timeoutMs)
                {
                    await Task.Delay(500);
                    elapsed += 500;
                    try
                    {
                        int state = _api.GetConnectState();
                        if (state == 1)
                        {
                            _connected = true;
                            OnLog?.Invoke($"[키움] 폴링 성공 ({elapsed}ms)");
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (_connected)
            {
                await Task.Delay(1000);
                string accList = _api.GetLoginInfo("ACCNO") ?? "";
                OnLog?.Invoke($"[키움] GetLoginInfo(\"ACCNO\") = \"{accList}\"");

                if (string.IsNullOrWhiteSpace(accList))
                {
                    accList = _api.GetLoginInfo("ACCLIST") ?? "";
                    OnLog?.Invoke($"[키움] GetLoginInfo(\"ACCLIST\") fallback = \"{accList}\"");
                }

                Accounts = accList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                IsSimulation = _api.GetLoginInfo("GetServerGubun") == "1";
                OnLog?.Invoke($"[키움] 계좌 {Accounts.Length}개: {string.Join(", ", Accounts)} (모의={IsSimulation})");
            }

            OnLoginCompleted?.Invoke(_connected);
            return _connected;
        }


        public int CommKwRqData(string arrCode, bool next, int codeCount, int typeFlag, string rqName, string screenNo)
    => _api?.CommKwRqData(arrCode, next, codeCount, typeFlag, rqName, screenNo) ?? -1;





        // ── COM 이벤트 핸들러 ──

        private void Api_OnEventConnect(object sender, _DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            OnLog?.Invoke($"[키움] OnEventConnect 이벤트: nErrCode={e.nErrCode}");
            _loginTcs?.TrySetResult(e.nErrCode);
        }

        private void Api_OnReceiveTrData(object sender, _DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            OnLog?.Invoke($"[키움] Api_OnReceiveTrData 수신: rqName=\"{e.sRQName}\" trCode=\"{e.sTrCode}\"");
            OnReceiveTrData?.Invoke(e);
        }

        private void Api_OnReceiveRealData(object sender, _DKHOpenAPIEvents_OnReceiveRealDataEvent e)
            => OnReceiveRealData?.Invoke(e.sRealKey, e.sRealType, e.sRealData);

        private void Api_OnReceiveChejanData(object sender, _DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
            => OnReceiveChejanData?.Invoke(e);

        private void Api_OnReceiveMsg(object sender, _DKHOpenAPIEvents_OnReceiveMsgEvent e)
            => OnReceiveMsg?.Invoke(e.sScrNo, e.sRQName, e.sTrCode, e.sMsg);

        private void Api_OnReceiveConditionVer(object sender, _DKHOpenAPIEvents_OnReceiveConditionVerEvent e)
            => OnReceiveConditionVer?.Invoke(e.lRet, e.sMsg);

        private void Api_OnReceiveTrCondition(object sender, _DKHOpenAPIEvents_OnReceiveTrConditionEvent e)
            => OnReceiveTrCondition?.Invoke(e.sScrNo, e.strCodeList, e.strConditionName, e.nIndex, e.nNext);

        private void Api_OnReceiveRealCondition(object sender, _DKHOpenAPIEvents_OnReceiveRealConditionEvent e)
            => OnReceiveRealCondition?.Invoke(e.strCode, e.strType, e.strConditionName, e.strConditionIndex);

        // ── API 래퍼 ──

        public string GetFirstAccount() => Accounts.Length > 0 ? Accounts[0] : "";
        public void SetInputValue(string id, string value) => _api?.SetInputValue(id, value);
        public int CommRqData(string rqName, string trCode, int prevNext, string screenNo)
            => _api?.CommRqData(rqName, trCode, prevNext, screenNo) ?? -1;
        public string GetCommData(string trCode, string rqName, int index, string itemName)
            => _api?.GetCommData(trCode, rqName, index, itemName)?.Trim() ?? "";
        public int GetRepeatCnt(string trCode, string rqName)
            => _api?.GetRepeatCnt(trCode, rqName) ?? 0;

        public object GetCommDataEx(string trCode, string rqName)
            => _api?.GetCommDataEx(trCode, rqName);
        public string GetCommRealData(string code, int fid)
            => _api?.GetCommRealData(code, fid)?.Trim() ?? "";
        public int SetRealReg(string screenNo, string codeList, string fidList, string optType)
            => _api?.SetRealReg(screenNo, codeList, fidList, optType) ?? -1;
        public void SetRealRemove(string screenNo, string code)
            => _api?.SetRealRemove(screenNo, code);
        public int SendOrder(string rqName, string screenNo, string accNo,
            int orderType, string code, int qty, int price, string hogaGb, string orgOrderNo)
            => _api?.SendOrder(rqName, screenNo, accNo, orderType, code, qty, price, hogaGb, orgOrderNo) ?? -1;
        public string GetChejanData(int fid) => _api?.GetChejanData(fid)?.Trim() ?? "";
        public string[] GetCodeListByMarket(string marketCode)
        {
            string codes = _api?.GetCodeListByMarket(marketCode) ?? "";
            return codes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public string GetMasterCodeName(string code) => _api?.GetMasterCodeName(code) ?? "";

        // 조건검색 래퍼
        public int GetConditionLoad() => _api?.GetConditionLoad() ?? 0;
        public string GetConditionNameList() => _api?.GetConditionNameList() ?? "";
        public int SendCondition(string screenNo, string conditionName, int conditionIndex, int nSearch)
            => _api?.SendCondition(screenNo, conditionName, conditionIndex, nSearch) ?? 0;
        public void SendConditionStop(string screenNo, string conditionName, int conditionIndex)
            => _api?.SendConditionStop(screenNo, conditionName, conditionIndex);
    }
}
