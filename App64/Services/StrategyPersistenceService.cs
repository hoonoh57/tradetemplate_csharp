using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace App64.Services
{
    /// <summary>
    /// 전략 리스트를 파일(JSON)로 저장하고 불러오는 관리 서비스.
    /// 사용자가 설계한 소중한 전략들을 영구적으로 보관합니다.
    /// </summary>
    public static class StrategyPersistenceService
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "strategies.json");

        public static void SaveStrategies(List<StrategyDefinition> strategies)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(strategies, options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                // 실무 환경에서는 로그 매니저를 통해 기록
                Console.WriteLine($"전략 저장 실패: {ex.Message}");
            }
        }

        public static List<StrategyDefinition> LoadStrategies()
        {
            if (!File.Exists(FilePath)) return new List<StrategyDefinition>();

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<StrategyDefinition>>(json) ?? new List<StrategyDefinition>();
            }
            catch
            {
                return new List<StrategyDefinition>();
            }
        }
    }
}
