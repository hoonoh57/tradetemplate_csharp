using System;
using System.Runtime.InteropServices;

namespace Server32.Kiwoom
{
    /// <summary>
    /// 키움 OpenAPI+ 접속 관리자 — 불변 패턴
    /// COM ProgID: KHOpenAPI.KHOpenAPICtrl.1
    /// </summary>
    public sealed class KiwoomConnector
    {
        private dynamic _api;
        private bool _connected;

        /// <summary>COM API 인스턴스 (다른 모듈에서 사용)</summary>
        public dynamic Api => _api;

        /// <summary>접속 상태</summary>
        public bool IsConnected => _connected;

        /// <summary>모의투자 여부</summary>
        public bool IsSimulation { get; private set; }

        /// <summary>계좌 목록</summary>
        public string[] Accounts { get; private set; } = Array.Empty<string>();

        /// <summary>초기화 (COM 생성 + 접속 확인)</summary>
        public bool Initialize()
        {
            try
            {
                Type comType = Type.GetTypeFromProgID("KHOpenAPI.KHOpenAPICtrl.1");
                if (comType == null) return false;

                _api = Activator.CreateInstance(comType);
                if (_api == null) return false;

                // 접속 상태 확인
                int state = (int)_api.CommConnect();
                // CommConnect는 로그인 창을 띄움 → 이벤트 기반
                // 여기서는 이미 로그인된 상태를 가정
                _connected = (int)_api.GetConnectState() == 1;

                if (_connected)
                {
                    string accList = _api.GetLoginInfo("ACCLIST")?.ToString() ?? "";
                    Accounts = accList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    IsSimulation = _api.GetLoginInfo("GetServerGubun")?.ToString() == "1";
                }

                return _connected;
            }
            catch (COMException)
            {
                _connected = false;
                return false;
            }
            catch (Exception)
            {
                _connected = false;
                return false;
            }
        }

        /// <summary>첫 번째 계좌번호</summary>
        public string GetFirstAccount()
        {
            return Accounts.Length > 0 ? Accounts[0] : "";
        }
    }
}