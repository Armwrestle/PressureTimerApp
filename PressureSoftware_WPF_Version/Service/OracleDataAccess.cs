using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace PressureTimerApp
{
    public class PreviousStationRecord
    {
        public string LotName { get; set; }
        public string AreaName { get; set; }
        public int PressureDuration { get; set; }
        public DateTime DatabaseTime { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class OracleDataAccess
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _stationValidationTable;

        public OracleDataAccess(string connectionString, string tableName = "TIMER_RECORDS", string stationValidationTable = "TYPEDEFINITION")
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _stationValidationTable = stationValidationTable;
        }

        public bool InsertTimerRecord(TimerRecord record, string workstation)
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // 获取数据库服务器时间
                    var dbTime = GetDatabaseTime(connection);

                    // 如果 TimerCode2 不为空，说明是三输入框模式，需要在一条记录中记录两个计时器
                    if (!string.IsNullOrEmpty(record.TimerCode2))
                    {
                        // 一条记录包含两个计时器信息
                        string sql = $@"
                            INSERT INTO {_tableName} 
                            (LOTNAME, CONSUMABLENAME, ATTRIBUTE1, CONSUMABLETYPE, FACTORYNAME, 
                             AREANAME, EVENTNAME, EVENTTIMEKEY, MATERIALPOSITION, ATTRIBUTE2, ATTRIBUTE3)
                            VALUES 
                            (:Barcode, :TimerCode1, :TimerCode2, 'ZJ', 'HN_ASSY', :Workstation, 
                             'PRESSURE', TO_CHAR(SYSDATE,'YYYY/MM/DD HH24:MI:SS'), '3', :DurationSeconds, :StartTime)";

                        using (var command = new OracleCommand(sql, connection))
                        {
                            command.Parameters.Add(":Barcode", OracleDbType.Varchar2).Value = (object)record.Barcode ?? DBNull.Value;
                            command.Parameters.Add(":TimerCode1", OracleDbType.Varchar2).Value = (object)record.TimerCode1 ?? DBNull.Value;
                            command.Parameters.Add(":TimerCode2", OracleDbType.Varchar2).Value = (object)record.TimerCode2 ?? DBNull.Value;
                            command.Parameters.Add(":Workstation", OracleDbType.Varchar2).Value = workstation;
                            command.Parameters.Add(":DurationSeconds", OracleDbType.Varchar2).Value = record.DurationSeconds.ToString();
                            command.Parameters.Add(":StartTime", OracleDbType.Varchar2).Value = record.StartTime.ToString("yyyy/MM/dd HH:mm:ss");

                            int rowsAffected = command.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                    else
                    {
                        // 双输入框模式，只记录一个计时器
                        string sql = $@"
                            INSERT INTO {_tableName} 
                            (LOTNAME, CONSUMABLENAME, CONSUMABLETYPE, FACTORYNAME, 
                             AREANAME, EVENTNAME, EVENTTIMEKEY, MATERIALPOSITION, ATTRIBUTE2, ATTRIBUTE3)
                            VALUES 
                            (:Barcode, :TimerCode1, 'ZJ', 'HN_ASSY', :Workstation, 
                             'PRESSURE', TO_CHAR(SYSDATE,'YYYY/MM/DD HH24:MI:SS'), '3', :DurationSeconds, :StartTime)";

                        using (var command = new OracleCommand(sql, connection))
                        {
                            command.Parameters.Add(":Barcode", OracleDbType.Varchar2).Value = (object)record.Barcode ?? DBNull.Value;
                            command.Parameters.Add(":TimerCode1", OracleDbType.Varchar2).Value = (object)record.TimerCode1 ?? DBNull.Value;
                            command.Parameters.Add(":Workstation", OracleDbType.Varchar2).Value = workstation;
                            command.Parameters.Add(":DurationSeconds", OracleDbType.Varchar2).Value = record.DurationSeconds.ToString();
                            command.Parameters.Add(":StartTime", OracleDbType.Varchar2).Value = record.StartTime.ToString("yyyy/MM/dd HH:mm:ss");

                            int rowsAffected = command.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                }
            }
            catch (OracleException oraEx)
            {
                System.Diagnostics.Debug.WriteLine($"Oracle数据库错误: {oraEx.Message}");
                System.Diagnostics.Debug.WriteLine($"错误代码: {oraEx.Number}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库插入失败: {ex.Message}");
                return false;
            }
        }

        // 验证工站站点是否在配置表中存在
        public bool ValidateWorkstation(string workstation)
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string sql = $@"
                        SELECT COUNT(*) FROM {_stationValidationTable} 
                        WHERE TYPENAME = 'PRESSURE_CONFIG' AND TYPEVALUE = :Workstation";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(":Workstation", OracleDbType.Varchar2).Value = workstation;
                        var count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (OracleException oraEx)
            {
                System.Diagnostics.Debug.WriteLine($"工站验证失败 - Oracle错误: {oraEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"工站验证失败: {ex.Message}");
                return false;
            }
        }

        // 获取有效的工站列表
        public List<string> GetValidWorkstations()
        {
            var workstations = new List<string>();

            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string sql = $@"
                        SELECT TYPEVALUE FROM {_stationValidationTable} 
                        WHERE TYPENAME = 'PRESSURE_CONFIG' 
                        ORDER BY TYPEVALUE";

                    using (var command = new OracleCommand(sql, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            workstations.Add(reader["TYPEVALUE"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取工站列表失败: {ex.Message}");
            }

            return workstations;
        }

        private DateTime GetDatabaseTime(OracleConnection connection)
        {
            try
            {
                using (var command = new OracleCommand("SELECT SYSDATE FROM DUAL", connection))
                {
                    var result = command.ExecuteScalar();
                    return Convert.ToDateTime(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取数据库时间失败: {ex.Message}");
                return DateTime.Now;
            }
        }

        // 检查数据库连接是否正常
        public bool TestConnection()
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // 执行一个简单的查询来验证连接
                    using (var command = new OracleCommand("SELECT 1 FROM DUAL", connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) == 1;
                    }
                }
            }
            catch (OracleException oraEx)
            {
                System.Diagnostics.Debug.WriteLine($"Oracle连接测试失败 - 错误代码: {oraEx.Number}, 消息: {oraEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库连接测试失败: {ex.Message}");
                return false;
            }
        }

        // 检查表是否存在，如果不存在则创建
        public bool EnsureTableExists()
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // 检查主表是否存在
                    string checkTableSql = @"
                        SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = UPPER(:TableName)";

                    using (var command = new OracleCommand(checkTableSql, connection))
                    {
                        command.Parameters.Add(":TableName", OracleDbType.Varchar2).Value = _tableName.ToUpper();
                        var tableCount = Convert.ToInt32(command.ExecuteScalar());

                        if (tableCount == 0)
                        {
                            // 创建主表
                            string createTableSql = $@"
                                CREATE TABLE {_tableName} (
                                    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                    LOTNAME VARCHAR2(100),
                                    CONSUMABLENAME VARCHAR2(20),
                                    ATTRIBUTE1 VARCHAR2(20),
                                    CONSUMABLETYPE VARCHAR2(10),
                                    FACTORYNAME VARCHAR2(20),
                                    AREANAME VARCHAR2(20),
                                    EVENTNAME VARCHAR2(20),
                                    EVENTTIMEKEY VARCHAR2(20),
                                    MATERIALPOSITION VARCHAR2(10),
                                    ATTRIBUTE2 VARCHAR2(20),
                                    ATTRIBUTE3 VARCHAR2(20),
                                    STATION VARCHAR2(50),
                                    CREATE_TIME DATE DEFAULT SYSDATE
                                )";

                            using (var createCommand = new OracleCommand(createTableSql, connection))
                            {
                                createCommand.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"表 {_tableName} 创建成功");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"表 {_tableName} 已存在");
                        }

                        // 检查工站配置表是否存在
                        command.Parameters[":TableName"].Value = _stationValidationTable.ToUpper();
                        var stationTableCount = Convert.ToInt32(command.ExecuteScalar());

                        if (stationTableCount == 0)
                        {
                            // 创建工站配置表
                            string createStationTableSql = $@"
                                CREATE TABLE {_stationValidationTable} (
                                    STATION_NAME VARCHAR2(50) PRIMARY KEY,
                                    DESCRIPTION VARCHAR2(200),
                                    STATUS VARCHAR2(10) DEFAULT 'ACTIVE',
                                    CREATE_TIME DATE DEFAULT SYSDATE
                                )";

                            using (var createCommand = new OracleCommand(createStationTableSql, connection))
                            {
                                createCommand.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"表 {_stationValidationTable} 创建成功");

                                // 插入一些示例数据
                                string insertSampleSql = $@"
                                    INSERT INTO {_stationValidationTable} (STATION_NAME, DESCRIPTION) VALUES ('PRESSURE_01', '压力测试工站01')
                                ";

                                using (var insertCommand = new OracleCommand(insertSampleSql, connection))
                                {
                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"表 {_stationValidationTable} 已存在");
                        }

                        return true;
                    }
                }
            }
            catch (OracleException oraEx)
            {
                System.Diagnostics.Debug.WriteLine($"Oracle表创建失败 - 错误代码: {oraEx.Number}, 消息: {oraEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"确保表存在失败: {ex.Message}");
                return false;
            }
        }

        // 获取数据库信息（用于调试）
        public string GetDatabaseInfo()
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT 
                            INSTANCE_NAME, 
                            HOST_NAME, 
                            VERSION,
                            STATUS
                        FROM V$INSTANCE";

                    using (var command = new OracleCommand(sql, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return $"实例: {reader["INSTANCE_NAME"]}, 主机: {reader["HOST_NAME"]}, 版本: {reader["VERSION"]}, 状态: {reader["STATUS"]}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"获取数据库信息失败: {ex.Message}";
            }

            return "无法获取数据库信息";
        }

        // 获取工站顺序配置
        public WorkstationSequence GetWorkstationSequence(string currentWorkstation)
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string sql = $@"
                        SELECT TYPEVALUE, CUSTOMVALUE1 
                        FROM {_stationValidationTable} 
                        WHERE TYPENAME = 'PRESSURE_AREA_CONFIG' AND TYPEVALUE = :CurrentWorkstation";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(":CurrentWorkstation", OracleDbType.Varchar2).Value = currentWorkstation;

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new WorkstationSequence
                                {
                                    CurrentWorkstation = reader["TYPEVALUE"].ToString(),
                                    PreviousWorkstation = reader["CUSTOMVALUE1"]?.ToString()
                                };
                            }
                            else
                            {
                                // 没有找到记录，说明是第一个站点
                                return new WorkstationSequence
                                {
                                    CurrentWorkstation = currentWorkstation,
                                    PreviousWorkstation = null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取工站顺序配置失败: {ex.Message}");
                return new WorkstationSequence
                {
                    CurrentWorkstation = currentWorkstation,
                    PreviousWorkstation = null
                };
            }
        }

        // 查询上一工站的记录
        public PreviousStationRecord GetPreviousStationRecord(string barcode, string previousWorkstation)
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT LOTNAME, AREANAME, ATTRIBUTE2, ATTRIBUTE3, EVENTTIMEKEY 
                        FROM R_KEY_PART_MATERIAL 
                        WHERE LOTNAME = :Barcode AND AREANAME = :PreviousWorkstation 
                        ORDER BY EVENTTIMEKEY DESC";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(":Barcode", OracleDbType.Varchar2).Value = barcode;
                        command.Parameters.Add(":PreviousWorkstation", OracleDbType.Varchar2).Value = previousWorkstation;

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // 解析保压时长（字符串转整数）
                                int pressureDuration = 0;
                                if (int.TryParse(reader["ATTRIBUTE2"]?.ToString(), out int duration))
                                {
                                    pressureDuration = duration;
                                }

                                // 解析数据库时间
                                DateTime databaseTime = DateTime.Now;
                                if (DateTime.TryParse(reader["ATTRIBUTE3"]?.ToString(), out DateTime dbTime))
                                {
                                    databaseTime = dbTime;
                                }

                                // 解析创建时间
                                DateTime createTime = DateTime.Now;
                                if (DateTime.TryParse(reader["EVENTTIMEKEY"]?.ToString(), out DateTime crTime))
                                {
                                    createTime = crTime;
                                }

                                return new PreviousStationRecord
                                {
                                    LotName = reader["LOTNAME"].ToString(),
                                    AreaName = reader["AREANAME"].ToString(),
                                    PressureDuration = pressureDuration,
                                    DatabaseTime = databaseTime,
                                    CreateTime = createTime
                                };
                            }
                            else
                            {
                                return null; // 没有找到记录
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查询上一工站记录失败: {ex.Message}");
                return null;
            }
        }

        // 检查时间差是否满足保压要求
        public (bool isValid, string message) ValidatePressureTime(string barcode, WorkstationSequence sequence, int currentDuration)
        {
            try
            {
                // 如果没有上一工站，直接通过验证
                if (!sequence.HasPreviousStation)
                {
                    return (true, "当前为初始站点，无需时间验证");
                }

                // 查询上一工站记录
                var previousRecord = GetPreviousStationRecord(barcode, sequence.PreviousWorkstation);
                if (previousRecord == null)
                {
                    return (false, $"条码 {barcode} 在上一工站 {sequence.PreviousWorkstation} 没有记录");
                }

                // 获取当前数据库时间
                var currentDbTime = GetCurrentDatabaseTime();

                // 计算时间差（秒）
                var timeDifference = (currentDbTime - previousRecord.DatabaseTime).TotalSeconds;

                // 检查时间差是否大于上一工站的保压时长
                if (timeDifference >= previousRecord.PressureDuration)
                {
                    return (true, $"时间验证通过: 当前时间差 {timeDifference:F0}秒 ≥ 上一工站保压时长 {previousRecord.PressureDuration}秒");
                }
                else
                {
                    return (false, $"时间验证失败: 当前时间差 {timeDifference:F0}秒 < 上一工站保压时长 {previousRecord.PressureDuration}秒");
                }
            }
            catch (Exception ex)
            {
                return (false, $"时间验证过程中发生错误: {ex.Message}");
            }
        }

        // 获取当前数据库时间
        public DateTime GetCurrentDatabaseTime()
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    return GetDatabaseTime(connection);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取当前数据库时间失败: {ex.Message}");
                return DateTime.Now;
            }
        }
    }
}