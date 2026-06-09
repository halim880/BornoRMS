# BornoBit SQL MCP server

A small **stdio MCP server** (.NET 10, official MCP C# SDK + `Microsoft.Data.SqlClient`) that lets an MCP
client query the restaurant's SQL Server (LocalDB) database. Used by Claude Code via the project's
[`.mcp.json`](../../.mcp.json).

Why a custom .NET server (not an npx/Python one): the database is **SQL Server LocalDB** with
**Windows/Trusted authentication** (`Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True`). The common
Node MSSQL MCP servers use the `tedious` driver, which can't reach LocalDB named pipes; Python ones need a
Python runtime + ODBC driver. `Microsoft.Data.SqlClient` supports LocalDB + Trusted_Connection natively.

## Tools

| Tool | Purpose |
|---|---|
| `list_tables` | List user tables (`schema.table`) with row counts |
| `describe_table` | Columns, types, lengths, nullability, primary key for a table |
| `run_query` | Run a **read-only** `SELECT` / `WITH … SELECT` (≤200 rows). Anything else is rejected. |

`run_query` is guarded: single statement only, must start with `SELECT`/`WITH`, and rejects
INSERT/UPDATE/DELETE/DROP/ALTER/CREATE/EXEC/etc. It is a guard, not a full SQL sandbox — the server still
connects with whatever rights the connection string grants.

## Build

```
dotnet build tools/BornoBit.Restaurant.Mcp
```

`.mcp.json` points at the built DLL (`bin/Debug/net10.0/BornoBit.Restaurant.Mcp.dll`), so build it once
before Claude Code launches the server. The connection string is supplied via the `CONNECTION_STRING`
environment variable in `.mcp.json` (defaults to the dev LocalDB database).

## Use in Claude Code

With `.mcp.json` at the repo root, Claude Code auto-detects the `bornobit-sql` server. Approve it when
prompted (`/mcp` lists status). Then ask things like “list the tables”, “describe the Orders table”, or
“show the 5 most recent orders”.
