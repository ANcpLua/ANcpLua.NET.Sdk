using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using ANcpLua.Sdk.Tests.Infrastructure;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Helpers;

public enum SdkImportStyle
{
    Default,
    ProjectElement,
    SdkElement,
    SdkElementDirectoryBuildProps
}

public sealed class ProjectBuilder : IAsyncDisposable
{
    private const string SarifFileName = "BuildOutput.sarif";
    private readonly SdkImportStyle _defaultSdkImportStyle;
    private readonly string _defaultSdkName;

    private readonly TemporaryDirectory _directory;
    private readonly PackageFixture _fixture;
    private readonly FullPath _githubStepSummaryFile;
    private readonly List<NuGetReference> _nugetPackages = [];

    // Fluent API state
    private readonly List<(string Key, string Value)> _properties = [];
    private readonly List<(string Name, string Content)> _sourceFiles = [];
    private readonly ITestOutputHelper _testOutputHelper;
    private int _buildCount;
    private SdkImportStyle? _importStyleOverride;
    private string? _projectFilename = "ANcpLua.TestProject.csproj";
    private NetSdkVersion _sdkVersion = NetSdkVersion.Net100;

    public ProjectBuilder(PackageFixture fixture, ITestOutputHelper testOutputHelper,
        SdkImportStyle defaultSdkImportStyle, string defaultSdkName)
    {
        _fixture = fixture;
        _testOutputHelper = testOutputHelper;
        _defaultSdkImportStyle = defaultSdkImportStyle;
        _defaultSdkName = defaultSdkName;
        _directory = TemporaryDirectory.Create();
        _directory.CreateTextFile("NuGet.config", $"""
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

        // Create isolated global.json to prevent inheriting test.runner from parent directory
        // This ensures temp projects don't get forced into MTP mode
        _directory.CreateTextFile("global.json", """
                                                 {
                                                   "sdk": {
                                                     "rollForward": "latestMinor",
                                                     "version": "10.0.100"
                                                   }
                                                 }
                                                 """);

        if (defaultSdkImportStyle is SdkImportStyle.SdkElementDirectoryBuildProps)
            AddDirectoryBuildPropsFile(string.Empty);

        _githubStepSummaryFile = _directory.CreateEmptyFile("GITHUB_STEP_SUMMARY.txt");
    }

    public FullPath RootFolder => _directory.FullPath;

    public IEnumerable<(string Name, string Value)> GitHubEnvironmentVariables
    {
        get
        {
            yield return ("GITHUB_ACTIONS", "true");
            yield return ("GITHUB_STEP_SUMMARY", _githubStepSummaryFile);
        }
    }

    public async ValueTask DisposeAsync()
    {
        TestContext.Current.AddAttachment("GITHUB_STEP_SUMMARY",
            XmlSanitizer.SanitizeForXml(GetGitHubStepSummaryContent()));
        await _directory.DisposeAsync();
    }

    public string? GetGitHubStepSummaryContent()
    {
        return File.Exists(_githubStepSummaryFile) ? File.ReadAllText(_githubStepSummaryFile) : null;
    }

    public FullPath AddFile(string relativePath, string content)
    {
        var path = _directory.FullPath / relativePath;
        path.CreateParentDirectory();
        File.WriteAllText(path, content);
        return path;
    }

    public void SetDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        _sdkVersion = dotnetSdkVersion;
    }

    /// <summary>
    ///     Enables Microsoft.Testing.Platform mode for test projects that use MTP packages.
    ///     Required for .NET 10+ when using xunit.v3.mtp-*, TUnit, or other MTP-based frameworks.
    /// </summary>
    public void EnableMtpMode()
    {
        _directory.CreateTextFile("global.json", """
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

    /// <summary>Sets the target framework</summary>
    public ProjectBuilder WithTargetFramework(string tfm)
    {
        _properties.Add((Prop.TargetFramework, tfm));
        return this;
    }

    /// <summary>Sets the output type (Library, Exe)</summary>
    public ProjectBuilder WithOutputType(string type)
    {
        _properties.Add((Prop.OutputType, type));
        return this;
    }

    /// <summary>Sets the language version</summary>
    public ProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        _properties.Add((Prop.LangVersion, version));
        return this;
    }

    /// <summary>Sets an arbitrary MSBuild property</summary>
    public ProjectBuilder WithProperty(string name, string value)
    {
        _properties.Add((name, value));
        return this;
    }

    /// <summary>Sets multiple MSBuild properties</summary>
    public ProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        _properties.AddRange(properties);
        return this;
    }

    /// <summary>Adds a source file to the project</summary>
    public ProjectBuilder AddSource(string filename, string content)
    {
        _sourceFiles.Add((filename, content));
        return this;
    }

    /// <summary>Adds a NuGet package reference</summary>
    public ProjectBuilder WithPackage(string name, string version)
    {
        _nugetPackages.Add(new NuGetReference(name, version));
        return this;
    }

    /// <summary>Sets the project filename</summary>
    public ProjectBuilder WithFilename(string filename)
    {
        _projectFilename = filename;
        return this;
    }

    /// <summary>Overrides the SDK import style</summary>
    public ProjectBuilder WithImportStyle(SdkImportStyle style)
    {
        _importStyleOverride = style;
        return this;
    }

