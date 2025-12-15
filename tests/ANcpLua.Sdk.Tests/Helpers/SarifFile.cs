using System.Text.Json.Serialization;

namespace ANcpLua.Sdk.Tests.Helpers;

public class SarifFile
{
    [JsonPropertyName("runs")] public SarifFileRun[]? Runs { get; set; }

    public IEnumerable<SarifFileRunResult> AllResults()
    {
        return Runs?.SelectMany(r => r.Results ?? []) ?? [];
    }
}

public class SarifFileRunResult
{
    [JsonPropertyName("ruleId")] public string? RuleId { get; set; }

    [JsonPropertyName("level")] public string? Level { get; set; }

    [JsonPropertyName("message")] public SarifFileRunResultMessage? Message { get; set; }

    public override string ToString()
    {
        return $"{Level}:{RuleId} {Message}";
    }
}

public class SarifFileRunResultMessage
{
    [JsonPropertyName("text")] public string? Text { get; set; }

    public override string ToString()
    {
        return Text ?? "";
    }
}

public class SarifFileRun
{
    [JsonPropertyName("results")] public SarifFileRunResult[]? Results { get; set; }
}