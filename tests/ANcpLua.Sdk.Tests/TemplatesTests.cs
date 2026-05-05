using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Meziantou.Framework;
using NuGet.Packaging;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Tests for the ANcpLua.NET.Sdk.Templates package — verifies the pack pipeline
///     (Template package type, file layout, pack-time version stamping) and the
///     scaffolding pipeline (dotnet new install + scaffold + build of all 3 templates
///     against the local fixture feed).
/// </summary>
public sealed partial class TemplatesTests(PackageFixture fixture)
{
    private const string TemplatesPackageId = "ANcpLua.NET.Sdk.Templates";

    private static readonly string[] s_shortNames = ["ancplua-app", "ancplua-lib", "ancplua-web"];

    // Concurrent dotnet-build invocations against scaffolded projects step on each other
    // via the shared MSBuild build server (node reuse) and the shared NuGet global packages
    // folder — manifests as flaky "file is being used by another process" errors in
    // MvcApplicationPartsDiscovery for the Web template. Serialize the slow scaffold+build
    // path only; the fast pack-inspection tests stay parallel.
    private static readonly SemaphoreSlim s_scaffoldBuildGate = new(initialCount: 1, maxCount: 1);

    private string TemplatesNupkgPath
    {
        get
        {
            var matches = Directory.GetFiles(
                fixture.PackageDirectory,
                $"{TemplatesPackageId}.{fixture.Version}.nupkg",
                SearchOption.TopDirectoryOnly);
            matches.Should().ContainSingle(
                $"Expected exactly one {TemplatesPackageId}.{fixture.Version}.nupkg in " +
                $"{fixture.PackageDirectory}, found {matches.Length}.");
            return matches[0];
        }
    }

