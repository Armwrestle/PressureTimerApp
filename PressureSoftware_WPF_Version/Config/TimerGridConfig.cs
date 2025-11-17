using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PressureTimerApp
{
    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.220.200)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=YZMES)));User Id=HNMES;Password=HNMES123;";
        public string TableName { get; set; } = "R_KEY_PART_MATERIAL";
        public string StationValidationTable { get; set; } = "TYPEDEFINITION";
    }

    public class TimerGridConfig
    {
        public int Columns { get; set; } = 13;
        public int TimerDurationSeconds { get; set; } = 3600;
        public double TimerWidth { get; set; } = 80;
        public double TimerHeight { get; set; } = 40;
        public string DefaultInputMode { get; set; } = "General";
        public string Workstation { get; set; } = "PRESSURE_01";
        public DatabaseConfig DatabaseConfig { get; set; } = new DatabaseConfig();

        public Dictionary<string, int> CustomDurations { get; set; } = new Dictionary<string, int>();
    }

    public static class ConfigManager
    {
        private static readonly string ConfigPath = "timerConfig.json";

        public static TimerGridConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<TimerGridConfig>(json);

                    // 如果配置中没有数据库连接字符串，使用默认值
                    if (config.DatabaseConfig == null)
                    {
                        config.DatabaseConfig = new DatabaseConfig();
                    }
                    else if (string.IsNullOrEmpty(config.DatabaseConfig.ConnectionString))
                    {
                        config.DatabaseConfig.ConnectionString = new DatabaseConfig().ConnectionString;
                    }

                    return config;
                }
                else
                {
                    // 如果配置文件不存在，创建默认配置
                    return CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                // 如果加载失败，返回默认配置并记录错误
                System.Diagnostics.Debug.WriteLine($"加载配置文件失败: {ex.Message}");
                return CreateDefaultConfig();
            }
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {ex.Message}");
            }
        }

        private static TimerGridConfig CreateDefaultConfig()
        {
            var config = new TimerGridConfig
            {
                Columns = 13,
                TimerDurationSeconds = 3600,
                TimerWidth = 80,
                TimerHeight = 40,
                DefaultInputMode = "General",
                DatabaseConfig = new DatabaseConfig(),
                CustomDurations = new Dictionary<string, int>()
            };

            // 保存默认配置到文件
            SaveConfig(config);
            return config;
        }
    }
}