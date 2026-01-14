using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using ANcpLua.Roslyn.Utilities.Testing.MSBuild;
using ANcpLua.Sdk.Tests.Infrastructure;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Helpers;

/// <summary>
/// SDK import styles for testing different SDK reference patterns.
/// </summary>
public enum SdkImportStyle
{
    Default,
    ProjectElement,
    SdkElement,
    SdkElementDirectoryBuildProps
}

/// <summary>
/// SDK-specific project builder that extends the base ProjectBuilder with
/// PackageFixture support, SDK import styles, and xunit.v3 TestContext integration.
/// </summary>
public sealed class SdkProjectBuilder : ProjectBuilder
{
    private readonly SdkImportStyle _defaultSdkImportStyle;
    private readonly string _defaultSdkName;
    private readonly PackageFixture _fixture;
    private SdkImportStyle? _importStyleOverride;

    public SdkProjectBuilder(PackageFixture fixture, ITestOutputHelper testOutputHelper,
        SdkImportStyle defaultSdkImportStyle, string defaultSdkName)
        : base(testOutputHelper)
    {
        _fixture = fixture;
        _defaultSdkImportStyle = defaultSdkImportStyle;
        _defaultSdkName = defaultSdkName;

        // Configure NuGet.config for SDK package testing
        Directory.CreateTextFile("NuGet.config", $"""
            <configuration>
                <config>
                    <add key="globalPackagesFolder" value="{_fixture.PackageDirectory}/packages" />
                </config>
                <packageSources>
                    <clear />
                    <add key="TestSource" value="{_fixture.PackageDirectory}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
                <packageSourceMapping>
                    <packageSource key="TestSource">
                        <package pattern="ANcpLua.NET.Sdk*" />
                        <package pattern="ANcpSdk.*" />
                    </packageSource>
                    <packageSource key="nuget.org">
                        <package pattern="*" />
                    </packageSource>
                </packageSourceMapping>
            </configuration>
            """);

        if (defaultSdkImportStyle is SdkImportStyle.SdkElementDirectoryBuildProps)
            AddDirectoryBuildPropsFile(string.Empty);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        TestContext.Current.AddAttachment("GITHUB_STEP_SUMMARY",
            XmlSanitizer.SanitizeForXml(GetGitHubStepSummaryContent()));
        await base.DisposeAsync();
    }