    [Fact]
    public async Task Pack_PackageType_IsTemplate()
    {
        using var reader = new PackageArchiveReader(TemplatesNupkgPath);
        var nuspec = await reader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        var packageTypes = nuspec.GetPackageTypes();

        packageTypes.Should().Contain(pt => pt.Name == "Template");
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public void Pack_ContainsExpectedTemplateFiles(string shortName)
    {
        using var reader = new PackageArchiveReader(TemplatesNupkgPath);
        var files = reader.GetFiles().ToList();

        // Every template ships these four scaffolding files
        foreach (var expected in new[]
                 {
                     $"content/{shortName}/.template.config/template.json",
                     $"content/{shortName}/global.json",
                     $"content/{shortName}/nuget.config",
                     $"content/{shortName}/Directory.Packages.props"
                 })
            files.Should().Contain(expected);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Pack_TemplateJson_HasNoUnsubstitutedPlaceholders(string shortName)
    {
        var content = await ReadEntryAsync($"content/{shortName}/.template.config/template.json");

        content.Should().NotContain("__PACK_TIME_SDK_VERSION__");
        content.Should().NotContain("__PACK_TIME_DOTNET_SDK_VERSION__");
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Pack_TemplateJson_StampsSdkVersionToPackVersion(string shortName)
    {
        // Pack-time substitution: __PACK_TIME_SDK_VERSION__ ← $(Version)
        var content = await ReadEntryAsync($"content/{shortName}/.template.config/template.json");
        using var doc = JsonDocument.Parse(content);

        var defaultValue = doc.RootElement
            .GetProperty("symbols")
            .GetProperty("SdkVersion")
            .GetProperty("defaultValue")
            .GetString();

        defaultValue.Should().Be(fixture.Version);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Pack_TemplateJson_StampsDotNetSdkVersionFromVersionProps(string shortName)
    {
        // Pack-time substitution: __PACK_TIME_DOTNET_SDK_VERSION__ ← $(DotNetSdkVersion) from Version.props
        var versionPropsPath = RepositoryRoot.Locate()["src"] / "Build" / "Common" / "Version.props";
        var versionPropsContent = await File.ReadAllTextAsync(versionPropsPath, TestContext.Current.CancellationToken);
        var match = DotNetSdkVersionRegex().Match(versionPropsContent);
        match.Success.Should().BeTrue($"<DotNetSdkVersion> not found in {versionPropsPath}.");
        var expectedDotNetSdkVersion = match.Groups[1].Value.Trim();

        var content = await ReadEntryAsync($"content/{shortName}/.template.config/template.json");
        using var doc = JsonDocument.Parse(content);

        var defaultValue = doc.RootElement
            .GetProperty("symbols")
            .GetProperty("DotNetSdkVersion")
            .GetProperty("defaultValue")
            .GetString();

        defaultValue.Should().Be(expectedDotNetSdkVersion);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Pack_GlobalJson_HasPlaceholders_ForScaffoldTimeSubstitution(string shortName)
    {
        // Unlike template.json (substituted at pack time), global.json keeps the
        // ANCPLUA_*_PLACEHOLDER tokens — those are substituted by the dotnet-new
        // template engine at scaffold time using the SdkVersion / DotNetSdkVersion
        // symbol values (which template.json's defaults provide).
        var content = await ReadEntryAsync($"content/{shortName}/global.json");

        content.Should().Contain("ANCPLUA_SDK_VERSION_PLACEHOLDER");
        content.Should().Contain("ANCPLUA_DOTNET_SDK_VERSION_PLACEHOLDER");
    }

    [Fact]
    public async Task Pack_WebTemplate_GlobalJsonPinsAllThreeMsbuildSdks()
    {
        // ancplua-web consumers need ANcpLua.NET.Sdk.Web *and* the regular SDK pinned
        // (the Web SDK depends on Build/Common/* shared with the regular SDK), plus
        // ANcpLua.NET.Sdk.Test for the tests project.
        var content = await ReadEntryAsync("content/ancplua-web/global.json");

        content.Should().Contain("\"ANcpLua.NET.Sdk\":");
        content.Should().Contain("\"ANcpLua.NET.Sdk.Web\":");
        content.Should().Contain("\"ANcpLua.NET.Sdk.Test\":");
    }

    [Theory]
    [InlineData("ancplua-app", "MyApp")]
    [InlineData("ancplua-lib", "MyLib")]
    [InlineData("ancplua-web", "MyWeb")]
    public async Task Scaffold_ProducesNamedTreeWithVersionPins(string shortName, string sourceName)
    {
        // Hermetic install via --debug:custom-hive to avoid polluting the user's
        // global dotnet-new state on the test runner.
        await using var hive = TemporaryDirectory.Create();
        await using var output = TemporaryDirectory.Create();
        var projectName = "T" + Guid.NewGuid().ToString("N")[..8];

        await DotnetNewInstallAsync(hive.FullPath);
        await DotnetNewScaffoldAsync(shortName, projectName, output.FullPath, hive.FullPath);

        // sourceName=MyApp/MyLib/MyWeb is replaced with $projectName everywhere by
        // the template engine (filenames, directory names, namespaces, references).
        File.Exists(output.FullPath / $"{projectName}.slnx").Should().BeTrue(
            $"Expected slnx at {projectName}.slnx (sourceName={sourceName}, output={output.FullPath})");
        File.Exists(output.FullPath / "src" / projectName / $"{projectName}.csproj").Should().BeTrue();
        File.Exists(output.FullPath / "tests" / $"{projectName}.Tests" / $"{projectName}.Tests.csproj").Should().BeTrue();

        // Scaffolded global.json should pin SDKs to fixture.Version (the template engine
        // substituted ANCPLUA_SDK_VERSION_PLACEHOLDER with the SdkVersion default we
        // stamped at pack time).
        var globalJsonPath = output.FullPath / "global.json";
        var globalJson = await File.ReadAllTextAsync(globalJsonPath, TestContext.Current.CancellationToken);
        globalJson.Should().NotContain("ANCPLUA_SDK_VERSION_PLACEHOLDER");
        globalJson.Should().NotContain("ANCPLUA_DOTNET_SDK_VERSION_PLACEHOLDER");
        globalJson.Should().Contain($"\"ANcpLua.NET.Sdk\": \"{fixture.Version}\"");

        // Scaffolded Directory.Packages.props is required by the SDK contract — verify
        // it's present and CPM-enabled.
        var dirPackagesPath = output.FullPath / "Directory.Packages.props";
        File.Exists(dirPackagesPath).Should().BeTrue();
        var dirPackages = await File.ReadAllTextAsync(dirPackagesPath, TestContext.Current.CancellationToken);
        dirPackages.Should().Contain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Scaffold_BuildsCleanAgainstFixtureFeed(string shortName)
    {
        await s_scaffoldBuildGate.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            await ScaffoldAndBuildAsync(shortName);
        }
        finally
        {
            s_scaffoldBuildGate.Release();
        }
    }

    private async Task ScaffoldAndBuildAsync(string shortName)
    {
        await using var hive = TemporaryDirectory.Create();
        await using var output = TemporaryDirectory.Create();
        var projectName = "T" + Guid.NewGuid().ToString("N")[..8];

        // macOS: TemporaryDirectory.Create() returns paths under /var/folders/... which
        // is a symlink to /private/var/folders/... NuGet, MSBuild, and other dotnet
        // tooling sometimes resolve through the symlink and sometimes don't, producing
        // "obj/X.csproj.nuget.g.props already exists" during slnx restore (the same
        // physical file is referenced via both prefixes within one restore pass).
        // Canonicalize once up front so every subsequent path is the resolved form.
        var hivePath = Canonicalize(hive.FullPath);
        var outputPath = Canonicalize(output.FullPath);

        // Shut down any inherited MSBuild / Razor / VBCSCompiler build servers from the
        // surrounding test process so this scaffold's restore+build doesn't reuse state.
        await RunDotnetAsync(["build-server", "shutdown"], outputPath);

        await DotnetNewInstallAsync(hivePath);
        await DotnetNewScaffoldAsync(shortName, projectName, outputPath, hivePath);

        // The scaffolded nuget.config restricts to nuget.org — but the SDK packages at
        // fixture.Version (e.g. 999.9.9) only exist in the local fixture feed. Replace
        // it with a dual-source config so SDK packages resolve from local and analyzer
        // / test packages resolve from nuget.org. Use a per-test globalPackagesFolder
        // (under output, deleted with the temp dir) so concurrent SDK tests against the
        // shared fixture's packages/ folder don't race on obj/*.nuget.g.props.
        var perTestGlobalPackages = outputPath / "nuget-cache";
        var fixturePackages = Canonicalize(fixture.PackageDirectory);

        // Generate nuget.config using XElement to ensure proper XML escaping of paths
        var nugetConfig = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("configuration",
                new XElement("config",
                    new XElement("add",
                        new XAttribute("key", "globalPackagesFolder"),
                        new XAttribute("value", perTestGlobalPackages.Value))),
                new XElement("packageSources",
                    new XElement("clear"),
                    new XElement("add",
                        new XAttribute("key", "TestSource"),
                        new XAttribute("value", fixturePackages.Value)),
                    new XElement("add",
                        new XAttribute("key", "nuget.org"),
                        new XAttribute("value", "https://api.nuget.org/v3/index.json")))));

        await File.WriteAllTextAsync(
            outputPath / "nuget.config",
            nugetConfig.ToString(),
            TestContext.Current.CancellationToken);

        var slnxPath = outputPath / $"{projectName}.slnx";

        // Split restore + build. Two macOS-specific contributors to flakiness:
        //
        //   1. Parallel slnx-level restore: NuGet restores src/ + tests/ concurrently
        //      and writes obj/<proj>.csproj.nuget.g.props for each. On macOS the
        //      /var ⟷ /private/var symlink ambiguity occasionally trips NuGet's
        //      "file already exists" guard within a single restore pass.
        //   2. Razor SDK's MvcApplicationPartsDiscovery target races with the
        //      implicit build-time restore — a deterministic --no-restore build
        //      after explicit restore avoids that.
        //
        // --disable-parallel serializes project restore inside the slnx; nodeReuse:false
        // prevents reusing an MSBuild server from a parallel sibling test.
        var restore = await RunDotnetAsync(
            ["restore", slnxPath, "--disable-parallel", "-nodeReuse:false"],
            outputPath);
        restore.ExitCode.Should().Be(0,
            $"Scaffolded {shortName} solution failed to restore (exit {restore.ExitCode}):{Environment.NewLine}{restore.Output}");

        var build = await RunDotnetAsync(
            ["build", slnxPath, "--no-restore", "--nologo", "-nodeReuse:false", "-maxcpucount:1"],
            outputPath);
        build.ExitCode.Should().Be(0,
            $"Scaffolded {shortName} solution failed to build (exit {build.ExitCode}):{Environment.NewLine}{build.Output}");
    }

    private async Task DotnetNewInstallAsync(FullPath hive)
    {
        var result = await RunDotnetAsync(
            ["new", "install", TemplatesNupkgPath, "--debug:custom-hive", hive],
            workingDirectory: null);
        result.ExitCode.Should().Be(0,
            $"dotnet new install failed (exit {result.ExitCode}):{Environment.NewLine}{result.Output}");
    }

    private static async Task DotnetNewScaffoldAsync(
        string shortName,
        string projectName,
        FullPath outputPath,
        FullPath hive)
    {
        var result = await RunDotnetAsync(
            [
                "new", shortName,
                "-n", projectName,
                "-o", outputPath,
                "--debug:custom-hive", hive,
                "--skipRestore"
            ],
            workingDirectory: null);
        result.ExitCode.Should().Be(0,
            $"dotnet new {shortName} failed (exit {result.ExitCode}):{Environment.NewLine}{result.Output}");
    }

    private async Task<string> ReadEntryAsync(string entryPath)
    {
        using var reader = new PackageArchiveReader(TemplatesNupkgPath);
        await using var stream = reader.GetStream(entryPath);
        using var sr = new StreamReader(stream);
        return await sr.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetAsync(
        IEnumerable<object> args,
        FullPath? workingDirectory)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory?.Value ?? Environment.CurrentDirectory
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg.ToString() ?? "");

        var result = await psi.RunAsTaskAsync(TestContext.Current.CancellationToken);
        return (result.ExitCode, result.Output.ToString());
    }

    /// <summary>
    ///     Resolves macOS symlink prefixes (/var → /private/var, /tmp → /private/tmp) so the
    ///     same physical directory has exactly one canonical string representation. Without
    ///     this, NuGet's slnx-level restore on macOS sometimes writes obj/X.csproj.nuget.g.props
    ///     under one prefix and re-checks under the other, producing a spurious "file already
    ///     exists" error on the second project in the solution.
    /// </summary>
    private static FullPath Canonicalize(FullPath path)
    {
        var resolved = new DirectoryInfo(path.Value).ResolveLinkTarget(returnFinalTarget: true)?.FullName;
        if (!string.IsNullOrEmpty(resolved)) return FullPath.FromPath(resolved);

        // No direct symlink on the leaf; walk up to find an ancestor that is one
        // (e.g. /var → /private/var) and rebuild the path through the resolved ancestor.
        var current = new DirectoryInfo(path.Value);
        var suffix = new Stack<string>();
        while (current is not null)
        {
            var link = current.ResolveLinkTarget(returnFinalTarget: true);
            if (link is not null)
            {
                var rebuilt = link.FullName;
                while (suffix.Count > 0) rebuilt = Path.Combine(rebuilt, suffix.Pop());
                return FullPath.FromPath(rebuilt);
            }

            suffix.Push(current.Name);
            current = current.Parent;
        }

        return path;
    }

    [GeneratedRegex(@"<DotNetSdkVersion>([^<]+)</DotNetSdkVersion>")]
    private static partial Regex DotNetSdkVersionRegex();
}
