using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Helpers;

/// <summary>
///     SDK import style options for test projects.
/// </summary>
public enum SdkImportStyle
{
    Default,
    ProjectElement,
    SdkElement,
    SdkElementDirectoryBuildProps
}

/// <summary>
///     SDK-specific project builder that extends the base <see cref="ProjectBuilder" />.
/// </summary>
/// <remarks>
///     <para>
///         Adds SDK import style support, PackageFixture integration, and xUnit TestContext attachments.
///         Uses <see cref="TestContext.Current" /> for test output and attachments.
///     </para>
///     <para>
///         Prefer using the <see cref="Create" /> factory method for a fully-configured builder with sensible defaults.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Recommended: Use the factory method
/// await using var project = SdkProjectBuilder.Create(fixture);
/// var result = await project
///     .AddSource("Code.cs", code)
///     .BuildAsync();
///
/// // Override defaults when needed
/// await using var project = SdkProjectBuilder.Create(fixture);
/// var result = await project
///     .WithTargetFramework(Tfm.Net80)
///     .WithOutputType(Val.Exe)
///     .AddSource("Program.cs", code)
///     .BuildAsync();
/// </code>
/// </example>
public sealed class SdkProjectBuilder : ProjectBuilder
{
    private readonly PackageFixture _fixture;
    private readonly List<XElement> _additionalProjectElements = [];
    private SdkImportStyle _sdkImportStyle;
    private string _sdkName;

    /// <summary>
    ///     Creates a new SDK project builder with the specified configuration.
    /// </summary>
    /// <param name="fixture">The package fixture providing SDK version and package paths.</param>
    /// <param name="defaultSdkImportStyle">The SDK import style to use.</param>
    /// <param name="defaultSdkName">The SDK name to use.</param>
    public SdkProjectBuilder(
        PackageFixture fixture,
        SdkImportStyle defaultSdkImportStyle,
        string defaultSdkName) : base(TestContext.Current.TestOutputHelper)
    {
        _fixture = fixture;
        _sdkImportStyle = defaultSdkImportStyle;
        _sdkName = defaultSdkName;

        // Override default project filename to enable SDK branding (_IsANcpLuaProject detection)
        ProjectFilename = "ANcpLua.TestProject.csproj";

        // Configure NuGet with local package source
        // No packageSourceMapping - NuGet tries sources in order (TestSource first, then nuget.org)
        WithNuGetConfig($"""
                         <configuration>
                             <config>
                                 <add key="globalPackagesFolder" value="{_fixture.PackageDirectory}/packages" />
                             </config>
                             <packageSources>
                                 <clear />
                                 <add key="TestSource" value="{_fixture.PackageDirectory}" />
                                 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                             </packageSources>
                         </configuration>
                         """);

        if (defaultSdkImportStyle is SdkImportStyle.SdkElementDirectoryBuildProps)
            AddDirectoryBuildPropsFile(string.Empty);
    }

    /// <summary>
    ///     Creates a fully-configured SDK test builder with sensible defaults.
    /// </summary>
    /// <param name="fixture">The package fixture providing SDK version and package paths.</param>
    /// <param name="style">The SDK import style. Defaults to <see cref="SdkImportStyle.SdkElement" />.</param>
    /// <param name="sdkName">The SDK name. Defaults to <see cref="PackageFixture.SdkName" />.</param>
    /// <returns>A new <see cref="SdkProjectBuilder" /> configured with TFM=net10.0.</returns>
    /// <remarks>
    ///     <para>This factory method provides the recommended way to create SDK test projects.</para>
    ///     <para>Default configuration:</para>
    ///     <list type="bullet">
    ///         <item><description>Target Framework: net10.0</description></item>
    ///         <item><description>Output Type: Exe (allows top-level statements)</description></item>
    ///         <item><description>SDK Import Style: SdkElement</description></item>
    ///         <item><description>SDK Name: ANcpLua.NET.Sdk</description></item>
    ///     </list>
    ///     <para>Override OutputType with <see cref="WithOutputType"/> when testing library scenarios.</para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Most tests - use defaults
    /// await using var project = SdkProjectBuilder.Create(fixture);
    /// var result = await project.AddSource("Code.cs", code).BuildAsync();
    ///
    /// // Override when needed
    /// await using var project = SdkProjectBuilder.Create(fixture, SdkImportStyle.ProjectElement);
    /// var result = await project
    ///     .WithTargetFramework(Tfm.Net80)
    ///     .AddSource("Code.cs", code)
    ///     .BuildAsync();
    /// </code>
    /// </example>
    public static SdkProjectBuilder Create(
        PackageFixture fixture,
        SdkImportStyle style = SdkImportStyle.SdkElement,
        string? sdkName = null)
    {
        var builder = new SdkProjectBuilder(fixture, style, sdkName ?? PackageFixture.SdkName);
        builder.WithTargetFramework(Tfm.Net100);
        // OutputType=Exe is set in GenerateCsprojFile to allow top-level statements
        return builder;
    }

