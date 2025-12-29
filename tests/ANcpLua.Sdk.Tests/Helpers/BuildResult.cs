using System.Text.Json.Serialization;
using Meziantou.Framework;
using Microsoft.Build.Logging.StructuredLogger;

namespace ANcpLua.Sdk.Tests.Helpers;

public sealed record BuildResult(
    int ExitCode,
    ProcessOutputCollection ProcessOutput,
    SarifFile? SarifFile,
    byte[] BinaryLogContent)
{
    private Build? _cachedBuild;
    public ProcessOutputCollection Output => ProcessOutput;

    private Build GetBuild()
    {
        if (_cachedBuild is not null)
            return _cachedBuild;

        using var stream = new MemoryStream(BinaryLogContent);
        _cachedBuild = Serialization.ReadBinLog(stream);
        return _cachedBuild;
    }

    public bool OutputContains(string value, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return ProcessOutput.Any(line => line.Text.Contains(value, stringComparison));
    }

    public bool OutputDoesNotContain(string value, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return !ProcessOutput.Any(line => line.Text.Contains(value, stringComparison));
    }

    public bool HasError()
    {
        return SarifFile?.AllResults().Any(r => r.Level == "error") ?? false;
    }

    public bool HasError(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "error" && r.RuleId == ruleId) ?? false;
    }

    public bool HasWarning()
    {
        return SarifFile?.AllResults().Any(r => r.Level == "warning") ?? false;
    }

    public bool HasWarning(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "warning" && r.RuleId == ruleId) ?? false;
    }

    public bool HasNote(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "note" && r.RuleId == ruleId) ?? false;
    }

    public bool HasInfo(string ruleId)
    {
        // Info-level diagnostics may be reported as "note" or "none" in SARIF
        return SarifFile?.AllResults().Any(r =>
            r.Level is "note" or "none" && r.RuleId == ruleId) ?? false;
    }

    public IReadOnlyCollection<string> GetBinLogFiles()
    {
        var build = GetBuild();
        return [.. build.SourceFiles.Select(file => file.FullPath)];
    }

    public List<string> GetMsBuildItems(string name)
    {
        var result = new List<string>();
        var build = GetBuild();
        build.VisitAllChildren<Item>(item =>
        {
            if (item.Parent is AddItem parent && parent.Name == name) result.Add(item.Text);
        });

        return result;
    }

    public string? GetMsBuildPropertyValue(string name)
    {
        var build = GetBuild();
        return build.FindLastDescendant<Property>(e => e.Name == name)?.Value;
    }

    public void AssertMsBuildPropertyValue(string name, string? expectedValue, bool ignoreCase = true)
    {
        var build = GetBuild();
        var actual = build.FindLastDescendant<Property>(e => e.Name == name)?.Value;

        Assert.Equal(expectedValue, actual, ignoreCase);
    }

    public bool IsMsBuildTargetExecuted(string name)
    {
        var build = GetBuild();
        var target = build.FindLastDescendant<Target>(e => e.Name == name);
        if (target is null)
            return false;

        if (target.Skipped)
            return false;

        return true;
    }
}

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