using System.Text.Json.Serialization;
using Microsoft.Build.Logging.StructuredLogger;
using Xunit;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public sealed record BuildResult(int ExitCode, string Output, SarifFile? Sarif, byte[] BinaryLogContent)
{
    private Build? _cachedBuild;

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> RecordedProperties { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    public bool Succeeded => ExitCode is 0;

    public string? GetRecordedProperty(string name, string? targetFramework = null)
    {
        if (RecordedProperties.Count is 0)
            return null;

        if (targetFramework is null)
        {
            if (RecordedProperties.Count > 1)
                throw new InvalidOperationException(
                    $"Build produced recorded properties for {RecordedProperties.Count} target frameworks " +
                    $"({string.Join(", ", RecordedProperties.Keys)}); pass targetFramework explicitly.");

            return RecordedProperties.Values.First().GetValueOrDefault(name);
        }

        return RecordedProperties.TryGetValue(targetFramework, out var props) ? props.GetValueOrDefault(name) : null;
    }

    public bool OutputContains(string value, StringComparison comparison = StringComparison.Ordinal) =>
        Output.Contains(value, comparison);

    public bool OutputDoesNotContain(string value, StringComparison comparison = StringComparison.Ordinal) =>
        !Output.Contains(value, comparison);

    public bool HasError() => Sarif?.AllResults().Any(static r => r.Level is "error") ?? false;

    public bool HasError(string ruleId) =>
        Sarif?.AllResults().Any(r => r.Level is "error" && r.RuleId == ruleId) ?? false;

    public bool HasWarning() => Sarif?.AllResults().Any(static r => r.Level is "warning") ?? false;

    public bool HasWarning(string ruleId) =>
        Sarif?.AllResults().Any(r => r.Level is "warning" && r.RuleId == ruleId) ?? false;

    public IReadOnlyCollection<string> GetBinLogFiles() =>
        [.. GetBuild().SourceFiles.Select(static file => file.FullPath)];

    public List<string> GetMsBuildItems(string name)
    {
        var result = new List<string>();
        GetBuild().VisitAllChildren<Item>(item =>
        {
            if (item.Parent is AddItem parent && parent.Name == name)
                result.Add(item.Text);
        });
        return result;
    }

    public bool IsMsBuildTargetExecuted(string name) =>
        GetBuild().FindLastDescendant<Target>(e => e.Name == name) is { Skipped: false };

    private Build GetBuild()
    {
        if (_cachedBuild is not null)
            return _cachedBuild;

        using var stream = new MemoryStream(BinaryLogContent);
        return _cachedBuild = Serialization.ReadBinLog(stream);
    }
}

public sealed class SarifFile
{
    [JsonPropertyName("runs")]
    public SarifFileRun[]? Runs { get; init; }

    public IEnumerable<SarifFileRunResult> AllResults() => Runs?.SelectMany(static r => r.Results ?? []) ?? [];
}

public sealed class SarifFileRun
{
    [JsonPropertyName("results")]
    public SarifFileRunResult[]? Results { get; init; }
}

public sealed class SarifFileRunResult
{
    [JsonPropertyName("ruleId")]
    public string? RuleId { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("message")]
    public SarifFileRunResultMessage? Message { get; init; }

    public override string ToString() => $"{Level}:{RuleId} {Message}";
}

public sealed class SarifFileRunResultMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    public override string ToString() => Text ?? "";
}

public static class BuildResultAssertions
{
    public static BuildResult ShouldSucceed(this BuildResult result, string? because = null)
    {
        Assert.True(result.ExitCode is 0, because ?? $"Build should succeed. Output: {result.Output}");
        return result;
    }

    public static BuildResult ShouldHaveWarning(this BuildResult result, string ruleId)
    {
        Assert.True(result.HasWarning(ruleId), $"Expected warning {ruleId}. Output: {result.Output}");
        return result;
    }

    public static BuildResult ShouldNotHaveWarning(this BuildResult result, string ruleId)
    {
        Assert.False(result.HasWarning(ruleId), $"Did not expect warning {ruleId}. Output: {result.Output}");
        return result;
    }

    public static BuildResult ShouldHaveRecordedProperty(
        this BuildResult result,
        string name,
        string? expectedValue,
        string? targetFramework = null,
        bool ignoreCase = true)
    {
        var actual = result.GetRecordedProperty(name, targetFramework);
        var normalised = string.IsNullOrEmpty(actual) ? null : actual;
        Assert.Equal(expectedValue, normalised, ignoreCase);
        return result;
    }
}
