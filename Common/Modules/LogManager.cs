using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace Common.Modules
{
    /// <summary>
    /// 로그 매니저 — Thread-safe 싱글톤
    /// </summary>
    public sealed class LogManager
    {
        private static readonly Lazy<LogManager> _instance =
            new Lazy<LogManager>(() => new LogManager());
        public static LogManager Instance => _instance.Value;

        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly Timer _flushTimer;
        private string _logDir;
        private string _currentFile;

        public event Action<string> OnLog;

        private LogManager()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logDir);
            _currentFile = Path.Combine(_logDir, $"{DateTime.Now:yyyyMMdd}.log");
            _flushTimer = new Timer(_ => Flush(), null, 1000, 1000);
        }

        public void SetLogDir(string dir)
        {
            _logDir = dir;
            Directory.CreateDirectory(_logDir);
            _currentFile = Path.Combine(_logDir, $"{DateTime.Now:yyyyMMdd}.log");
        }

        public void Info(string msg) => Write("INFO", msg);
        public void Warn(string msg) => Write("WARN", msg);
        public void Error(string msg) => Write("ERROR", msg);
        public void Error(string msg, Exception ex) => Write("ERROR", $"{msg} | {ex.Message}");
        public void Debug(string msg) => Write("DEBUG", msg);

        private void Write(string level, string msg)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}";
            _queue.Enqueue(line);
            OnLog?.Invoke(line);
        }

        public void Flush()
        {
            if (_queue.IsEmpty) return;
            try
            {
                using (var sw = new StreamWriter(_currentFile, true, System.Text.Encoding.UTF8))
                {
                    while (_queue.TryDequeue(out string line))
                        sw.WriteLine(line);
                }
            }
            catch { }
        }
    }
}