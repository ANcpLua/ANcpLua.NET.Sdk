using System.Diagnostics;

namespace ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation.GenAi;

/// <summary>
///     Extension methods for setting GenAI semantic convention attributes on Activities.
/// </summary>
/// <remarks>
///     Provides fluent API for OTel 1.39 GenAI semantic conventions.
/// </remarks>
public static class GenAiActivityExtensions
{
    /// <summary>
    ///     Sets GenAI request attributes on the activity.
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="model">The model requested (e.g., "gpt-4o", "claude-3-opus").</param>
    /// <param name="temperature">Temperature setting (0.0-2.0).</param>
    /// <param name="maxTokens">Maximum tokens to generate.</param>
    /// <param name="topP">Nucleus sampling threshold.</param>
    /// <param name="topK">Top-k sampling parameter.</param>
    /// <returns>The activity for fluent chaining.</returns>
    public static Activity SetGenAiRequest(
        this Activity activity,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        double? topP = null,
        int? topK = null)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (model is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.RequestModel, model);

        if (temperature.HasValue)
            activity.SetTag(SemanticConventions.GenAi.RequestTemperature, temperature.Value);

        if (maxTokens.HasValue)
            activity.SetTag(SemanticConventions.GenAi.RequestMaxTokens, maxTokens.Value);

        if (topP.HasValue)
            activity.SetTag(SemanticConventions.GenAi.RequestTopP, topP.Value);

        if (topK.HasValue)
            activity.SetTag(SemanticConventions.GenAi.RequestTopK, topK.Value);

        return activity;
    }

    /// <summary>
    ///     Sets GenAI token usage attributes on the activity.
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="inputTokens">Number of input/prompt tokens.</param>
    /// <param name="outputTokens">Number of output/completion tokens.</param>
    /// <param name="cachedTokens">Number of cached input tokens (Anthropic).</param>
    /// <param name="reasoningTokens">Number of reasoning tokens (o1-style models).</param>
    /// <returns>The activity for fluent chaining.</returns>
    public static Activity SetGenAiUsage(
        this Activity activity,
        long? inputTokens = null,
        long? outputTokens = null,
        long? cachedTokens = null,
        long? reasoningTokens = null)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (inputTokens.HasValue)
            activity.SetTag(SemanticConventions.GenAi.UsageInputTokens, inputTokens.Value);

        if (outputTokens.HasValue)
            activity.SetTag(SemanticConventions.GenAi.UsageOutputTokens, outputTokens.Value);

        if (cachedTokens.HasValue)
            activity.SetTag(SemanticConventions.GenAi.UsageInputTokensCached, cachedTokens.Value);

        if (reasoningTokens.HasValue)
            activity.SetTag(SemanticConventions.GenAi.UsageOutputTokensReasoning, reasoningTokens.Value);

        return activity;
    }

    /// <summary>
    ///     Sets GenAI response attributes on the activity.
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="model">The model that generated the response.</param>
    /// <param name="responseId">Unique completion identifier.</param>
    /// <param name="finishReasons">Reasons the model stopped generating.</param>
    /// <returns>The activity for fluent chaining.</returns>
    public static Activity SetGenAiResponse(
        this Activity activity,
        string? model = null,
        string? responseId = null,
        string[]? finishReasons = null)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (model is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.ResponseModel, model);

        if (responseId is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.ResponseId, responseId);

        if (finishReasons is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.ResponseFinishReasons, finishReasons);

        return activity;
    }

    /// <summary>
    ///     Sets GenAI agent attributes on the activity (OTel 1.38+).
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="agentId">Unique agent identifier.</param>
    /// <param name="agentName">Human-readable agent name.</param>
    /// <param name="agentDescription">Agent description.</param>
    /// <returns>The activity for fluent chaining.</returns>
    public static Activity SetGenAiAgent(
        this Activity activity,
        string? agentId = null,
        string? agentName = null,
        string? agentDescription = null)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (agentId is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.AgentId, agentId);

        if (agentName is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.AgentName, agentName);

        if (agentDescription is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.AgentDescription, agentDescription);

        return activity;
    }

    /// <summary>
    ///     Sets GenAI tool attributes on the activity (OTel 1.39).
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="toolName">Tool name.</param>
    /// <param name="toolCallId">Tool call identifier.</param>
    /// <param name="conversationId">Session/thread identifier for multi-turn conversations.</param>
    /// <returns>The activity for fluent chaining.</returns>
    public static Activity SetGenAiTool(
        this Activity activity,
        string? toolName = null,
        string? toolCallId = null,
        string? conversationId = null)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (toolName is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.ToolName, toolName);

        if (toolCallId is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.ToolCallId, toolCallId);

        if (conversationId is { Length: > 0 })
            activity.SetTag(SemanticConventions.GenAi.ConversationId, conversationId);

        return activity;
    }
}
