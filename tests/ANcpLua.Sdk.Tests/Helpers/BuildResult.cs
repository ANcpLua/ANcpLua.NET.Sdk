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
        return [.. build.SourceFiles.Select(static file => file.FullPath)];
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

        return !target.Skipped;
    }

    /// <summary>Asserts the build succeeded (exit code 0)</summary>
    public BuildResult ShouldSucceed(string? because = null)
    {
        Assert.True(ExitCode is 0,
            because ?? $"Build should succeed. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the build failed (non-zero exit code)</summary>
    public BuildResult ShouldFail(string? because = null)
    {
        Assert.True(ExitCode is not 0,
            because ?? $"Build should fail. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the build has a specific warning</summary>
    public BuildResult ShouldHaveWarning(string ruleId)
    {
        Assert.True(HasWarning(ruleId),
            $"Expected warning {ruleId}. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the build does not have a specific warning</summary>
    public BuildResult ShouldNotHaveWarning(string ruleId)
    {
        Assert.False(HasWarning(ruleId),
            $"Did not expect warning {ruleId}. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the build has a specific error</summary>
    public BuildResult ShouldHaveError(string ruleId)
    {
        Assert.True(HasError(ruleId),
            $"Expected error {ruleId}. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the build does not have a specific error</summary>
    public BuildResult ShouldNotHaveError(string ruleId)
    {
        Assert.False(HasError(ruleId),
            $"Did not expect error {ruleId}. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the output contains a specific string</summary>
    public BuildResult ShouldContainOutput(string text)
    {
        Assert.True(OutputContains(text),
            $"Expected output to contain '{text}'. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts the output does not contain a specific string</summary>
    public BuildResult ShouldNotContainOutput(string text)
    {
        Assert.True(OutputDoesNotContain(text),
            $"Expected output to NOT contain '{text}'. Output: {ProcessOutput}");
        return this;
    }

    /// <summary>Asserts a specific MSBuild property has an expected value</summary>
    public BuildResult ShouldHavePropertyValue(string name, string? expectedValue, bool ignoreCase = true)
    {
        AssertMsBuildPropertyValue(name, expectedValue, ignoreCase);
        return this;
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