    /// <summary>Builds the project and returns the result</summary>
    public async Task<BuildResult> BuildAsync(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        // Generate csproj from accumulated state
        AddCsprojFile(
            [.._properties],
            [.._nugetPackages],
            filename: _projectFilename ?? "ANcpLua.TestProject.csproj",
            importStyle: _importStyleOverride ?? SdkImportStyle.Default);

        // Add all source files
        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await BuildAndGetOutput(buildArguments, environmentVariables);
    }

    private string GetSdkElementContent(string sdkName)
    {
        return $"""<Sdk Name="{sdkName}" Version="{_fixture.Version}" />""";
    }

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
        var fullPath = _directory.FullPath / RepositoryPaths.DirectoryBuildProps;
        fullPath.CreateParentDirectory();
        File.WriteAllText(fullPath, fileContent);
    }

    public ProjectBuilder AddCsprojFile((string Name, string Value)[]? properties = null,
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
                               <ErrorLog>{SarifFileName},version=2.1</ErrorLog>
                               <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                           </PropertyGroup>
                           {propertiesElement}
                           {packagesElement}
                           {string.Join('\n', additionalProjectElements?.Select(e => e.ToString()) ?? [])}
                       </Project>
                       """;

        var fullPath = _directory.FullPath / filename;
        fullPath.CreateParentDirectory();
        File.WriteAllText(fullPath, content);
        return this;
    }

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

    public async Task<BuildResult> ExecuteDotnetCommandAndGetOutput(string command, string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        _buildCount++;

        foreach (var file in Directory.GetFiles(_directory.FullPath, "*", SearchOption.AllDirectories))
        {
            _testOutputHelper.WriteLine("File: " + file);
            var content = await File.ReadAllTextAsync(file);
            _testOutputHelper.WriteLine(XmlSanitizer.SanitizeForXml(content));
        }

        _testOutputHelper.WriteLine("-------- dotnet " + command);
        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(_sdkVersion))
        {
            WorkingDirectory = _directory.FullPath,
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
        psi.Environment.Remove("DOTNET_ENVIRONMENT"); // Causes SBOM tool DI failure (microsoft/sbom-tool#278)
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
                _testOutputHelper.WriteLine(
                    $"SDK resolution or restore error detected, retrying ({retry + 1}/{maxRetries})...");

                // Exponential backoff: 100ms, 200ms, 400ms, 800ms, 1600ms
                await Task.Delay(100 * (1 << retry));

                result = await psi.RunAsTaskAsync();
            }
            else
                break;

        _testOutputHelper.WriteLine("Process exit code: " + result.ExitCode);
        _testOutputHelper.WriteLine(XmlSanitizer.SanitizeForXml(result.Output.ToString()));

        var sarifPath = _directory.FullPath / SarifFileName;
        SarifFile? sarif = null;
        if (File.Exists(sarifPath))
        {
            var bytes = await File.ReadAllBytesAsync(sarifPath);
            sarif = JsonSerializer.Deserialize<SarifFile>(bytes);
            _testOutputHelper.WriteLine("Sarif result:\n" +
                                        XmlSanitizer.SanitizeForXml(string.Join("\n",
                                            sarif!.AllResults().Select(static r => r.ToString()))));
        }
        else
            _testOutputHelper.WriteLine("Sarif file not found: " + sarifPath);

        var binlogContent = await File.ReadAllBytesAsync(_directory.FullPath / "msbuild.binlog");
        TestContext.Current.AddAttachment($"msbuild{_buildCount}.binlog", binlogContent);

        if (File.Exists(vstestdiagPath))
        {
            var vstestDiagContent = await File.ReadAllTextAsync(vstestdiagPath);
            TestContext.Current.AddAttachment(vstestdiagPath.Name, XmlSanitizer.SanitizeForXml(vstestDiagContent));
        }

        if (result.Output.Any(static line => line.Text.Contains("Could not resolve SDK")))
            Assert.Fail("The SDK cannot be found, expected version: " + _fixture.Version);

        return new BuildResult(result.ExitCode, result.Output, sarif, binlogContent);
    }

    public Task ExecuteGitCommand(params string[]? arguments)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = _directory.FullPath,
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
}

/// <summary>
///     Utility to sanitize strings for XML 1.0 compatibility.
///     XML 1.0 does not allow certain control characters (like form feed 0x0C).
/// </summary>
internal static class XmlSanitizer
{
    /// <summary>
    ///     Removes characters that are invalid in XML 1.0 documents.
    ///     Valid characters: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
    /// </summary>
    public static string SanitizeForXml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        // Fast path: check if any invalid characters exist
        var hasInvalidChars = text.Any(static ch => !IsValidXmlChar(ch));

        return !hasInvalidChars
            ? text
            :
            // Slow path: filter out invalid characters
            new string(text.Where(IsValidXmlChar).ToArray());
    }

    private static bool IsValidXmlChar(char ch)
    {
        // XML 1.0 valid characters:
        // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
        // Note: Surrogate pairs (#x10000-#x10FFFF) are handled by .NET as two chars
        return ch == 0x9 ||
               ch == 0xA ||
               ch == 0xD ||
               (ch >= 0x20 && ch <= 0xD7FF) ||
               (ch >= 0xE000 && ch <= 0xFFFD);
    }
}