    /// <summary>
    ///     Gets the current test output helper from TestContext.
    /// </summary>
    private static ITestOutputHelper? Output => TestContext.Current.TestOutputHelper;

    #region SDK-specific fluent methods

    // Shadowing base class methods to return SdkProjectBuilder for proper chaining

    /// <summary>
    ///     Sets the target framework for the project.
    /// </summary>
    public new SdkProjectBuilder WithTargetFramework(string tfm)
    {
        base.WithTargetFramework(tfm);
        return this;
    }

    /// <summary>
    ///     Sets the output type of the project.
    /// </summary>
    public new SdkProjectBuilder WithOutputType(string type)
    {
        base.WithOutputType(type);
        return this;
    }

    /// <summary>
    ///     Sets the C# language version for the project.
    /// </summary>
    public new SdkProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        base.WithLangVersion(version);
        return this;
    }

    /// <summary>
    ///     Sets an arbitrary MSBuild property on the project.
    /// </summary>
    public new SdkProjectBuilder WithProperty(string name, string value)
    {
        base.WithProperty(name, value);
        return this;
    }

    /// <summary>
    ///     Sets multiple MSBuild properties on the project.
    /// </summary>
    public new SdkProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        base.WithProperties(properties);
        return this;
    }

    /// <summary>
    ///     Adds a source file to the project.
    /// </summary>
    public new SdkProjectBuilder AddSource(string filename, string content)
    {
        base.AddSource(filename, content);
        return this;
    }

    /// <summary>
    ///     Adds a NuGet package reference to the project.
    /// </summary>
    public new SdkProjectBuilder WithPackage(string name, string version)
    {
        base.WithPackage(name, version);
        return this;
    }

    /// <summary>
    ///     Sets the project filename.
    /// </summary>
    public new SdkProjectBuilder WithFilename(string filename)
    {
        base.WithFilename(filename);
        return this;
    }

    /// <summary>
    ///     Sets the root SDK for the project.
    /// </summary>
    public new SdkProjectBuilder WithRootSdk(string sdk)
    {
        base.WithRootSdk(sdk);
        return this;
    }

    /// <summary>
    ///     Sets the .NET SDK version to use for building.
    /// </summary>
    public new SdkProjectBuilder WithDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        base.WithDotnetSdkVersion(dotnetSdkVersion);
        return this;
    }

    /// <summary>
    ///     Enables Microsoft Testing Platform (MTP) mode.
    /// </summary>
    public new SdkProjectBuilder WithMtpMode()
    {
        base.WithMtpMode();
        return this;
    }

    /// <summary>
    ///     Adds a Directory.Build.props file.
    /// </summary>
    /// <remarks>
    ///     Automatically injects DisableVersionAnalyzer property for test projects.
    /// </remarks>
    public new SdkProjectBuilder WithDirectoryBuildProps(string content)
    {
        // Inject DisableVersionAnalyzer to prevent AL0017-AL0019 errors in test projects
        var modifiedContent = content.Replace(
            "<Project>",
            """
            <Project>
                <PropertyGroup>
                    <DisableVersionAnalyzer>true</DisableVersionAnalyzer>
                </PropertyGroup>
            """);
        base.WithDirectoryBuildProps(modifiedContent);
        return this;
    }

    /// <summary>
    ///     Adds a Directory.Packages.props file for Central Package Management.
    /// </summary>
    public new SdkProjectBuilder WithDirectoryPackagesProps(string content)
    {
        base.WithDirectoryPackagesProps(content);
        return this;
    }

    // SDK-specific methods

    /// <summary>
    ///     Sets the SDK import style for this project.
    /// </summary>
    /// <param name="style">The SDK import style to use.</param>
    /// <returns>The current builder for method chaining.</returns>
    public SdkProjectBuilder WithSdkImportStyle(SdkImportStyle style)
    {
        _sdkImportStyle = style;
        return this;
    }

    /// <summary>
    ///     Sets the SDK name for this project.
    /// </summary>
    /// <param name="name">The SDK name (e.g., "ANcpLua.NET.Sdk", "ANcpSdk.Test").</param>
    /// <returns>The current builder for method chaining.</returns>
    public SdkProjectBuilder WithSdkName(string name)
    {
        _sdkName = name;
        return this;
    }

    /// <summary>
    ///     Adds an additional XML element to the project file.
    /// </summary>
    /// <param name="element">The XML element to add (e.g., ItemGroup, Target).</param>
    /// <returns>The current builder for method chaining.</returns>
    public SdkProjectBuilder WithAdditionalProjectElement(XElement element)
    {
        _additionalProjectElements.Add(element);
        return this;
    }

    /// <summary>
    ///     Configures the project to use the Web SDK (ANcpSdk.Web).
    /// </summary>
    /// <returns>The current builder for method chaining.</returns>
    public SdkProjectBuilder UseWebSdk() => WithSdkName(PackageFixture.SdkWebName);

    /// <summary>
    ///     Configures the project to use the Test SDK (ANcpSdk.Test).
    /// </summary>
    /// <returns>The current builder for method chaining.</returns>
    public SdkProjectBuilder UseTestSdk() => WithSdkName(PackageFixture.SdkTestName);

    #endregion

    /// <summary>
    ///     Adds a Directory.Build.props file with optional SDK import.
    /// </summary>
    /// <remarks>
    ///     Automatically disables the version analyzer (AL0017-AL0019) for test projects
    ///     since they don't have a Version.props file.
    /// </remarks>
    public void AddDirectoryBuildPropsFile(string postSdkContent, string preSdkContent = "", string? sdkName = null)
    {
        var sdk = _sdkImportStyle == SdkImportStyle.SdkElementDirectoryBuildProps
            ? GetSdkElementContent(sdkName ?? _sdkName)
            : string.Empty;

        var content = $"""
                       <Project>
                           <PropertyGroup>
                               <DisableVersionAnalyzer>true</DisableVersionAnalyzer>
                           </PropertyGroup>
                           {preSdkContent}
                           {sdk}
                           {postSdkContent}
                       </Project>
                       """;

        AddFile(RepositoryPaths.DirectoryBuildProps, content);
    }

    /// <inheritdoc />
    protected override void GenerateCsprojFile()
    {
        var rootSdkName = _sdkImportStyle == SdkImportStyle.ProjectElement
            ? $"{_sdkName}/{_fixture.Version}"
            : RootSdk;
        var innerSdkXmlElement = _sdkImportStyle == SdkImportStyle.SdkElement ? GetSdkElementContent(_sdkName) : string.Empty;

        var propertiesElement = new XElement("PropertyGroup");
        foreach (var prop in Properties)
            propertiesElement.Add(new XElement(prop.Key, prop.Value));

        var packagesElement = new XElement("ItemGroup");
        foreach (var package in NuGetPackages)
            packagesElement.Add(new XElement("PackageReference",
                new XAttribute("Include", package.Name),
                new XAttribute("Version", package.Version)));

        // Only set default OutputType=exe if user didn't explicitly set it via WithOutputType
        var hasExplicitOutputType = Properties.Any(p => p.Key == Prop.OutputType);
        var defaultOutputType = hasExplicitOutputType ? "" : "<OutputType>exe</OutputType>";

        var content = $"""
                       <Project Sdk="{rootSdkName}">
                           {innerSdkXmlElement}
                           <PropertyGroup>
                               {defaultOutputType}
                               <ErrorLog>{SarifFileName},version=2.1</ErrorLog>
                               <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                               <ANcpLuaSdkSkipCPMEnforcement>true</ANcpLuaSdkSkipCPMEnforcement>
                           </PropertyGroup>
                           {propertiesElement}
                           {packagesElement}
                           {string.Join('\n', _additionalProjectElements.Select(static e => e.ToString()))}
                       </Project>
                       """;

        AddFile(ProjectFilename ?? "ANcpLua.TestProject.csproj", content);
    }

    private string GetSdkElementContent(string sdkName) =>
        $"""<Sdk Name="{sdkName}" Version="{_fixture.Version}" />""";

    #region Backward compatibility - deprecated methods

    /// <summary>
    ///     Adds a csproj file with SDK-specific configuration.
    /// </summary>
    /// <remarks>
    ///     This method is deprecated. Prefer using the fluent API:
    ///     <code>
    /// await using var project = SdkProjectBuilder.Create(fixture);
    /// var result = await project
    ///     .WithTargetFramework(Tfm.Net100)
    ///     .AddSource("Code.cs", code)
    ///     .BuildAsync();
    /// </code>
    /// </remarks>
    [Obsolete("Use fluent API: SdkProjectBuilder.Create(fixture).WithTargetFramework().AddSource().BuildAsync()")]
    public SdkProjectBuilder AddCsprojFile(
        (string Name, string Value)[]? properties = null,
        NuGetReference[]? nuGetPackages = null,
        IEnumerable<XElement>? additionalProjectElements = null,
        string? sdk = null,
        string? rootSdk = null,
        string filename = "ANcpLua.TestProject.csproj",
        SdkImportStyle importStyle = SdkImportStyle.Default)
    {
        // Migrate parameters to fluent state
        if (properties is not null)
            foreach (var prop in properties)
                WithProperty(prop.Name, prop.Value);

        if (nuGetPackages is not null)
            foreach (var pkg in nuGetPackages)
                WithPackage(pkg.Name, pkg.Version);

        if (additionalProjectElements is not null)
            foreach (var element in additionalProjectElements)
                _additionalProjectElements.Add(element);

        if (sdk is not null) _sdkName = sdk;
        if (rootSdk is not null) RootSdk = rootSdk;
        if (filename != "ANcpLua.TestProject.csproj") WithFilename(filename);
        if (importStyle != SdkImportStyle.Default) _sdkImportStyle = importStyle;

        // Generate immediately for backward compatibility
        GenerateCsprojFile();
        return this;
    }

    /// <summary>
    ///     Sets the .NET SDK version (alias for WithDotnetSdkVersion for backward compatibility).
    /// </summary>
    public void SetDotnetSdkVersion(NetSdkVersion version) => WithDotnetSdkVersion(version);

    /// <summary>
    ///     Builds the project (alias for BuildAsync for backward compatibility).
    /// </summary>
    public Task<BuildResult> BuildAndGetOutput(
        string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        ExecuteDotnetCommandAsync("build", buildArguments, environmentVariables);

    /// <summary>
    ///     Packs the project (alias for PackAsync for backward compatibility).
    /// </summary>
    public Task<BuildResult> PackAndGetOutput(
        string[]? packArguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        ExecuteDotnetCommandAsync("pack", packArguments, environmentVariables);

    /// <summary>
    ///     Runs tests (alias for TestAsync for backward compatibility).
    /// </summary>
    public Task<BuildResult> TestAndGetOutput(
        string[]? testArguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        ExecuteDotnetCommandAsync("test", testArguments, environmentVariables);

    /// <summary>
    ///     Runs the project (alias for RunAsync for backward compatibility).
    /// </summary>
    public Task<BuildResult> RunAndGetOutput(
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        ExecuteDotnetCommandAsync("run", ["--", .. arguments ?? []], environmentVariables);

    /// <summary>
    ///     Restores the project (alias for RestoreAsync for backward compatibility).
    /// </summary>
    public Task<BuildResult> RestoreAndGetOutput(
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        ExecuteDotnetCommandAsync("restore", arguments, environmentVariables);

    /// <summary>
    ///     Executes an arbitrary dotnet command (alias for ExecuteDotnetCommandAsync for backward compatibility).
    /// </summary>
    public Task<BuildResult> ExecuteDotnetCommandAndGetOutput(
        string command,
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        ExecuteDotnetCommandAsync(command, arguments, environmentVariables);

    #endregion

    /// <inheritdoc />
    public override async Task<BuildResult> ExecuteDotnetCommandAsync(
        string command,
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        BuildCount++;

        // Log all files using TestContext
        if (Output is not null)
        {
            foreach (var file in System.IO.Directory.GetFiles(Directory.FullPath, "*", SearchOption.AllDirectories))
            {
                Output.WriteLine("File: " + file);
                var content = await File.ReadAllTextAsync(file);
                Output.WriteLine(XmlSanitizer.SanitizeForXml(content));
            }

            Output.WriteLine("-------- dotnet " + command);
        }

        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(SdkVersion))
        {
            WorkingDirectory = Directory.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add(command);

        // For 'run' command:
        // 1. Explicitly specify the project file (prevents "not a valid project file" errors
        //    when Directory.Build.props exists or SDK resolution is complex)
        // 2. /bl must come BEFORE the '--' separator (otherwise becomes app argument)
        var dashDashIndex = arguments is not null ? Array.IndexOf(arguments, "--") : -1;
        if (command == "run")
        {
            // Explicitly specify the project file
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add(ProjectFilename ?? "ANcpLua.TestProject.csproj");

            if (dashDashIndex >= 0)
            {
                // Add args before --, then /bl, then -- and remaining args
                for (var i = 0; i < dashDashIndex; i++)
                    psi.ArgumentList.Add(arguments![i]);
                psi.ArgumentList.Add("/bl");
                for (var i = dashDashIndex; i < arguments!.Length; i++)
                    psi.ArgumentList.Add(arguments[i]);
            }
            else
            {
                if (arguments is not null)
                    foreach (var arg in arguments)
                        psi.ArgumentList.Add(arg);
                psi.ArgumentList.Add("/bl");
            }
        }
        else
        {
            if (arguments is not null)
                foreach (var arg in arguments)
                    psi.ArgumentList.Add(arg);
            psi.ArgumentList.Add("/bl");
        }

        // Remove interfering environment variables
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

        Output?.WriteLine("Executing: " + psi.FileName + " " + string.Join(' ', psi.ArgumentList));
        foreach (var env in psi.Environment.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            Output?.WriteLine($"  {env.Key}={env.Value}");

        var result = await psi.RunAsTaskAsync();

        // Retry on SDK resolution errors
        const int maxRetries = 5;
        for (var retry = 0; retry < maxRetries && result.ExitCode is not 0; retry++)
            if (result.Output.Any(static line => line.Text.Contains("error MSB4236", StringComparison.Ordinal) ||
                                                 line.Text.Contains(
                                                     "The project file may be invalid or missing targets required for restore",
                                                     StringComparison.Ordinal)))
            {
                Output?.WriteLine($"SDK resolution or restore error detected, retrying ({retry + 1}/{maxRetries})...");
                await Task.Delay(100 * (1 << retry));
                result = await psi.RunAsTaskAsync();
            }
            else
                break;

        Output?.WriteLine("Process exit code: " + result.ExitCode);
        Output?.WriteLine(XmlSanitizer.SanitizeForXml(result.Output.ToString()));

        // Parse SARIF
        var sarifPath = Directory.FullPath / SarifFileName;
        SarifFile? sarif = null;
        if (File.Exists(sarifPath))
        {
            var bytes = await File.ReadAllBytesAsync(sarifPath);
            sarif = JsonSerializer.Deserialize<SarifFile>(bytes);
            if (sarif is not null)
                Output?.WriteLine("Sarif result:\n" +
                                  XmlSanitizer.SanitizeForXml(string.Join("\n",
                                      sarif.AllResults().Select(static r => r.ToString()))));
        }
        else
            Output?.WriteLine("Sarif file not found: " + sarifPath);

        // Attach binlog to TestContext
        var binlogContent = await File.ReadAllBytesAsync(Directory.FullPath / "msbuild.binlog");
        TestContext.Current.AddAttachment($"msbuild{BuildCount}.binlog", binlogContent);

        // Attach vstest diagnostics if present
        if (File.Exists(vstestdiagPath))
        {
            var vstestDiagContent = await File.ReadAllTextAsync(vstestdiagPath);
            TestContext.Current.AddAttachment(vstestdiagPath.Name, XmlSanitizer.SanitizeForXml(vstestDiagContent));
        }

        // Fail fast on SDK resolution errors
        if (result.Output.Any(static line => line.Text.Contains("Could not resolve SDK")))
            Assert.Fail("The SDK cannot be found, expected version: " + _fixture.Version);

        return new BuildResult(result.ExitCode, result.Output, sarif, binlogContent);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        TestContext.Current.AddAttachment("GITHUB_STEP_SUMMARY",
            XmlSanitizer.SanitizeForXml(GetGitHubStepSummaryContent()));
        return base.DisposeAsync();
    }

    /// <summary>
    ///     Initializes a git repository in the project directory.
    ///     This is required for many SDK features like SourceLink, versioning, and SBOM generation.
    /// </summary>
    public async Task InitializeGitRepoAsync()
    {
        await ExecuteGitCommand("init");
        await ExecuteGitCommand("add", ".");
        await ExecuteGitCommand("commit", "-m", "Initial commit");
        await ExecuteGitCommand("remote", "add", "origin", "https://github.com/ancplua/sample.git");
    }

    /// <summary>
    ///     Executes a git command in the project directory.
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
}

/// <summary>
///     Utility to sanitize strings for XML 1.0 compatibility.
/// </summary>
internal static class XmlSanitizer
{
    public static string SanitizeForXml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        var hasInvalidChars = text.Any(static ch => !IsValidXmlChar(ch));
        return !hasInvalidChars ? text : new string(text.Where(IsValidXmlChar).ToArray());
    }

    private static bool IsValidXmlChar(char ch) =>
        ch == 0x9 ||
        ch == 0xA ||
        ch == 0xD ||
        (ch >= 0x20 && ch <= 0xD7FF) ||
        (ch >= 0xE000 && ch <= 0xFFFD);
}
