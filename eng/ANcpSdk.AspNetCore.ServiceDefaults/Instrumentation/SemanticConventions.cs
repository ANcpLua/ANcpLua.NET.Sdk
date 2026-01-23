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
    ///     GenAI semantic conventions (OTel 1.39).
    /// </summary>
    /// <remarks>
    ///     See: https://opentelemetry.io/docs/specs/semconv/gen-ai/
    /// </remarks>
    public static class GenAi
    {
        /// <summary>Schema URL for OTel 1.39 semantic conventions.</summary>
        public const string SchemaUrl = "https://opentelemetry.io/schemas/1.39.0";

        // =====================================================================
        // CORE ATTRIBUTES (Required/Conditionally Required)
        // =====================================================================

        /// <summary>gen_ai.provider.name - The GenAI provider (e.g., "openai", "anthropic").</summary>
        /// <remarks>OTel 1.37+: Replaces deprecated gen_ai.system</remarks>
        public const string ProviderName = "gen_ai.provider.name";

        /// <summary>gen_ai.operation.name - The operation being performed (e.g., "chat", "embeddings").</summary>
        public const string OperationName = "gen_ai.operation.name";

        // =====================================================================
        // REQUEST PARAMETERS
        // =====================================================================

        /// <summary>gen_ai.request.model - The model requested (e.g., "gpt-4o", "claude-3-opus").</summary>
        public const string RequestModel = "gen_ai.request.model";

        /// <summary>gen_ai.request.temperature - Temperature setting (0.0-2.0).</summary>
        public const string RequestTemperature = "gen_ai.request.temperature";

        /// <summary>gen_ai.request.max_tokens - Maximum tokens to generate.</summary>
        public const string RequestMaxTokens = "gen_ai.request.max_tokens";

        /// <summary>gen_ai.request.top_p - Nucleus sampling threshold.</summary>
        public const string RequestTopP = "gen_ai.request.top_p";

        /// <summary>gen_ai.request.top_k - Top-k sampling parameter.</summary>
        public const string RequestTopK = "gen_ai.request.top_k";

        /// <summary>gen_ai.request.stop_sequences - Stop sequence array.</summary>
        public const string RequestStopSequences = "gen_ai.request.stop_sequences";

        /// <summary>gen_ai.request.frequency_penalty - Frequency penalty (-2.0 to 2.0).</summary>
        public const string RequestFrequencyPenalty = "gen_ai.request.frequency_penalty";

        /// <summary>gen_ai.request.presence_penalty - Presence penalty (-2.0 to 2.0).</summary>
        public const string RequestPresencePenalty = "gen_ai.request.presence_penalty";

        /// <summary>gen_ai.request.seed - Reproducibility seed.</summary>
        public const string RequestSeed = "gen_ai.request.seed";

        // =====================================================================
        // RESPONSE ATTRIBUTES
        // =====================================================================

        /// <summary>gen_ai.response.model - The model that generated the response.</summary>
        public const string ResponseModel = "gen_ai.response.model";

        /// <summary>gen_ai.response.id - Unique completion identifier.</summary>
        public const string ResponseId = "gen_ai.response.id";

        /// <summary>gen_ai.response.finish_reasons - Reasons the model stopped generating.</summary>
        public const string ResponseFinishReasons = "gen_ai.response.finish_reasons";

        // =====================================================================
        // USAGE/TOKENS (OTel 1.37+ naming)
        // =====================================================================

        /// <summary>gen_ai.usage.input_tokens - Number of tokens in the input/prompt.</summary>
        public const string UsageInputTokens = "gen_ai.usage.input_tokens";

        /// <summary>gen_ai.usage.output_tokens - Number of tokens in the output/completion.</summary>
        public const string UsageOutputTokens = "gen_ai.usage.output_tokens";

        /// <summary>gen_ai.usage.input_tokens.cached - Cached input tokens (Anthropic).</summary>
        public const string UsageInputTokensCached = "gen_ai.usage.input_tokens.cached";

        /// <summary>gen_ai.usage.output_tokens.reasoning - Reasoning output tokens (o1-style models).</summary>
        public const string UsageOutputTokensReasoning = "gen_ai.usage.output_tokens.reasoning";

        // =====================================================================
        // AGENT ATTRIBUTES (OTel 1.38+)
        // =====================================================================

        /// <summary>gen_ai.agent.id - Unique agent identifier.</summary>
        public const string AgentId = "gen_ai.agent.id";

        /// <summary>gen_ai.agent.name - Human-readable agent name.</summary>
        public const string AgentName = "gen_ai.agent.name";

        /// <summary>gen_ai.agent.description - Agent description.</summary>
        public const string AgentDescription = "gen_ai.agent.description";

        // =====================================================================
        // TOOL ATTRIBUTES (OTel 1.39)
        // =====================================================================

        /// <summary>gen_ai.tool.name - Tool name.</summary>
        public const string ToolName = "gen_ai.tool.name";

        /// <summary>gen_ai.tool.call.id - Tool call identifier.</summary>
        public const string ToolCallId = "gen_ai.tool.call.id";

        /// <summary>gen_ai.conversation.id - Session/thread identifier.</summary>
        public const string ConversationId = "gen_ai.conversation.id";

        // =====================================================================
        // DEPRECATED (for backward compatibility)
        // =====================================================================

        /// <summary>
        ///     Deprecated attribute names (pre-1.37). Use for migration/normalization.
        /// </summary>
        public static class Deprecated
        {
            /// <summary>gen_ai.system - DEPRECATED: Use ProviderName instead.</summary>
            public const string System = "gen_ai.system";

            /// <summary>gen_ai.usage.prompt_tokens - DEPRECATED: Use UsageInputTokens instead.</summary>
            public const string UsagePromptTokens = "gen_ai.usage.prompt_tokens";

            /// <summary>gen_ai.usage.completion_tokens - DEPRECATED: Use UsageOutputTokens instead.</summary>
            public const string UsageCompletionTokens = "gen_ai.usage.completion_tokens";
        }
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
