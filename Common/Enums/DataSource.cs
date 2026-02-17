namespace Common.Enums
{
    /// <summary>데이터 소스 (불변)</summary>
    public enum DataSource
    {
        None = 0,
        Kiwoom = 1,
        CybosPlus = 2,
        Ebest = 3,
        Local = 10,
        Replay = 11,
    }

    /// <summary>서버 연결 상태 (불변)</summary>
    public enum ConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Error = 99,
    }
}