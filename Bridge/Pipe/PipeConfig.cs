namespace Bridge.Pipe
{
    /// <summary>
    /// Named Pipe 설정 — 불변
    /// </summary>
    public sealed class PipeConfig
    {
        /// <summary>파이프 이름 (기본값)</summary>
        public string PipeName { get; }

        /// <summary>수신 버퍼 크기 (bytes)</summary>
        public int BufferSize { get; }

        /// <summary>최대 동시 연결 수</summary>
        public int MaxConnections { get; }

        /// <summary>연결 타임아웃 (ms)</summary>
        public int ConnectTimeout { get; }

        /// <summary>읽기 타임아웃 (ms)</summary>
        public int ReadTimeout { get; }

        public static readonly PipeConfig Default = new PipeConfig(
            "TradingBridge", 1024 * 64, 4, 5000, 1000);

        public PipeConfig(string pipeName, int bufferSize, int maxConnections,
            int connectTimeout, int readTimeout)
        {
            PipeName = pipeName ?? "TradingBridge";
            BufferSize = bufferSize;
            MaxConnections = maxConnections;
            ConnectTimeout = connectTimeout;
            ReadTimeout = readTimeout;
        }
    }
}