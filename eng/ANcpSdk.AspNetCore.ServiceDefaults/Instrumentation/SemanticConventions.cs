namespace ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;

/// <summary>
///     OpenTelemetry semantic convention attribute names.
/// </summary>
/// <remarks>
///     See: https://opentelemetry.io/docs/specs/semconv/
/// </remarks>
internal static class SemanticConventions
{
    /// <summary>
    ///     GenAI semantic conventions.
    /// </summary>
    /// <remarks>
    ///     See: https://opentelemetry.io/docs/specs/semconv/gen-ai/
    /// </remarks>
    public static class GenAi
    {
        /// <summary>The GenAI system (e.g., "openai", "anthropic", "ollama").</summary>
        public const string System = "gen_ai.system";

        /// <summary>The operation name (e.g., "chat", "embeddings").</summary>
        public const string OperationName = "gen_ai.operation.name";

        /// <summary>The model requested (e.g., "gpt-4o", "claude-3-opus").</summary>
        public const string RequestModel = "gen_ai.request.model";

        /// <summary>The model that generated the response.</summary>
        public const string ResponseModel = "gen_ai.response.model";

        /// <summary>Number of tokens in the input/prompt.</summary>
        public const string InputTokens = "gen_ai.usage.input_tokens";

        /// <summary>Number of tokens in the output/completion.</summary>
        public const string OutputTokens = "gen_ai.usage.output_tokens";

        /// <summary>Reasons the model stopped generating (e.g., "stop", "length").</summary>
        public const string FinishReasons = "gen_ai.response.finish_reasons";
    }

    /// <summary>
    ///     Database semantic conventions.
    /// </summary>
    /// <remarks>
    ///     See: https://opentelemetry.io/docs/specs/semconv/database/
    /// </remarks>
    public static class Db
    {
        /// <summary>The database system (e.g., "duckdb", "postgresql", "sqlite").</summary>
        public const string SystemName = "db.system.name";

        /// <summary>The database query text (SQL statement).</summary>
        public const string QueryText = "db.query.text";

        /// <summary>The operation name (e.g., "ExecuteReader", "ExecuteNonQuery").</summary>
        public const string OperationName = "db.operation.name";

        /// <summary>The database namespace (database name).</summary>
        public const string Namespace = "db.namespace";
    }
}
