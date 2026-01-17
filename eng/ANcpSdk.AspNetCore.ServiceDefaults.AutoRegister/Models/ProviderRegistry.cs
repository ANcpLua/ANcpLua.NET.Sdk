using System.Collections.Immutable;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;

/// <summary>
///     Central registry of all instrumentation providers.
/// </summary>
/// <remarks>
///     <para>
///         Provider definitions for compile-time detection in the source generator.
///     </para>
///     <para>
///         Database system name mappings are in <see cref="Shared.DbSystemMappings"/> (SSOT),
///         which is shared between the generator and runtime library.
///     </para>
/// </remarks>
internal static class ProviderRegistry
{
    /// <summary>
    ///     OpenAI provider definition.
    /// </summary>
    public static readonly ProviderDefinition OpenAi = new(
        ProviderCategory.GenAi,
        "openai",
        "OpenAI",
        "OpenAI.OpenAIClient",
        "OpenAI",
        [
            new OperationDefinition("chat", "CompleteChatAsync", "CompleteChat"),
            new OperationDefinition("embeddings", "GenerateEmbeddingsAsync", "GenerateEmbeddings")
        ],
        new TokenUsageDefinition(
            "Usage.InputTokenCount",
            "Usage.OutputTokenCount"));

    /// <summary>
    ///     Anthropic provider definition.
    /// </summary>
    public static readonly ProviderDefinition Anthropic = new(
        ProviderCategory.GenAi,
        "anthropic",
        "Anthropic.SDK",
        "Anthropic.AnthropicClient",
        "Anthropic",
        [
            new OperationDefinition("chat", "CreateMessageAsync", "CreateMessage")
        ],
        new TokenUsageDefinition(
            "Usage.InputTokens",
            "Usage.OutputTokens"));

    /// <summary>
    ///     Azure OpenAI provider definition.
    /// </summary>
    public static readonly ProviderDefinition AzureOpenAi = new(
        ProviderCategory.GenAi,
        "azure_openai",
        "Azure.AI.OpenAI",
        "Azure.AI.OpenAI.OpenAIClient",
        "Azure.AI.OpenAI",
        [
            new OperationDefinition("chat", "CompleteChatAsync", "CompleteChat")
        ],
        new TokenUsageDefinition(
            "Usage.PromptTokens",
            "Usage.CompletionTokens"));

    /// <summary>
    ///     Ollama provider definition.
    /// </summary>
    public static readonly ProviderDefinition Ollama = new(
        ProviderCategory.GenAi,
        "ollama",
        "OllamaSharp",
        "OllamaSharp.OllamaApiClient",
        "Ollama",
        [
            new OperationDefinition("chat", "ChatAsync", "Chat"),
            new OperationDefinition("embeddings", "GenerateEmbeddingsAsync", "GenerateEmbeddings")
        ],
        null);

    /// <summary>
    ///     Google AI (Gemini) provider definition.
    /// </summary>
    public static readonly ProviderDefinition GoogleAi = new(
        ProviderCategory.GenAi,
        "google_ai",
        "Mscc.GenerativeAI",
        "Mscc.GenerativeAI.GoogleAI",
        "GenerativeAI",
        [
            new OperationDefinition("chat", "GenerateContentAsync", "GenerateContent")
        ],
        null);

    /// <summary>
    ///     Vertex AI provider definition.
    /// </summary>
    public static readonly ProviderDefinition VertexAi = new(
        ProviderCategory.GenAi,
        "vertex_ai",
        "Google.Cloud.AIPlatform.V1",
        "Google.Cloud.AIPlatform.V1.PredictionServiceClient",
        "AIPlatform",
        [
            new OperationDefinition("chat", "PredictAsync", "Predict")
        ],
        null);

