using System.Text.Json;
using System.Text.RegularExpressions;

var rootDir = args.Length > 0 ? args[0] : ".";
var format = args.Contains("--format")
    ? args[Array.IndexOf(args, "--format") + 1]
    : "compact";

var violations = ArchitectureLinter.Analyze(rootDir);

// Output based on format
switch (format)
{
    case "json":
        Console.WriteLine(JsonSerializer.Serialize(violations, new JsonSerializerOptions { WriteIndented = true }));
        break;

    case "github":
        foreach (var v in violations)
            Console.WriteLine($"::{(v.Severity == "ERROR" ? "error" : "warning")} file={v.File},line={v.Line}::[{v.Rule}] {v.Message}");
        break;

    case "compact":
    default:
        foreach (var v in violations)
            Console.WriteLine($"{v.Severity}: {v.File}:{v.Line} [{v.Rule}] {v.Message}");

        if (violations.Count == 0)
            Console.WriteLine("CLEAN: All architecture rules passed");
        else
            Console.WriteLine($"\n{violations.Count} violation(s) found");
        break;
}

return violations.Any(v => v.Severity == "ERROR") ? 1 : 0;

public static class ArchitectureLinter
{
    public static List<Violation> Analyze(string rootDir)
    {
        var violations = new List<Violation>();

        violations.AddRange(CheckRuleA(rootDir));
        violations.AddRange(CheckRuleB(rootDir));
        violations.AddRange(CheckRuleC(rootDir));
        violations.AddRange(CheckRuleG(rootDir));

        return violations;
    }

    /// <summary>
    /// Rule A: No hardcoded versions in Directory.Packages.props
    /// </summary>
    private static IEnumerable<Violation> CheckRuleA(string rootDir)
    {
        var dppPath = Path.Combine(rootDir, "Directory.Packages.props");
        if (!File.Exists(dppPath))
            yield break;

        var lines = File.ReadAllLines(dppPath);
        var pattern = new Regex(@"<PackageVersion\s+Include=""[^""]+""[^>]*Version=""([^$][^""]*)""");

        for (var i = 0; i < lines.Length; i++)
        {
            var match = pattern.Match(lines[i]);
            if (match.Success)
            {
                yield return new Violation
                {
                    Rule = "RULE_A",
                    Severity = "ERROR",
                    File = dppPath,
                    Line = i + 1,
                    Message = $"Hardcoded version '{match.Groups[1].Value}'. Use $(VariableName) from Version.props."
                };
            }
        }
    }

    /// <summary>
    /// Rule B: Version.props import owners
    /// </summary>
    private static IEnumerable<Violation> CheckRuleB(string rootDir)
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

        var propsFiles = Directory.EnumerateFiles(rootDir, "*.props", SearchOption.AllDirectories);
        var importPattern = new Regex(@"<Import\s+[^>]*Project=""[^""]*Version\.props");

        foreach (var file in propsFiles)
        {
            var relativePath = Path.GetRelativePath(rootDir, file).Replace('\\', '/');
            var fileName = Path.GetFileName(file);

            if (allowedOwners.Contains(fileName))
                continue;

            var isAllowed = allowedPatterns.Any(p =>
                Regex.IsMatch(relativePath, p, RegexOptions.IgnoreCase));
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
                        Severity = "ERROR",
                        File = file,
                        Line = i + 1,
                        Message = "Version.props imported outside allowed owners. Only Directory.Packages.props, eng/Directory.Build.props, or src/Sdk/*/Sdk.props may import it."
                    };
                }
            }
        }
    }

    /// <summary>
    /// Rule C: Version.props symlink integrity
    /// </summary>
    private static IEnumerable<Violation> CheckRuleC(string rootDir)
    {
        var vpPath = Path.Combine(rootDir, "Version.props");

        if (!File.Exists(vpPath) && !IsSymlink(vpPath))
            yield break;

        // Source repo has src/common/Version.props
        var isSourceRepo = File.Exists(Path.Combine(rootDir, "src", "common", "Version.props"));
        if (isSourceRepo)
            yield break;

        if (!IsSymlink(vpPath))
        {
            yield return new Violation
            {
                Rule = "RULE_C",
                Severity = "ERROR",
                File = vpPath,
                Line = 0,
                Message = "Version.props should be a symlink in consumer repos, not a regular file."
            };
        }
        else if (!File.Exists(vpPath))
        {
            yield return new Violation
            {
                Rule = "RULE_C",
                Severity = "ERROR",
                File = vpPath,
                Line = 0,
                Message = "Version.props symlink is broken (target does not exist)."
            };
        }
    }

    /// <summary>
    /// Rule G: CPM enforcement (no inline versions)
    /// Skips projects that explicitly disable CPM with ManagePackageVersionsCentrally=false
    /// </summary>
    private static IEnumerable<Violation> CheckRuleG(string rootDir)
    {
        var csprojFiles = Directory.EnumerateFiles(rootDir, "*.csproj", SearchOption.AllDirectories);
        var pattern = new Regex(@"<PackageReference\s+[^>]*Version=""([^$][^""]*)""");
        var cpmDisabledPattern = new Regex(@"<ManagePackageVersionsCentrally\s*>\s*false\s*</ManagePackageVersionsCentrally>", RegexOptions.IgnoreCase);

        foreach (var file in csprojFiles)
        {
            var content = File.ReadAllText(file);

            // Skip projects that intentionally disable CPM
            if (cpmDisabledPattern.IsMatch(content))
                continue;

            var lines = content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("VersionOverride", StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = pattern.Match(lines[i]);
                if (match.Success)
                {
                    yield return new Violation
                    {
                        Rule = "RULE_G",
                        Severity = "ERROR",
                        File = file,
                        Line = i + 1,
                        Message = $"Inline version '{match.Groups[1].Value}' in PackageReference. Use Central Package Management."
                    };
                }
            }
        }
    }

    private static bool IsSymlink(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.LinkTarget != null;
        }
        catch
        {
            return false;
        }
    }
}

public class Violation
{
    public string Rule { get; set; } = "";
    public string Severity { get; set; } = "ERROR";
    public string File { get; set; } = "";
    public int Line { get; set; }
    public string Message { get; set; } = "";
}
