using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            if (matches.Length != 1)
                Assert.Fail(
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

        Assert.Contains(packageTypes, static pt => pt.Name == "Template");
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
            Assert.Contains(expected, files);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Pack_TemplateJson_HasNoUnsubstitutedPlaceholders(string shortName)
    {
        var content = await ReadEntryAsync($"content/{shortName}/.template.config/template.json");

        Assert.DoesNotContain("__PACK_TIME_SDK_VERSION__", content);
        Assert.DoesNotContain("__PACK_TIME_DOTNET_SDK_VERSION__", content);
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

        Assert.Equal(fixture.Version, defaultValue);
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
        Assert.True(match.Success, $"<DotNetSdkVersion> not found in {versionPropsPath}.");
        var expectedDotNetSdkVersion = match.Groups[1].Value.Trim();

        var content = await ReadEntryAsync($"content/{shortName}/.template.config/template.json");
        using var doc = JsonDocument.Parse(content);

        var defaultValue = doc.RootElement
            .GetProperty("symbols")
            .GetProperty("DotNetSdkVersion")
            .GetProperty("defaultValue")
            .GetString();

        Assert.Equal(expectedDotNetSdkVersion, defaultValue);
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

        Assert.Contains("ANCPLUA_SDK_VERSION_PLACEHOLDER", content);
        Assert.Contains("ANCPLUA_DOTNET_SDK_VERSION_PLACEHOLDER", content);
    }

    [Fact]
    public async Task Pack_WebTemplate_GlobalJsonPinsAllThreeMsbuildSdks()
    {
        // ancplua-web consumers need ANcpLua.NET.Sdk.Web *and* the regular SDK pinned
        // (the Web SDK depends on Build/Common/* shared with the regular SDK), plus
        // ANcpLua.NET.Sdk.Test for the tests project.
        var content = await ReadEntryAsync("content/ancplua-web/global.json");

        Assert.Contains("\"ANcpLua.NET.Sdk\":", content);
        Assert.Contains("\"ANcpLua.NET.Sdk.Web\":", content);
        Assert.Contains("\"ANcpLua.NET.Sdk.Test\":", content);
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
        Assert.True(File.Exists(output.FullPath / $"{projectName}.slnx"),
            $"Expected slnx at {projectName}.slnx (sourceName={sourceName}, output={output.FullPath})");
        Assert.True(File.Exists(output.FullPath / "src" / projectName / $"{projectName}.csproj"));
        Assert.True(File.Exists(output.FullPath / "tests" / $"{projectName}.Tests" / $"{projectName}.Tests.csproj"));

        // Scaffolded global.json should pin SDKs to fixture.Version (the template engine
        // substituted ANCPLUA_SDK_VERSION_PLACEHOLDER with the SdkVersion default we
        // stamped at pack time).
        var globalJsonPath = output.FullPath / "global.json";
        var globalJson = await File.ReadAllTextAsync(globalJsonPath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("ANCPLUA_SDK_VERSION_PLACEHOLDER", globalJson);
        Assert.DoesNotContain("ANCPLUA_DOTNET_SDK_VERSION_PLACEHOLDER", globalJson);
        Assert.Contains($"\"ANcpLua.NET.Sdk\": \"{fixture.Version}\"", globalJson);

        // Scaffolded Directory.Packages.props is required by the SDK contract — verify
        // it's present and CPM-enabled.
        var dirPackagesPath = output.FullPath / "Directory.Packages.props";
        Assert.True(File.Exists(dirPackagesPath));
        var dirPackages = await File.ReadAllTextAsync(dirPackagesPath, TestContext.Current.CancellationToken);
        Assert.Contains("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>", dirPackages);
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

        await DotnetNewInstallAsync(hive.FullPath);
        await DotnetNewScaffoldAsync(shortName, projectName, output.FullPath, hive.FullPath);

        // The scaffolded nuget.config restricts to nuget.org — but the SDK packages at
        // fixture.Version (e.g. 999.9.9) only exist in the local fixture feed. Replace
        // it with a dual-source config so SDK packages resolve from local and analyzer
        // / test packages resolve from nuget.org. Use a per-test globalPackagesFolder
        // (under output, deleted with the temp dir) so concurrent SDK tests against the
        // shared fixture's packages/ folder don't race on obj/*.nuget.g.props.
        var perTestGlobalPackages = output.FullPath / "nuget-cache";
        await File.WriteAllTextAsync(
            output.FullPath / "nuget.config",
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <configuration>
               <config>
                 <add key="globalPackagesFolder" value="{perTestGlobalPackages}" />
               </config>
               <packageSources>
                 <clear/>
                 <add key="TestSource" value="{fixture.PackageDirectory}" />
                 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
               </packageSources>
             </configuration>
             """,
            TestContext.Current.CancellationToken);

        var slnxPath = output.FullPath / $"{projectName}.slnx";

        // Split restore + build. dotnet build's implicit restore can race with Razor's
        // own restore inside the build phase, producing "obj/*.nuget.g.props already
        // exists" errors for the Web template. Explicit restore before --no-restore
        // build is deterministic. -nodeReuse:false avoids inheriting an MSBuild server
        // started by another concurrent test in the suite.
        var restore = await RunDotnetAsync(
            ["restore", slnxPath, "-nodeReuse:false"],
            output.FullPath);
        Assert.True(
            restore.ExitCode == 0,
            $"Scaffolded {shortName} solution failed to restore (exit {restore.ExitCode}):{Environment.NewLine}{restore.Output}");

        var build = await RunDotnetAsync(
            ["build", slnxPath, "--no-restore", "--nologo", "-nodeReuse:false", "-maxcpucount:1"],
            output.FullPath);
        Assert.True(
            build.ExitCode == 0,
            $"Scaffolded {shortName} solution failed to build (exit {build.ExitCode}):{Environment.NewLine}{build.Output}");
    }

    private async Task DotnetNewInstallAsync(FullPath hive)
    {
        var result = await RunDotnetAsync(
            ["new", "install", TemplatesNupkgPath, "--debug:custom-hive", hive],
            workingDirectory: null);
        Assert.True(
            result.ExitCode == 0,
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
        Assert.True(
            result.ExitCode == 0,
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

    [GeneratedRegex(@"<DotNetSdkVersion>([^<]+)</DotNetSdkVersion>")]
    private static partial Regex DotNetSdkVersionRegex();
}
