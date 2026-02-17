using System;
using System.IO;
using System.Collections.Generic;

namespace Common.Modules
{
    /// <summary>
    /// 설정 매니저 — Thread-safe 싱글톤
    /// INI 형태의 간단한 Key=Value 설정 관리
    /// </summary>
    public sealed class ConfigManager
    {
        private static readonly Lazy<ConfigManager> _instance =
            new Lazy<ConfigManager>(() => new ConfigManager());
        public static ConfigManager Instance => _instance.Value;

        private readonly Dictionary<string, string> _config = new Dictionary<string, string>();
        private string _filePath;
        private readonly object _lock = new object();

        private ConfigManager() { }

        public void Load(string filePath)
        {
            _filePath = filePath;
            lock (_lock)
            {
                _config.Clear();
                if (!File.Exists(filePath)) return;
                foreach (var line in File.ReadAllLines(filePath))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                        continue;
                    int eq = trimmed.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = trimmed.Substring(0, eq).Trim();
                    string val = trimmed.Substring(eq + 1).Trim();
                    _config[key] = val;
                }
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_filePath)) return;
            lock (_lock)
            {
                var lines = new List<string>();
                foreach (var kv in _config)
                    lines.Add($"{kv.Key}={kv.Value}");
                File.WriteAllLines(_filePath, lines);
            }
        }

        public string Get(string key, string defaultValue = "")
        {
            lock (_lock)
                return _config.TryGetValue(key, out string val) ? val : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            string val = Get(key);
            return int.TryParse(val, out int result) ? result : defaultValue;
        }

        public double GetDouble(string key, double defaultValue = 0)
        {
            string val = Get(key);
            return double.TryParse(val, out double result) ? result : defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            string val = Get(key).ToLower();
            if (val == "true" || val == "1" || val == "yes") return true;
            if (val == "false" || val == "0" || val == "no") return false;
            return defaultValue;
        }

        public void Set(string key, string value)
        {
            lock (_lock)
                _config[key] = value;
        }

        public void Set(string key, int value) => Set(key, value.ToString());
        public void Set(string key, double value) => Set(key, value.ToString());
        public void Set(string key, bool value) => Set(key, value.ToString().ToLower());
    }
}