using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace PressureTimerApp
{
    public class OracleDataAccess
    {
        private readonly string _connectionString;
        private readonly string _tableName;

        public OracleDataAccess(string connectionString, string tableName = "R_KEY_PART_MATERIAL")
        {
            _connectionString = connectionString;
            _tableName = tableName;
        }

        public bool InsertTimerRecord(TimerRecord record)
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // 获取数据库服务器时间
                    var dbTime = GetDatabaseTime(connection);

                    string sql = $@"
                        INSERT INTO {_tableName} 
                        (BARCODE, TIMER_CODE1, TIMER_CODE2, DURATION_SECONDS, START_TIME, DATABASE_TIME, INPUT_MODE, CREATE_TIME)
                        VALUES 
                        (:Barcode, :TimerCode1, :TimerCode2, :DurationSeconds, :StartTime, :DatabaseTime, :InputMode, SYSDATE)";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(":Barcode", OracleDbType.Varchar2).Value = (object)record.Barcode ?? DBNull.Value;
                        command.Parameters.Add(":TimerCode1", OracleDbType.Varchar2).Value = (object)record.TimerCode1 ?? DBNull.Value;
                        command.Parameters.Add(":TimerCode2", OracleDbType.Varchar2).Value = (object)record.TimerCode2 ?? DBNull.Value;
                        command.Parameters.Add(":DurationSeconds", OracleDbType.Int32).Value = record.DurationSeconds;
                        command.Parameters.Add(":StartTime", OracleDbType.Date).Value = record.StartTime;
                        command.Parameters.Add(":DatabaseTime", OracleDbType.Date).Value = dbTime;
                        command.Parameters.Add(":InputMode", OracleDbType.Varchar2).Value = record.InputMode;

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库插入失败: {ex.Message}");
                return false;
            }
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
                    return true;
                }
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

                    // 检查表是否存在
                    string checkTableSql = @"
                        SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = UPPER(:TableName)";

                    using (var command = new OracleCommand(checkTableSql, connection))
                    {
                        command.Parameters.Add(":TableName", OracleDbType.Varchar2).Value = _tableName;
                        var tableCount = Convert.ToInt32(command.ExecuteScalar());

                        if (tableCount == 0)
                        {
                            // 创建表
                            string createTableSql = $@"
                                CREATE TABLE {_tableName} (
                                    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                    BARCODE VARCHAR2(100),
                                    TIMER_CODE1 VARCHAR2(20),
                                    TIMER_CODE2 VARCHAR2(20),
                                    DURATION_SECONDS NUMBER(10),
                                    START_TIME DATE,
                                    DATABASE_TIME DATE,
                                    INPUT_MODE VARCHAR2(20),
                                    CREATE_TIME DATE DEFAULT SYSDATE
                                )";

                            using (var createCommand = new OracleCommand(createTableSql, connection))
                            {
                                createCommand.ExecuteNonQuery();
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"确保表存在失败: {ex.Message}");
                return false;
            }
        }
    }
}