    /// <summary>
    ///     DuckDB provider definition.
    /// </summary>
    public static readonly ProviderDefinition DuckDb = new(
        ProviderCategory.Database,
        "duckdb",
        "DuckDB.NET.Data",
        "DuckDB.NET.Data.DuckDBCommand",
        "DuckDB",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     SQLite (Microsoft.Data.Sqlite) provider definition.
    /// </summary>
    public static readonly ProviderDefinition SqliteMicrosoft = new(
        ProviderCategory.Database,
        "sqlite",
        "Microsoft.Data.Sqlite",
        "Microsoft.Data.Sqlite.SqliteCommand",
        "Sqlite",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     SQLite (System.Data.SQLite) provider definition.
    /// </summary>
    public static readonly ProviderDefinition SqliteSystem = new(
        ProviderCategory.Database,
        "sqlite",
        "System.Data.SQLite",
        "System.Data.SQLite.SQLiteCommand",
        "SQLite",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     PostgreSQL (Npgsql) provider definition.
    /// </summary>
    public static readonly ProviderDefinition PostgreSql = new(
        ProviderCategory.Database,
        "postgresql",
        "Npgsql",
        "Npgsql.NpgsqlCommand",
        "Npgsql",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     MySQL (MySqlConnector) provider definition.
    /// </summary>
    public static readonly ProviderDefinition MySqlConnector = new(
        ProviderCategory.Database,
        "mysql",
        "MySqlConnector",
        "MySqlConnector.MySqlCommand",
        "MySql",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     MySQL (MySql.Data) provider definition.
    /// </summary>
    public static readonly ProviderDefinition MySqlData = new(
        ProviderCategory.Database,
        "mysql",
        "MySql.Data",
        "MySql.Data.MySqlClient.MySqlCommand",
        "MySql",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     SQL Server (Microsoft.Data.SqlClient) provider definition.
    /// </summary>
    public static readonly ProviderDefinition SqlServerMicrosoft = new(
        ProviderCategory.Database,
        "mssql",
        "Microsoft.Data.SqlClient",
        "Microsoft.Data.SqlClient.SqlCommand",
        "SqlClient",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     SQL Server (System.Data.SqlClient) provider definition.
    /// </summary>
    public static readonly ProviderDefinition SqlServerSystem = new(
        ProviderCategory.Database,
        "mssql",
        "System.Data.SqlClient",
        "System.Data.SqlClient.SqlCommand",
        "SqlClient",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     Oracle provider definition.
    /// </summary>
    public static readonly ProviderDefinition Oracle = new(
        ProviderCategory.Database,
        "oracle",
        "Oracle.ManagedDataAccess",
        "Oracle.ManagedDataAccess.Client.OracleCommand",
        "Oracle",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     Firebird provider definition.
    /// </summary>
    public static readonly ProviderDefinition Firebird = new(
        ProviderCategory.Database,
        "firebird",
        "FirebirdSql.Data.FirebirdClient",
        "FirebirdSql.Data.FirebirdClient.FbCommand",
        "Firebird",
        ImmutableArray<OperationDefinition>.Empty,
        null);

    /// <summary>
    ///     All GenAI providers.
    /// </summary>
    public static readonly ImmutableArray<ProviderDefinition> GenAiProviders =
    [
        OpenAi,
        Anthropic,
        AzureOpenAi,
        Ollama,
        GoogleAi,
        VertexAi
    ];

    /// <summary>
    ///     All database providers.
    /// </summary>
    public static readonly ImmutableArray<ProviderDefinition> DatabaseProviders =
    [
        DuckDb,
        SqliteMicrosoft,
        SqliteSystem,
        PostgreSql,
        MySqlConnector,
        MySqlData,
        SqlServerMicrosoft,
        SqlServerSystem,
        Oracle,
        Firebird
    ];

    /// <summary>
    ///     All providers.
    /// </summary>
    public static readonly ImmutableArray<ProviderDefinition> AllProviders =
        GenAiProviders.AddRange(DatabaseProviders);
}

/// <summary>
///     Defines an instrumentation provider.
/// </summary>
internal sealed record ProviderDefinition(
    ProviderCategory Category,
    string ProviderId,
    string AssemblyName,
    string PrimaryTypeName,
    string TypeContains,
    ImmutableArray<OperationDefinition> Operations,
    TokenUsageDefinition? TokenUsage);

/// <summary>
///     Defines an operation within a provider.
/// </summary>
internal sealed record OperationDefinition(
    string OperationId,
    string AsyncMethodName,
    string SyncMethodName);

/// <summary>
///     Defines how to extract token usage from a response.
/// </summary>
internal sealed record TokenUsageDefinition(
    string InputProperty,
    string OutputProperty);
