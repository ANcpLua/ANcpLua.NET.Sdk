using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Meziantou.Framework;
using NuGet.Packaging;

namespace ANcpLua.Sdk.Tests;

public sealed partial class TemplatesTests(PackageFixture fixture)
{
    private const string TemplatesPackageId = "ANcpLua.NET.Sdk.Templates";

    // The scaffold+build path shares the MSBuild build server and NuGet cache; run it serially to avoid flaky file races.
    private static readonly SemaphoreSlim s_scaffoldBuildGate = new(1, 1);

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
    public async Task Pack_WhenPackageType_IsTemplate()
    {
        using var reader = new PackageArchiveReader(TemplatesNupkgPath);
        var nuspec = await reader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);

        Assert.Contains(nuspec.GetPackageTypes(), static pt => pt.Name == "Template");
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public void Pack_WhenShortNameProvided_ContainsExpectedTemplateFiles(string shortName)
    {
        using var reader = new PackageArchiveReader(TemplatesNupkgPath);
        var files = reader.GetFiles().ToList();

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
    public async Task Pack_WhenTemplateJsonLoaded_HasNoUnsubstitutedPlaceholders(string shortName)
    {
        var content = await ReadEntryAsync($"content/{shortName}/.template.config/template.json");

        Assert.DoesNotContain("__PACK_TIME_SDK_VERSION__", content, StringComparison.Ordinal);
        Assert.DoesNotContain("__PACK_TIME_DOTNET_SDK_VERSION__", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Pack_WhenTemplateJsonLoaded_StampsSdkVersionToPackVersion(string shortName)
    {
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
    public async Task Pack_WhenTemplateJsonLoaded_StampsDotNetSdkVersionFromVersionProps(string shortName)
    {
        var expectedDotNetSdkVersion = await ReadDotNetSdkVersionFromVersionPropsAsync();

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
    public async Task Pack_WhenGlobalJsonLoaded_HasPlaceholdersForScaffoldTimeSubstitution(string shortName)
    {
        var content = await ReadEntryAsync($"content/{shortName}/global.json");

        Assert.Contains("ANCPLUA_SDK_VERSION_PLACEHOLDER", content, StringComparison.Ordinal);
        Assert.Contains("ANCPLUA_DOTNET_SDK_VERSION_PLACEHOLDER", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pack_WhenWebTemplateGlobalJsonLoaded_PinsAllThreeMsbuildSdks()
    {
        var content = await ReadEntryAsync("content/ancplua-web/global.json");

        Assert.Contains("\"ANcpLua.NET.Sdk\":", content, StringComparison.Ordinal);
        Assert.Contains("\"ANcpLua.NET.Sdk.Web\":", content, StringComparison.Ordinal);
        Assert.Contains("\"ANcpLua.NET.Sdk.Test\":", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ancplua-app", "MyApp")]
    [InlineData("ancplua-lib", "MyLib")]
    [InlineData("ancplua-web", "MyWeb")]
    public async Task Scaffold_WhenTemplateScaffolded_ProducesNamedTreeWithVersionPins(string shortName, string sourceName)
    {
        await using var hive = TemporaryDirectory.Create();
        await using var output = TemporaryDirectory.Create();
        var projectName = "T" + Guid.NewGuid().ToString("N")[..8];

        await DotnetNewInstallAsync(hive.FullPath);
        await DotnetNewScaffoldAsync(shortName, projectName, output.FullPath, hive.FullPath);

        Assert.True(File.Exists(output.FullPath / $"{projectName}.slnx"),
            $"Expected slnx at {projectName}.slnx (sourceName={sourceName}, output={output.FullPath})");
        Assert.True(File.Exists(output.FullPath / "src" / projectName / $"{projectName}.csproj"));
        Assert.True(File.Exists(output.FullPath / "tests" / $"{projectName}.Tests" / $"{projectName}.Tests.csproj"));

        var globalJsonPath = output.FullPath / "global.json";
        var globalJson = await File.ReadAllTextAsync(globalJsonPath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("ANCPLUA_SDK_VERSION_PLACEHOLDER", globalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("ANCPLUA_DOTNET_SDK_VERSION_PLACEHOLDER", globalJson, StringComparison.Ordinal);
        Assert.Contains($"\"ANcpLua.NET.Sdk\": \"{fixture.Version}\"", globalJson, StringComparison.Ordinal);

        var expectedDotNetSdkVersion = await ReadDotNetSdkVersionFromVersionPropsAsync();
        using var globalJsonDoc = JsonDocument.Parse(globalJson);
        var sdkVersion = globalJsonDoc.RootElement.GetProperty("sdk").GetProperty("version").GetString();
        Assert.Equal(expectedDotNetSdkVersion, sdkVersion);

        var dirPackagesPath = output.FullPath / "Directory.Packages.props";
        Assert.True(File.Exists(dirPackagesPath));
        var dirPackages = await File.ReadAllTextAsync(dirPackagesPath, TestContext.Current.CancellationToken);
        Assert.Contains("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>", dirPackages, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    public async Task Scaffold_WhenTargetFrameworkNet9Specified_HonorsChoice(string shortName)
    {
        await using var hive = TemporaryDirectory.Create();
        await using var output = TemporaryDirectory.Create();
        var projectName = "T" + Guid.NewGuid().ToString("N")[..8];

        await DotnetNewInstallAsync(hive.FullPath);
        await DotnetNewScaffoldAsync(
            shortName, projectName, output.FullPath, hive.FullPath, extraArgs: ["--TargetFramework", "net9.0"]);

        var srcCsproj = output.FullPath / "src" / projectName / $"{projectName}.csproj";
        var testsCsproj = output.FullPath / "tests" / $"{projectName}.Tests" / $"{projectName}.Tests.csproj";
        Assert.True(File.Exists(srcCsproj));
        Assert.True(File.Exists(testsCsproj));

        var srcContent = await File.ReadAllTextAsync(srcCsproj, TestContext.Current.CancellationToken);
        var testsContent = await File.ReadAllTextAsync(testsCsproj, TestContext.Current.CancellationToken);

        Assert.Contains("<TargetFramework>net9.0</TargetFramework>", srcContent, StringComparison.Ordinal);
        Assert.Contains("<TargetFramework>net9.0</TargetFramework>", testsContent, StringComparison.Ordinal);
        Assert.DoesNotContain("<TargetFramework>net10.0</TargetFramework>", srcContent, StringComparison.Ordinal);
        Assert.DoesNotContain("<TargetFramework>net10.0</TargetFramework>", testsContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ancplua-app")]
    [InlineData("ancplua-lib")]
    [InlineData("ancplua-web")]
    public async Task Scaffold_WhenScaffoldedProjectBuilt_BuildsCleanAgainstFixtureFeed(string shortName)
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

        var hivePath = Canonicalize(hive.FullPath);
        var outputPath = Canonicalize(output.FullPath);

        await RunDotnetAsync(["build-server", "shutdown"], outputPath);

        await DotnetNewInstallAsync(hivePath);
        await DotnetNewScaffoldAsync(shortName, projectName, outputPath, hivePath);

        var perTestGlobalPackages = outputPath / "nuget-cache";
        var fixturePackages = Canonicalize(fixture.PackageDirectory);
        await File.WriteAllTextAsync(
            outputPath / "nuget.config",
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <configuration>
               <config>
                 <add key="globalPackagesFolder" value="{perTestGlobalPackages}" />
               </config>
               <packageSources>
                 <clear/>
                 <add key="TestSource" value="{fixturePackages}" />
                 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
               </packageSources>
             </configuration>
             """,
            TestContext.Current.CancellationToken);

        var slnxPath = outputPath / $"{projectName}.slnx";

        // Split restore + build, serialised, to dodge macOS slnx-restore and Razor MvcApplicationPartsDiscovery races.
        var restore = await RunDotnetAsync(
            ["restore", slnxPath, "--disable-parallel", "-nodeReuse:false"], outputPath);
        Assert.True(restore.ExitCode == 0,
            $"Scaffolded {shortName} solution failed to restore (exit {restore.ExitCode}):{Environment.NewLine}{restore.Output}");

        var build = await RunDotnetAsync(
            ["build", slnxPath, "--no-restore", "--nologo", "-nodeReuse:false", "-maxcpucount:1"], outputPath);
        Assert.True(build.ExitCode == 0,
            $"Scaffolded {shortName} solution failed to build (exit {build.ExitCode}):{Environment.NewLine}{build.Output}");
    }

    private async Task DotnetNewInstallAsync(FullPath hive)
    {
        var result = await RunDotnetAsync(
            ["new", "install", TemplatesNupkgPath, "--debug:custom-hive", hive], workingDirectory: null);
        Assert.True(result.ExitCode == 0,
            $"dotnet new install failed (exit {result.ExitCode}):{Environment.NewLine}{result.Output}");
    }

    private static async Task DotnetNewScaffoldAsync(
        string shortName,
        string projectName,
        FullPath outputPath,
        FullPath hive,
        IReadOnlyList<string>? extraArgs = null)
    {
        var args = new List<object>
        {
            "new", shortName,
            "-n", projectName,
            "-o", outputPath,
            "--debug:custom-hive", hive,
            "--skipRestore"
        };
        if (extraArgs is not null)
            args.AddRange(extraArgs);

        var result = await RunDotnetAsync(args, workingDirectory: null);
        Assert.True(result.ExitCode == 0,
            $"dotnet new {shortName} failed (exit {result.ExitCode}):{Environment.NewLine}{result.Output}");
    }

    private async Task<string> ReadEntryAsync(string entryPath)
    {
        using var reader = new PackageArchiveReader(TemplatesNupkgPath);
        await using var stream = reader.GetStream(entryPath);
        using var sr = new StreamReader(stream);
        return await sr.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string> ReadDotNetSdkVersionFromVersionPropsAsync()
    {
        var versionPropsPath = RepositoryRoot.Locate()["src"] / "Build" / "Common" / "Version.props";
        var content = await File.ReadAllTextAsync(versionPropsPath, TestContext.Current.CancellationToken);
        var match = DotNetSdkVersionRegex().Match(content);
        Assert.True(match.Success, $"<DotNetSdkVersion> not found in {versionPropsPath}.");
        return match.Groups[1].Value.Trim();
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetAsync(
        IEnumerable<object> args,
        FullPath? workingDirectory)
    {
        var result = await ProcessWrapper.Create("dotnet")
            .WithWorkingDirectory(workingDirectory?.Value ?? Environment.CurrentDirectory)
            .WithArguments(args.Select(static arg => arg.ToString() ?? ""))
            .WithValidation(ProcessValidationMode.None)
            .ExecuteBufferedAsync(TestContext.Current.CancellationToken);

        return (result.ExitCode.Value, result.Output.ToString());
    }

    // macOS temp dirs live under /var → /private/var; resolve once so NuGet's slnx restore doesn't see one
    // physical file under two prefixes and fail with "file already exists". No-op on Linux/Windows.
    private static FullPath Canonicalize(FullPath path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return path;

        try
        {
            if (new DirectoryInfo(path.Value).ResolveLinkTarget(returnFinalTarget: true)?.FullName is { Length: > 0 } resolved)
                return FullPath.FromPath(resolved);

            var current = new DirectoryInfo(path.Value);
            var suffix = new Stack<string>();
            while (current?.Parent is not null)
            {
                if (current.ResolveLinkTarget(returnFinalTarget: true) is { } link)
                {
                    var rebuilt = link.FullName;
                    while (suffix.Count > 0)
                        rebuilt = Path.Combine(rebuilt, suffix.Pop());
                    return FullPath.FromPath(rebuilt);
                }

                suffix.Push(current.Name);
                current = current.Parent;
            }
        }
        catch (Exception e) when (e is DirectoryNotFoundException or UnauthorizedAccessException)
        {
            return path;
        }

        return path;
    }

    [GeneratedRegex(@"<DotNetSdkVersion>([^<]+)</DotNetSdkVersion>")]
    private static partial Regex DotNetSdkVersionRegex();
}
