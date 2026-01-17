namespace ANcpSdk.AspNetCore.ServiceDefaults.Shared;

/// <summary>
///     Single Source of Truth for database system mappings.
///     Used by both the source generator (compile-time) and runtime library.
/// </summary>
/// <remarks>
///     This file is shared between:
///     - ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister (netstandard2.0)
///     - ANcpSdk.AspNetCore.ServiceDefaults (net10.0)
/// </remarks>
internal static class DbSystemMappings
{
    /// <summary>
    ///     Maps a type name to the OTel db.system.name semantic convention value.
    /// </summary>
    /// <param name="typeName">The full or partial type name (e.g., "DuckDB.NET.Data.DuckDBConnection").</param>
    /// <returns>The db.system.name value (e.g., "duckdb"), or "unknown" if not recognized.</returns>
    public static string MapTypeNameToDbSystem(string typeName) =>
        typeName switch
        {
            _ when typeName.Contains("DuckDB", StringComparison.OrdinalIgnoreCase) => "duckdb",
            _ when typeName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "postgresql",
            _ when typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) => "mssql",
            _ when typeName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) => "sqlite",
            _ when typeName.Contains("Oracle", StringComparison.OrdinalIgnoreCase) => "oracle",
            _ when typeName.Contains("MySql", StringComparison.OrdinalIgnoreCase) => "mysql",
            _ when typeName.Contains("Firebird", StringComparison.OrdinalIgnoreCase) => "firebird",
            _ => "unknown"
        };

    /// <summary>
    ///     Database provider definitions with their type patterns and system names.
    /// </summary>
    public static readonly (string TypeContains, string SystemName)[] Providers =
    [
        ("DuckDB", "duckdb"),
        ("Npgsql", "postgresql"),
        ("SqlClient", "mssql"),
        ("Sqlite", "sqlite"),
        ("Oracle", "oracle"),
        ("MySql", "mysql"),
        ("Firebird", "firebird")
    ];
}
