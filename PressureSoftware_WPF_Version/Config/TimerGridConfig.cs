using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PressureTimerApp
{
    public class TimerGridConfig
    {
        public int Columns { get; set; } = 13;
        public int TimerDurationSeconds { get; set; } = 3600;
        public double TimerWidth { get; set; } = 80;
        public double TimerHeight { get; set; } = 40;
        public string DefaultInputMode { get; set; } = "General";

        public Dictionary<string, int> CustomDurations { get; set; } = new Dictionary<string, int>();
    }

    public class ConfigManager
    {
        private static readonly string ConfigPath = "timerConfig.json";

        public static TimerGridConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<TimerGridConfig>(json);
                }
            }
            catch
            {
                // 如果加载失败，返回默认配置
            }

            // 默认配置：3600秒 = 1小时
            return new TimerGridConfig 
            {
                Columns = 10,
                TimerDurationSeconds = 3600,
                TimerWidth = 60,
                TimerHeight = 30,
                DefaultInputMode = "General",
                CustomDurations = new Dictionary<string, int>()
            };
        }

        public static void SaveConfig(TimerGridConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // 保存失败处理
            }
        }
    }
}