    /// <summary>
    /// Sets the .NET SDK version to use.
    /// </summary>
    public void SetDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        SdkVersion = dotnetSdkVersion;
    }

    /// <summary>
    /// Enables Microsoft.Testing.Platform mode for test projects.
    /// </summary>
    public void EnableMtpMode()
    {
        Directory.CreateTextFile("global.json", """
            {
              "sdk": {
                "rollForward": "latestMinor",
                "version": "10.0.100"
              },
              "test": {
                "runner": "Microsoft.Testing.Platform"
              }
            }
            """);
    }

    /// <summary>
    /// Overrides the SDK import style.
    /// </summary>
    public SdkProjectBuilder WithImportStyle(SdkImportStyle style)
    {
        _importStyleOverride = style;
        return this;
    }

    /// <summary>
    /// Adds a Directory.Build.props file with SDK element injection.
    /// </summary>
    public void AddDirectoryBuildPropsFile(string postSdkContent, string preSdkContent = "", string? sdkName = null)
    {
        var sdk = _defaultSdkImportStyle == SdkImportStyle.SdkElementDirectoryBuildProps
            ? GetSdkElementContent(sdkName ?? _defaultSdkName)
            : string.Empty;

        var fileContent = $"""
            <Project>
                {preSdkContent}
                {sdk}
                {postSdkContent}
            </Project>
            """;
        var fullPath = Directory.FullPath / RepositoryPaths.DirectoryBuildProps;
        fullPath.CreateParentDirectory();
        File.WriteAllText(fullPath, fileContent);
    }

    /// <summary>
    /// Adds a csproj file with SDK import style handling.
    /// </summary>
    public SdkProjectBuilder AddCsprojFile((string Name, string Value)[]? properties = null,
        NuGetReference[]? nuGetPackages = null, XElement[]? additionalProjectElements = null, string? sdk = null,
        string? rootSdk = null, string filename = "ANcpLua.TestProject.csproj",
        SdkImportStyle importStyle = SdkImportStyle.Default)
    {
        sdk ??= _defaultSdkName;
        var propertiesElement = new XElement("PropertyGroup");
        if (properties is not null)
            foreach (var prop in properties)
                propertiesElement.Add(new XElement(prop.Name, prop.Value));

        var packagesElement = new XElement("ItemGroup");
        if (nuGetPackages is not null)
            foreach (var package in nuGetPackages)
                packagesElement.Add(new XElement("PackageReference", new XAttribute("Include", package.Name),
                    new XAttribute("Version", package.Version)));

        importStyle = importStyle == SdkImportStyle.Default ? _defaultSdkImportStyle : importStyle;
        var rootSdkName = importStyle == SdkImportStyle.ProjectElement
            ? $"{sdk}/{_fixture.Version}"
            : rootSdk ?? "Microsoft.NET.Sdk";
        var innerSdkXmlElement = importStyle == SdkImportStyle.SdkElement ? GetSdkElementContent(sdk) : string.Empty;

        var content = $"""
            <Project Sdk="{rootSdkName}">
                {innerSdkXmlElement}
                <PropertyGroup>
                    <OutputType>exe</OutputType>
                    <ErrorLog>BuildOutput.sarif,version=2.1</ErrorLog>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                    <ANcpLuaSdkSkipCPMEnforcement>true</ANcpLuaSdkSkipCPMEnforcement>
                </PropertyGroup>
                {propertiesElement}
                {packagesElement}
                {string.Join('\n', additionalProjectElements?.Select(e => e.ToString()) ?? [])}
            </Project>
            """;

        var fullPath = Directory.FullPath / filename;
        fullPath.CreateParentDirectory();
        File.WriteAllText(fullPath, content);
        return this;
    }

    /// <summary>
    /// Builds and returns the output using SDK-specific handling.
    /// </summary>
    public Task<BuildResult> BuildAndGetOutput(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        return ExecuteDotnetCommandAndGetOutput("build", buildArguments, environmentVariables);
    }

    public Task<BuildResult> RestoreAndGetOutput(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        return ExecuteDotnetCommandAndGetOutput("restore", buildArguments, environmentVariables);
    }

    public Task<BuildResult> CleanAndGetOutput(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        return ExecuteDotnetCommandAndGetOutput("clean", buildArguments, environmentVariables);
    }

    public Task<BuildResult> PackAndGetOutput(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        return ExecuteDotnetCommandAndGetOutput("pack", buildArguments, environmentVariables);
    }

    public Task<BuildResult> RunAndGetOutput(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        return ExecuteDotnetCommandAndGetOutput("run", ["--", .. buildArguments ?? []], environmentVariables);
    }

    public Task<BuildResult> TestAndGetOutput(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        return ExecuteDotnetCommandAndGetOutput("test", buildArguments, environmentVariables);
    }

    /// <inheritdoc />
    protected override void GenerateCsprojFile()
    {
        // Use SDK-specific csproj generation
        AddCsprojFile(
            [.. Properties],
            [.. NuGetPackages],
            filename: ProjectFilename ?? "ANcpLua.TestProject.csproj",
            importStyle: _importStyleOverride ?? SdkImportStyle.Default);
    }

    /// <summary>
    /// Executes a dotnet command with SDK-specific handling including retry logic and TestContext attachments.
    /// </summary>
    public async Task<BuildResult> ExecuteDotnetCommandAndGetOutput(string command, string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        BuildCount++;

        foreach (var file in System.IO.Directory.GetFiles(Directory.FullPath, "*", SearchOption.AllDirectories))
        {
            TestOutputHelper?.WriteLine("File: " + file);
            var content = await File.ReadAllTextAsync(file);
            TestOutputHelper?.WriteLine(XmlSanitizer.SanitizeForXml(content));
        }

        TestOutputHelper?.WriteLine("-------- dotnet " + command);
        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(SdkVersion))
        {
            WorkingDirectory = Directory.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add(command);
        if (buildArguments is not null)
            foreach (var arg in buildArguments)
                psi.ArgumentList.Add(arg);

        psi.ArgumentList.Add("/bl");

        // Remove parent environment variables that can interfere with SDK behavior
        psi.Environment.Remove("CI");
        psi.Environment.Remove("DOTNET_ENVIRONMENT");
        foreach (var kvp in psi.Environment.ToArray())
            if (kvp.Key.StartsWith("GITHUB", StringComparison.Ordinal) ||
                kvp.Key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.StartsWith("GITHUB_", StringComparison.Ordinal) ||
                kvp.Key.StartsWith("RUNNER_", StringComparison.Ordinal))
                psi.Environment.Remove(kvp.Key);

        psi.Environment["MSBUILDLOGALLENVIRONMENTVARIABLES"] = "true";
        var vstestdiagPath = RootFolder / "vstestdiag.txt";
        psi.Environment["VSTestDiag"] = vstestdiagPath;
        psi.Environment["DOTNET_ROOT"] = Path.GetDirectoryName(psi.FileName);
        psi.Environment["DOTNET_ROOT_X64"] = Path.GetDirectoryName(psi.FileName);
        psi.Environment["DOTNET_HOST_PATH"] = psi.FileName;
        psi.Environment["NUGET_HTTP_CACHE_PATH"] = _fixture.PackageDirectory / "http-cache";
        psi.Environment["NUGET_PACKAGES"] = _fixture.PackageDirectory / "packages";
        psi.Environment["NUGET_SCRATCH"] = _fixture.PackageDirectory / "nuget-scratch";
        psi.Environment["NUGET_PLUGINS_CACHE_PATH"] = _fixture.PackageDirectory / "nuget-plugins-cache";

        if (environmentVariables is not null)
            foreach (var env in environmentVariables)
                psi.Environment[env.Name] = env.Value;

        TestContext.Current.TestOutputHelper?.WriteLine("Executing: " + psi.FileName + " " +
                                                        string.Join(' ', psi.ArgumentList));
        foreach (var env in psi.Environment.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            TestContext.Current.TestOutputHelper?.WriteLine($"  {env.Key}={env.Value}");

        var result = await psi.RunAsTaskAsync();

        // Retry up to 5 times if MSB4236 error occurs (SDK resolution issue)
        const int maxRetries = 5;
        for (var retry = 0; retry < maxRetries && result.ExitCode is not 0; retry++)
            if (result.Output.Any(static line => line.Text.Contains("error MSB4236", StringComparison.Ordinal) ||
                                                 line.Text.Contains(
                                                     "The project file may be invalid or missing targets required for restore",
                                                     StringComparison.Ordinal)))
            {
                TestOutputHelper?.WriteLine(
                    $"SDK resolution or restore error detected, retrying ({retry + 1}/{maxRetries})...");

                // Exponential backoff: 100ms, 200ms, 400ms, 800ms, 1600ms
                await Task.Delay(100 * (1 << retry));

                result = await psi.RunAsTaskAsync();
            }
            else
            {
                break;
            }

        TestOutputHelper?.WriteLine("Process exit code: " + result.ExitCode);
        TestOutputHelper?.WriteLine(XmlSanitizer.SanitizeForXml(result.Output.ToString()));

        var sarifPath = Directory.FullPath / SarifFileName;
        SarifFile? sarif = null;
        if (File.Exists(sarifPath))
        {
            var bytes = await File.ReadAllBytesAsync(sarifPath);
            sarif = JsonSerializer.Deserialize<SarifFile>(bytes);
            TestOutputHelper?.WriteLine("Sarif result:\n" +
                                        XmlSanitizer.SanitizeForXml(string.Join("\n",
                                            sarif!.AllResults().Select(static r => r.ToString()))));
        }
        else
        {
            TestOutputHelper?.WriteLine("Sarif file not found: " + sarifPath);
        }

        var binlogContent = await File.ReadAllBytesAsync(Directory.FullPath / "msbuild.binlog");
        TestContext.Current.AddAttachment($"msbuild{BuildCount}.binlog", binlogContent);

        if (File.Exists(vstestdiagPath))
        {
            var vstestDiagContent = await File.ReadAllTextAsync(vstestdiagPath);
            TestContext.Current.AddAttachment(vstestdiagPath.Name, XmlSanitizer.SanitizeForXml(vstestDiagContent));
        }

        if (result.Output.Any(static line => line.Text.Contains("Could not resolve SDK")))
            Assert.Fail("The SDK cannot be found, expected version: " + _fixture.Version);

        return new BuildResult(result.ExitCode, result.Output, sarif, binlogContent);
    }

    /// <summary>
    /// Executes a git command in the project directory.
    /// </summary>
    public Task ExecuteGitCommand(params string[]? arguments)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = Directory.FullPath,
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        ICollection<KeyValuePair<string, string>> gitParameters =
        [
            KeyValuePair.Create("user.name", "sample"),
            KeyValuePair.Create("user.username", "sample"),
            KeyValuePair.Create("user.email", "sample@example.com"),
            KeyValuePair.Create("commit.gpgsign", "false"),
            KeyValuePair.Create("pull.rebase", "true"),
            KeyValuePair.Create("fetch.prune", "true"),
            KeyValuePair.Create("core.autocrlf", "false"),
            KeyValuePair.Create("core.longpaths", "true"),
            KeyValuePair.Create("rebase.autoStash", "true"),
            KeyValuePair.Create("submodule.recurse", "false"),
            KeyValuePair.Create("init.defaultBranch", "main")
        ];

        foreach (var param in gitParameters)
        {
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add($"{param.Key}={param.Value}");
        }

        if (arguments is not null)
            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

        return psi.RunAsTaskAsync();
    }

    private string GetSdkElementContent(string sdkName)
    {
        return $"""<Sdk Name="{sdkName}" Version="{_fixture.Version}" />""";
    }
}

/// <summary>
/// Utility to sanitize strings for XML 1.0 compatibility.
/// </summary>
internal static class XmlSanitizer
{
    /// <summary>
    /// Removes characters that are invalid in XML 1.0 documents.
    /// </summary>
    public static string SanitizeForXml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        var hasInvalidChars = text.Any(static ch => !IsValidXmlChar(ch));

        return !hasInvalidChars ? text : new string(text.Where(IsValidXmlChar).ToArray());
    }

    private static bool IsValidXmlChar(char ch)
    {
        return ch == 0x9 ||
               ch == 0xA ||
               ch == 0xD ||
               (ch >= 0x20 && ch <= 0xD7FF) ||
               (ch >= 0xE000 && ch <= 0xFFFD);
    }
}
