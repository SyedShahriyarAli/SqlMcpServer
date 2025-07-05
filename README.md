# SQL MCP Server

A Model Context Protocol (MCP) server that provides secure, read-only access to SQL Server databases. This server allows AI assistants and other MCP clients to query database schemas and execute SELECT queries with built-in security restrictions.

## Features

- **Read-only Access**: Only SELECT queries are allowed - no INSERT, UPDATE, DELETE, or DDL operations
- **Schema Exploration**: Get table schemas, column information, and available tables
- **Sample Data**: Retrieve sample data from tables (limited to 10 rows)
- **Parameterized Queries**: Support for parameterized SQL queries to prevent SQL injection
- **Security Controls**: Built-in query validation and table/schema filtering
- **MCP Integration**: Full Model Context Protocol compliance for AI assistant integration

## Prerequisites

- .NET 6.0 or higher
- SQL Server (any version that supports Microsoft.Data.SqlClient)
- Visual Studio Code (recommended) or any .NET IDE

## Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd SqlMcpServer
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Update the connection string in `SqlMcpServer/Services/SqlService.cs`:
```csharp
private readonly string _connectionString = "your-connection-string-here";
```

4. Build the project:
```bash
dotnet build
```

## Configuration

### Database Connection

Update the connection string in `SqlService.cs`:
```csharp
private readonly string _connectionString = "Server=your-server;Database=your-database;Integrated Security=true;";
```

### Security Settings

The server includes several security controls that can be configured in `SqlService.cs`:

- **Allowed Tables**: Restrict access to specific tables
- **Allowed Schemas**: Restrict access to specific schemas (default: `dbo`)
- **Forbidden Keywords**: Prevent execution of dangerous SQL operations

```csharp
private readonly HashSet<string> _allowedTables = []; // Empty = all tables allowed
private readonly HashSet<string> _allowedSchemas = ["dbo"]; // Only dbo schema by default
```

### MCP Client Configuration

For Visual Studio Code with MCP support, add this configuration to your `.vscode/mcp.json`:

```json
{
    "servers": {
        "sql-mcp-server": {
            "type": "stdio",
            "command": "dotnet",
            "args": [
                "run",
                "--project",
                "path/to/SqlMcpServer/SqlMcpServer.csproj"
            ]
        }
    }
}
```