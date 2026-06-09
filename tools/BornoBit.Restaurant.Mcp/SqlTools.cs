using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;

namespace BornoBit.Restaurant.Mcp;

[McpServerToolType]
public static class SqlTools
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("CONNECTION_STRING")
        ?? throw new InvalidOperationException("CONNECTION_STRING environment variable is not set.");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    [McpServerTool(Name = "list_tables"), Description("List all user tables in the database as schema.table, with row counts.")]
    public static async Task<string> ListTables()
    {
        const string sql = """
            SELECT s.name AS [schema], t.name AS [table],
                   SUM(p.rows) AS [rows]
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
            GROUP BY s.name, t.name
            ORDER BY s.name, t.name;
            """;
        return await QueryToJsonAsync(sql, null);
    }

    [McpServerTool(Name = "describe_table"), Description("Describe a table's columns: name, type, length, nullability, primary key. Arg 'table' may be 'schema.table' or just 'table' (defaults to dbo).")]
    public static async Task<string> DescribeTable(
        [Description("Table name, optionally schema-qualified, e.g. 'dbo.Orders' or 'Orders'.")] string table)
    {
        var (schema, name) = SplitTable(table);
        const string sql = """
            SELECT c.name AS [column],
                   ty.name AS [type],
                   c.max_length AS [maxLength],
                   c.is_nullable AS [nullable],
                   CAST(CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [primaryKey]
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            OUTER APPLY (
                SELECT ic.column_id
                FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id
            ) pk
            WHERE s.name = @schema AND t.name = @table
            ORDER BY c.column_id;
            """;
        var result = await QueryToJsonAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@schema", schema);
            cmd.Parameters.AddWithValue("@table", name);
        });
        return result == "[]"
            ? $"No table found named '{schema}.{name}'. Use list_tables to see available tables."
            : result;
    }

    [McpServerTool(Name = "run_query"), Description("Run a READ-ONLY SQL query (SELECT or WITH...SELECT) and return up to 200 rows as JSON. Non-read statements are rejected.")]
    public static async Task<string> RunQuery(
        [Description("A single read-only SELECT statement.")] string sql)
    {
        if (!IsReadOnly(sql))
            return "Rejected: only a single read-only SELECT (or WITH ... SELECT) statement is allowed.";

        return await QueryToJsonAsync(sql, null, maxRows: 200);
    }

    // ---- helpers ----

    private static (string schema, string name) SplitTable(string table)
    {
        var parts = table.Trim().Replace("[", "").Replace("]", "").Split('.', 2);
        return parts.Length == 2 ? (parts[0].Trim(), parts[1].Trim()) : ("dbo", parts[0].Trim());
    }

    private static bool IsReadOnly(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var trimmed = sql.Trim().TrimEnd(';').TrimStart();

        // single statement only
        if (trimmed.Contains(';')) return false;

        var upper = trimmed.ToUpperInvariant();
        if (!(upper.StartsWith("SELECT") || upper.StartsWith("WITH"))) return false;

        string[] forbidden = { "INSERT ", "UPDATE ", "DELETE ", "DROP ", "ALTER ", "CREATE ",
                               "TRUNCATE", "MERGE ", "EXEC", "GRANT ", "REVOKE", "BACKUP", "RESTORE",
                               "SP_", "XP_", "INTO " };
        return !forbidden.Any(f => upper.Contains(f));
    }

    private static async Task<string> QueryToJsonAsync(string sql, Action<SqlCommand>? configure, int maxRows = 1000)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        configure?.Invoke(cmd);

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync() && rows.Count < maxRows)
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = value;
            }
            rows.Add(row);
        }

        return JsonSerializer.Serialize(rows, Json);
    }
}
