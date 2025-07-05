using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SqlMcpServer.Services
{
    public class SqlService(ILogger<SqlService> logger)
    {
        private readonly ILogger<SqlService> _logger = logger;
        private readonly string _connectionString = "your-connection-string-here"; // Replace with your actual connection string
        private readonly HashSet<string> _allowedTables = [];
        private readonly HashSet<string> _allowedSchemas = ["dbo"];

        private bool IsQueryAllowed(string query)
        {
            var upperQuery = query.ToUpper();

            var forbiddenKeywords = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE" };

            foreach (var keyword in forbiddenKeywords)
            {
                if (upperQuery.Contains(keyword))
                    return false;
            }

            if (_allowedTables.Count == 0)
                return true;

            return true;
        }

        private bool IsTableAllowed(string tableName, string schemaName)
        {
            if (_allowedSchemas.Count > 0 && !_allowedSchemas.Contains(schemaName))
                return false;

            if (_allowedTables.Count > 0 && !_allowedTables.Contains(tableName))
                return false;

            return true;
        }

        public async Task<object> ExecuteSqlQueryAsync(string query, object? parameters = null)
        {
            if (!IsQueryAllowed(query))
            {
                _logger.LogWarning("Query not allowed: {Query}", query);
                return new { error = "Query not allowed" };
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);

            if (parameters != null)
            {
                var paramDict = JsonSerializer.Deserialize<Dictionary<string, object>>(parameters.ToString()!);

                foreach (var param in paramDict!)
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
            }

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }

                results.Add(row);
            }

            return new { rows = results, rowCount = results.Count };
        }

        public async Task<object> GetTableSchemaInfoAsync(string tableName)
        {
            var query = @"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    IS_NULLABLE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE,
                    COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TableName", tableName);

            using var reader = await command.ExecuteReaderAsync();
            var columns = new List<object>();

            while (await reader.ReadAsync())
            {
                columns.Add(new
                {
                    name = reader.GetString(reader.GetOrdinal("COLUMN_NAME")),
                    dataType = reader.GetString(reader.GetOrdinal("DATA_TYPE")),
                    nullable = reader.GetString(reader.GetOrdinal("IS_NULLABLE")) == "YES",
                    maxLength = reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")),
                    precision = reader.IsDBNull(reader.GetOrdinal("NUMERIC_PRECISION")) ? null : (byte?)reader.GetByte(reader.GetOrdinal("NUMERIC_PRECISION")),
                    scale = reader.IsDBNull(reader.GetOrdinal("NUMERIC_SCALE")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("NUMERIC_SCALE")),
                    defaultValue = reader.IsDBNull(reader.GetOrdinal("COLUMN_DEFAULT")) ? null : reader.GetString(reader.GetOrdinal("COLUMN_DEFAULT"))
                });
            }

            return new { tableName, columns };
        }

        public async Task<object> GetAvailableTablesAsync(string? schemaName = null)
        {
            var query = @"
                SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'";

            if (!string.IsNullOrEmpty(schemaName))
            {
                query += " AND TABLE_SCHEMA = @SchemaName";
            }

            query += " ORDER BY TABLE_SCHEMA, TABLE_NAME";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            if (!string.IsNullOrEmpty(schemaName))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
            }

            using var reader = await command.ExecuteReaderAsync();
            var tables = new List<object>();

            while (await reader.ReadAsync())
            {
                var table = new
                {
                    schema = reader.GetString(reader.GetOrdinal("TABLE_SCHEMA")),
                    name = reader.GetString(reader.GetOrdinal("TABLE_NAME")),
                    type = reader.GetString(reader.GetOrdinal("TABLE_TYPE"))
                };

                if (IsTableAllowed(table.name, table.schema))
                {
                    tables.Add(table);
                }
            }

            return new { tables };
        }
    }
}