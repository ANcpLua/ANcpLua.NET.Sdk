using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ANcpLua.NET.Sdk.ArchitectureLint;

/// <summary>
/// MSBuild task that validates .NET architecture patterns:
/// - Rule A: No hardcoded versions in Directory.Packages.props
/// - Rule B: Version.props import owners
/// - Rule C: Version.props symlink integrity
/// - Rule G: CPM enforcement (no inline PackageReference versions)
/// </summary>
public sealed class ArchitectureLintTask : Task
{
    /// <summary>
    /// The root directory to scan. Defaults to $(MSBuildProjectDirectory).
    /// </summary>
    [Required]
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether to treat violations as errors (true) or warnings (false).
    /// </summary>
    public bool TreatWarningsAsErrors { get; set; }

    /// <summary>
    /// Semicolon-separated list of rules to skip (e.g., "A;C").
    /// </summary>
    public string SkipRules { get; set; } = string.Empty;

    /// <summary>
    /// Number of violations found.
    /// </summary>
    [Output]
    public int ViolationCount { get; private set; }

    private HashSet<string> _skipRules = new HashSet<string>();

    public override bool Execute()
    {
        _skipRules = string.IsNullOrEmpty(SkipRules)
            ? new HashSet<string>()
            : new HashSet<string>(
                SkipRules.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()));

        var violations = new List<Violation>();

        // Rule A: Hardcoded versions in Directory.Packages.props
        if (!_skipRules.Contains("A"))
            violations.AddRange(CheckRuleA());

        // Rule B: Version.props import owners
        if (!_skipRules.Contains("B"))
            violations.AddRange(CheckRuleB());

        // Rule C: Version.props symlink integrity
        if (!_skipRules.Contains("C"))
            violations.AddRange(CheckRuleC());

        // Rule G: CPM enforcement
        if (!_skipRules.Contains("G"))
            violations.AddRange(CheckRuleG());

        ViolationCount = violations.Count;

        foreach (var v in violations)
        {
            if (TreatWarningsAsErrors)
            {
                Log.LogError(
                    subcategory: "ArchitectureLint",
                    errorCode: v.Rule,
                    helpKeyword: null,
                    file: v.File,
                    lineNumber: v.Line,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: v.Message);
            }
            else
            {
                Log.LogWarning(
                    subcategory: "ArchitectureLint",
                    warningCode: v.Rule,
                    helpKeyword: null,
                    file: v.File,
                    lineNumber: v.Line,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: v.Message);
            }
        }

        // Return true even with warnings, false only if treating as errors
        return !TreatWarningsAsErrors || violations.Count == 0;
    }

    private IEnumerable<Violation> CheckRuleA()
    {
        var dppPath = Path.Combine(RootDirectory, "Directory.Packages.props");
        if (!File.Exists(dppPath))
            yield break;

        var lines = File.ReadAllLines(dppPath);
        var hardcodedVersionPattern = new Regex(
            @"<PackageVersion\s+Include=""[^""]+""[^>]*Version=""([^$][^""]*)""",
            RegexOptions.Compiled);

        for (var i = 0; i < lines.Length; i++)
        {
            var match = hardcodedVersionPattern.Match(lines[i]);
            if (match.Success)
            {
                var version = match.Groups[1].Value;
                yield return new Violation
                {
                    Rule = "RULE_A",
                    File = dppPath,
                    Line = i + 1,
                    Message = $"Hardcoded version '{version}' detected. Use $(VariableName) from Version.props instead."
                };
            }
        }
    }

    private IEnumerable<Violation> CheckRuleB()
    {
        var allowedOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Directory.Packages.props"
        };

        var allowedPatterns = new[]
        {
            @"^eng[/\\]Directory\.Build\.props$",
            @"^src[/\\]Sdk[/\\][^/\\]+[/\\]Sdk\.props$",
            @"^src[/\\]common[/\\].*\.props$"
        };

        var propsFiles = Directory.EnumerateFiles(RootDirectory, "*.props", SearchOption.AllDirectories);
        var importPattern = new Regex(@"<Import\s+[^>]*Project=""[^""]*Version\.props", RegexOptions.Compiled);

        foreach (var file in propsFiles)
        {
            var relativePath = GetRelativePath(RootDirectory, file).Replace('\\', '/');
            var fileName = Path.GetFileName(file);

            // Check if allowed owner
            if (allowedOwners.Contains(fileName))
                continue;

            // Check allowed patterns
            var isAllowed = allowedPatterns.Any(p =>
                Regex.IsMatch(relativePath.Replace('/', Path.DirectorySeparatorChar), p, RegexOptions.IgnoreCase));
            if (isAllowed)
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (importPattern.IsMatch(lines[i]))
                {
                    yield return new Violation
                    {
                        Rule = "RULE_B",
                        File = file,
                        Line = i + 1,
                        Message = "Version.props imported outside allowed owners. Only Directory.Packages.props, eng/Directory.Build.props, or src/Sdk/*/Sdk.props may import it."
                    };
                }
            }
        }
    }

    private IEnumerable<Violation> CheckRuleC()
    {
        var vpPath = Path.Combine(RootDirectory, "Version.props");

        // Check if Version.props exists
        if (!File.Exists(vpPath) && !IsSymlink(vpPath))
            yield break;

        // Check if this is the source repo (has src/common/Version.props)
        var isSourceRepo = File.Exists(Path.Combine(RootDirectory, "src", "common", "Version.props"));

        if (isSourceRepo)
        {
            // Source repo doesn't need symlink at root
            yield break;
        }

        // Consumer repo: should be symlink
        if (!IsSymlink(vpPath))
        {
            yield return new Violation
            {
                Rule = "RULE_C",
                File = vpPath,
                Line = 0,
                Message = "Version.props should be a symlink in consumer repos, not a regular file."
            };
        }
        else if (!File.Exists(vpPath))
        {
            // Broken symlink
            yield return new Violation
            {
                Rule = "RULE_C",
                File = vpPath,
                Line = 0,
                Message = "Version.props symlink is broken (target does not exist)."
            };
        }
    }

    private IEnumerable<Violation> CheckRuleG()
    {
        var csprojFiles = Directory.EnumerateFiles(RootDirectory, "*.csproj", SearchOption.AllDirectories);
        var packageRefPattern = new Regex(
            @"<PackageReference\s+[^>]*Version=""([^$][^""]*)""",
            RegexOptions.Compiled);

        foreach (var file in csprojFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                // Skip VersionOverride (case-insensitive)
                if (lines[i].IndexOf("VersionOverride", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var match = packageRefPattern.Match(lines[i]);
                if (match.Success)
                {
                    var version = match.Groups[1].Value;
                    yield return new Violation
                    {
                        Rule = "RULE_G",
                        File = file,
                        Line = i + 1,
                        Message = $"Inline version '{version}' in PackageReference. Use Central Package Management instead."
                    };
                }
            }
        }
    }

    private static bool IsSymlink(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the relative path from one path to another. netstandard2.0 compatible.
    /// </summary>
    private static string GetRelativePath(string relativeTo, string path)
    {
        var fromUri = new Uri(EnsureTrailingSeparator(relativeTo));
        var toUri = new Uri(path);

        if (fromUri.Scheme != toUri.Scheme)
            return path;

        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

        return relativePath;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            path += Path.DirectorySeparatorChar;
        }
        return path;
    }

    private sealed class Violation
    {
        public string Rule { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public int Line { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
