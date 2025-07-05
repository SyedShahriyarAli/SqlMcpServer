using ModelContextProtocol.Server;
using SqlMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

[McpServerToolType]
public static class SqlMcpServerTools
{
    [McpServerTool, Description("Execute a read-only SQL SELECT query.")]
    public static async Task<string> ExecuteQuery(SqlService sqlService, [Description("The SQL SELECT query to execute")] string query, [Description("Parameters for the query")] object? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(sqlService);

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));

        return JsonSerializer.Serialize(await sqlService.ExecuteSqlQueryAsync(query, parameters));
    }

    [McpServerTool, Description("Get the schema information for a specific table.")]
    public static async Task<string> GetTableSchema(SqlService sqlService, [Description("The name of the table")] string tableName)
    {
        ArgumentNullException.ThrowIfNull(sqlService);

        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        return JsonSerializer.Serialize(await sqlService.GetTableSchemaInfoAsync(tableName));
    }

    
    [McpServerTool, Description("List all available tables that can be queried.")]
    public static async Task<string> GetAvailableTables(SqlService sqlService, [Description("Optional schema name to filter tables")] string? schemaName = "")
    {
        ArgumentNullException.ThrowIfNull(sqlService);

        return JsonSerializer.Serialize(await sqlService.GetAvailableTablesAsync(schemaName));
    }


    [McpServerTool, Description("Get sample data from a table (limited to 10 rows).")]
    public static async Task<string> GetSampleData(SqlService sqlService, [Description("The name of the table")] string tableName)
    {
        ArgumentNullException.ThrowIfNull(sqlService);

        var query = $"SELECT TOP 10 * FROM [dbo].[{tableName}]";

        return JsonSerializer.Serialize(await sqlService.ExecuteSqlQueryAsync(query));
    }
}