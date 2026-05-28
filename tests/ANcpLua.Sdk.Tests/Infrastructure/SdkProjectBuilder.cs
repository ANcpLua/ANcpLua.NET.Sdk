using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public sealed class SdkProjectBuilder : IAsyncDisposable
{
    private const string DefaultProjectFilename = "ANcpLua.TestProject.csproj";

    private readonly PackageFixture _fixture;
    private readonly TemporaryDirectory _directory = TemporaryDirectory.Create();
    private readonly FullPath _githubStepSummaryFile;
    private readonly List<NuGetReference> _packages = [];
    private readonly List<(string Name, string Content)> _sourceFiles = [];
    private readonly List<XElement> _additionalProjectElements = [];
    private readonly Dictionary<string, string> _extraProperties = new(StringComparer.Ordinal);
    private readonly HashSet<string> _propertiesToRecord = new(StringComparer.Ordinal);

    private string _sdkName;
    private string _projectFilename = DefaultProjectFilename;
    private NetSdkVersion _sdkVersion = NetSdkVersion.Net100;
    private string? _targetFramework;
    private string? _outputType;
    private bool _omitOutputType;
    private int _buildCount;

    private SdkProjectBuilder(PackageFixture fixture, SdkImportStyle importStyle, string sdkName)
    {
        _fixture = fixture;
        ImportStyle = importStyle;
        _sdkName = sdkName;
        _githubStepSummaryFile = _directory.CreateEmptyFile("GITHUB_STEP_SUMMARY.txt");

        _directory.CreateTextFile("global.json", """
            {
              "sdk": {
                "rollForward": "latestMinor",
                "version": "10.0.100"
              }
            }
            """);

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
            </configuration>
            """);

        if (importStyle is SdkImportStyle.SdkElementDirectoryBuildProps)
            AddDirectoryBuildPropsFile(string.Empty);
    }

    private SdkImportStyle ImportStyle { get; }

    public FullPath RootFolder => _directory.FullPath;

    public IEnumerable<(string Name, string Value)> GitHubEnvironmentVariables
    {
        get
        {
            yield return ("GITHUB_ACTIONS", "true");
            yield return ("GITHUB_STEP_SUMMARY", _githubStepSummaryFile);
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The factory returns the builder unowned; every call site wraps the result in `await using`.")]
    public static SdkProjectBuilder Create(
        PackageFixture fixture,
        SdkImportStyle style = SdkImportStyle.SdkElement,
        string? sdkName = null) =>
        new SdkProjectBuilder(fixture, style, sdkName ?? PackageFixture.SdkName)
            .WithTargetFramework(Tfm.Net100)
            .WithProperty("ANcpLuaSdkSkipCPMEnforcement", "true");

    public SdkProjectBuilder WithTargetFramework(string tfm)
    {
        _targetFramework = tfm;
        return this;
    }

    public SdkProjectBuilder WithOutputType(string type)
    {
        _outputType = type;
        _omitOutputType = false;
        return this;
    }

    public SdkProjectBuilder OmitOutputType()
    {
        _omitOutputType = true;
        return this;
    }

    public SdkProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        _extraProperties[Prop.LangVersion] = version;
        return this;
    }

    public SdkProjectBuilder WithProperty(string name, string value)
    {
        switch (name)
        {
            case Prop.TargetFramework:
                _targetFramework = value;
                break;
            case Prop.OutputType:
                _outputType = value;
                _omitOutputType = false;
                break;
            default:
                _extraProperties[name] = value;
                break;
        }

        return this;
    }

    public SdkProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        foreach (var (key, value) in properties)
            WithProperty(key, value);

        return this;
    }

    public SdkProjectBuilder WithPackage(string name, string version)
    {
        _packages.Add(new NuGetReference(name, version));
        return this;
    }

    public SdkProjectBuilder WithFilename(string filename)
    {
        _projectFilename = filename;
        return this;
    }

    public SdkProjectBuilder WithDotnetSdkVersion(NetSdkVersion version)
    {
        _sdkVersion = version;
        return this;
    }

    public SdkProjectBuilder WithSdkName(string name)
    {
        _sdkName = name;
        return this;
    }

    public SdkProjectBuilder WithAdditionalProjectElement(XElement element)
    {
        _additionalProjectElements.Add(element);
        return this;
    }

    public SdkProjectBuilder RecordProperties(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
            _propertiesToRecord.Add(name);

        return this;
    }

    public SdkProjectBuilder AddSource(string filename, string content)
    {
        _sourceFiles.Add((filename, content));
        return this;
    }

    public FullPath AddFile(string relativePath, string content)
    {
        var path = _directory.FullPath / relativePath;
        path.CreateParentDirectory();
        File.WriteAllText(path, content);
        return path;
    }

    public void AddDirectoryBuildPropsFile(string postSdkContent, string preSdkContent = "", string? sdkName = null)
    {
        var sdk = ImportStyle is SdkImportStyle.SdkElementDirectoryBuildProps
            ? SdkElement(sdkName ?? _sdkName)
            : string.Empty;

        AddFile("Directory.Build.props", $"""
            <Project>
                <PropertyGroup>
                    <DisableVersionAnalyzer>true</DisableVersionAnalyzer>
                </PropertyGroup>
                {preSdkContent}
                {sdk}
                {postSdkContent}
            </Project>
            """);
    }

    public Task<BuildResult> BuildAsync(
        string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        RunMaterializedAsync("build", buildArguments, environmentVariables);

    public Task<BuildResult> PackAsync(
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null) =>
        RunMaterializedAsync("pack", arguments, environmentVariables);

    public Task<BuildResult> RestoreAsync(
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();
        return ExecuteDotnetCommandAsync("restore", arguments, environmentVariables);
    }

    private Task<BuildResult> RunMaterializedAsync(
        string command,
        string[]? arguments,
        (string Name, string Value)[]? environmentVariables)
    {
        GenerateCsprojFile();
        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return ExecuteDotnetCommandAsync(command, arguments, environmentVariables);
    }

    public async Task InitializeGitRepositoryAsync()
    {
        await ExecuteGitCommand("init");
        await ExecuteGitCommand("add", ".");
        await ExecuteGitCommand("commit", "-m", "Initial commit");
        await ExecuteGitCommand("remote", "add", "origin", "https://github.com/ancplua/sample.git");
    }

    private void GenerateCsprojFile()
    {
        var rootSdk = ImportStyle is SdkImportStyle.ProjectElement
            ? $"{_sdkName}/{_fixture.Version}"
            : "Microsoft.NET.Sdk";
        var innerSdk = ImportStyle is SdkImportStyle.SdkElement ? SdkElement(_sdkName) : string.Empty;

        var properties = new XElement("PropertyGroup",
            new XElement("ErrorLog", "BuildOutput.$(TargetFramework).sarif,version=2.1"),
            new XElement("ManagePackageVersionsCentrally", "false"));

        if (!_omitOutputType)
            properties.Add(new XElement("OutputType", _outputType ?? "exe"));
        if (_targetFramework is not null)
            properties.Add(new XElement("TargetFramework", _targetFramework));
        foreach (var (key, value) in _extraProperties)
            properties.Add(new XElement(key, value));

        var packages = new XElement("ItemGroup");
        foreach (var package in _packages)
            packages.Add(new XElement("PackageReference",
                new XAttribute("Include", package.Name),
                new XAttribute("Version", package.Version)));

        AddFile(_projectFilename, $"""
            <Project Sdk="{rootSdk}">
                {innerSdk}
                {properties}
                {packages}
                {string.Join('\n', _additionalProjectElements.Select(static e => e.ToString()))}
            {GetRecordPropertiesTargetXml()}
            </Project>
            """);
    }

    private string SdkElement(string sdkName) => $"""<Sdk Name="{sdkName}" Version="{_fixture.Version}" />""";

    public async ValueTask DisposeAsync()
    {
        if (File.Exists(_githubStepSummaryFile))
            TestContext.Current.AddAttachment(
                "GITHUB_STEP_SUMMARY",
                XmlSanitizer.Sanitize(await File.ReadAllTextAsync(_githubStepSummaryFile)));

        await _directory.DisposeAsync();
    }

    private static readonly (string Key, string Value)[] s_gitConfig =
    [
        ("user.name", "sample"),
        ("user.email", "sample@example.com"),
        ("commit.gpgsign", "false"),
        ("core.autocrlf", "false"),
        ("init.defaultBranch", "main")
    ];

    private static ITestOutputHelper? Output => TestContext.Current.TestOutputHelper;

    public async Task<BuildResult> ExecuteDotnetCommandAsync(
        string command,
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        _buildCount++;
        LogProjectFiles(command);

        var dotnetPath = await DotNetSdk.GetAsync(_sdkVersion);
        var dotnetRoot = Path.GetDirectoryName(dotnetPath.Value)
                         ?? throw new InvalidOperationException("Cannot resolve dotnet root.");
        var vstestDiagPath = _directory.FullPath / "vstestdiag.txt";

        var processArguments = new List<string> { command };
        if (arguments is not null)
            processArguments.AddRange(arguments);
        processArguments.Add("/bl");

        var configureEnvironment = BuildEnvironmentConfigurator(dotnetPath, dotnetRoot, vstestDiagPath, environmentVariables);

        Output?.WriteLine("Executing: " + dotnetPath + " " + string.Join(' ', processArguments));

        async Task<(int ExitCode, string Output)> RunAsync()
        {
            var result = await ProcessWrapper.Create(dotnetPath)
                .WithWorkingDirectory(_directory.FullPath)
                .WithArguments(processArguments)
                .WithEnvironmentVariables(configureEnvironment)
                .WithValidation(ProcessValidationMode.None)
                .ExecuteBufferedAsync(TestContext.Current.CancellationToken);
            return (result.ExitCode.Value, result.Output.ToString());
        }

        var (exitCode, output) = await RunAsync();
        const int MaxRetries = 5;
        for (var retry = 0; retry < MaxRetries && exitCode is not 0 && IsTransientSdkError(output); retry++)
        {
            Output?.WriteLine($"SDK resolution or restore error detected, retrying ({retry + 1}/{MaxRetries})...");
            await Task.Delay(100 * (1 << retry));
            (exitCode, output) = await RunAsync();
        }

        Output?.WriteLine("Process exit code: " + exitCode);
        Output?.WriteLine(XmlSanitizer.Sanitize(output));

        var sarif = LoadSarif();
        if (sarif is not null)
            Output?.WriteLine("Sarif result:\n" +
                              XmlSanitizer.Sanitize(string.Join("\n", sarif.AllResults().Select(static r => r.ToString()))));

        if (output.Contains("Could not resolve SDK", StringComparison.Ordinal))
            Assert.Fail("The SDK cannot be found, expected version: " + _fixture.Version);

        var binlog = await File.ReadAllBytesAsync(_directory.FullPath / "msbuild.binlog");
        TestContext.Current.AddAttachment($"msbuild{_buildCount}.binlog", binlog);

        if (File.Exists(vstestDiagPath))
            TestContext.Current.AddAttachment(
                vstestDiagPath.Name,
                XmlSanitizer.Sanitize(await File.ReadAllTextAsync(vstestDiagPath)));

        return new BuildResult(exitCode, output, sarif, binlog)
        {
            RecordedProperties = LoadRecordedProperties(_directory.FullPath)
        };
    }

    private async Task ExecuteGitCommand(params string[] arguments)
    {
        var gitArguments = new List<string>();
        foreach (var (key, value) in s_gitConfig)
        {
            gitArguments.Add("-c");
            gitArguments.Add($"{key}={value}");
        }

        gitArguments.AddRange(arguments);

        await ProcessWrapper.Create("git")
            .WithWorkingDirectory(_directory.FullPath)
            .WithArguments(gitArguments)
            .WithValidation(ProcessValidationMode.None)
            .ExecuteBufferedAsync(TestContext.Current.CancellationToken);
    }

    private Action<ProcessWrapperEnvironmentVariables> BuildEnvironmentConfigurator(
        FullPath dotnetPath,
        string dotnetRoot,
        FullPath vstestDiagPath,
        (string Name, string Value)[]? callerEnvironment)
    {
        var packageDirectory = _fixture.PackageDirectory;
        var architectureRootKey = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "DOTNET_ROOT_X64",
            Architecture.Arm64 => "DOTNET_ROOT_ARM64",
            _ => null
        };
        var msbuildSdksPath = ResolveMsBuildSdksPath(dotnetRoot);
        var blankedKeys = Environment.GetEnvironmentVariables().Keys.OfType<string>().Where(IsInheritedCiKey).ToArray();

        return variables =>
        {
            // Blank (override to empty), not Remove: ProcessWrapper's Remove only drops overrides,
            // so an inherited CI/GITHUB_ACTIONS would still flow to the child and flip it into CI mode.
            variables.Set("CI", "");
            variables.Set("DOTNET_ENVIRONMENT", "");
            foreach (var key in blankedKeys)
                variables.Set(key, "");

            variables.Set("MSBUILDLOGALLENVIRONMENTVARIABLES", "true");
            variables.Set("VSTestDiag", vstestDiagPath);
            variables.Set("DOTNET_ROOT", dotnetRoot);
            if (architectureRootKey is not null)
                variables.Set(architectureRootKey, dotnetRoot);
            variables.Set("DOTNET_HOST_PATH", dotnetPath);
            variables.Set("NUGET_HTTP_CACHE_PATH", packageDirectory / "http-cache");
            variables.Set("NUGET_PACKAGES", packageDirectory / "packages");
            variables.Set("NUGET_SCRATCH", packageDirectory / "nuget-scratch");
            variables.Set("NUGET_PLUGINS_CACHE_PATH", packageDirectory / "nuget-plugins-cache");
            if (msbuildSdksPath is not null)
                variables.Set("MSBuildSDKsPath", msbuildSdksPath);

            if (callerEnvironment is not null)
                foreach (var (name, value) in callerEnvironment)
                    variables.Set(name, value);
        };
    }

    private static bool IsInheritedCiKey(string key) =>
        key.StartsWith("GITHUB", StringComparison.Ordinal) ||
        key.StartsWith("RUNNER_", StringComparison.Ordinal) ||
        key.StartsWith("VSTEST", StringComparison.OrdinalIgnoreCase);

    private static string? ResolveMsBuildSdksPath(string dotnetRoot)
    {
        var sdkRoot = Path.Combine(dotnetRoot, "sdk");
        if (!Directory.Exists(sdkRoot))
            return null;

        var preferred = Path.Combine(sdkRoot, Path.GetFileName(dotnetRoot));
        var sdkPath = Directory.Exists(preferred)
            ? preferred
            : Directory.GetDirectories(sdkRoot)
                .OrderByDescending(static p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        if (sdkPath is null)
            return null;

        var path = Path.Combine(sdkPath, "Sdks");
        return Directory.Exists(path) ? path : null;
    }

    private static bool IsTransientSdkError(string output) =>
        output.Contains("error MSB4236", StringComparison.Ordinal) ||
        output.Contains("The project file may be invalid or missing targets required for restore", StringComparison.Ordinal);

    private SarifFile? LoadSarif()
    {
        var files = Directory.EnumerateFiles(_directory.FullPath, "BuildOutput*.sarif")
            .OrderBy(static f => f, StringComparer.Ordinal)
            .ToList();

        if (files.Count is 0)
        {
            Output?.WriteLine("No BuildOutput*.sarif files found in " + _directory.FullPath);
            return null;
        }

        if (files.Count is 1)
            return JsonSerializer.Deserialize<SarifFile>(File.ReadAllBytes(files[0]));

        var runs = new List<SarifFileRun>();
        foreach (var file in files)
            if (JsonSerializer.Deserialize<SarifFile>(File.ReadAllBytes(file))?.Runs is { } fileRuns)
                runs.AddRange(fileRuns);

        return new SarifFile { Runs = [.. runs] };
    }

    private void LogProjectFiles(string command)
    {
        if (Output is null)
            return;

        foreach (var file in Directory.GetFiles(_directory.FullPath, "*", SearchOption.AllDirectories))
        {
            Output.WriteLine("File: " + file);
            if (Path.GetExtension(file).Equals(".binlog", StringComparison.OrdinalIgnoreCase))
                Output.WriteLine("<binary file skipped>");
            else
                Output.WriteLine(XmlSanitizer.Sanitize(File.ReadAllText(file)));
        }

        Output.WriteLine("-------- dotnet " + command);
    }

    private string GetRecordPropertiesTargetXml()
    {
        if (_propertiesToRecord.Count is 0)
            return string.Empty;

        var items = new StringBuilder();
        foreach (var name in _propertiesToRecord.OrderBy(static n => n, StringComparer.Ordinal))
        {
            if (items.Length > 0)
                items.AppendLine();
            items.Append("      <_Recorded Include=\"").Append(name).Append("=$(").Append(name).Append(")\" />");
        }

        return $"""
                  <Target Name="_WriteRecordedProperties" AfterTargets="Build">
                    <ItemGroup>
                {items}
                    </ItemGroup>
                    <WriteLinesToFile File="$(MSBuildProjectDirectory)\obj\recorded.$(TargetFramework).properties"
                                      Lines="@(_Recorded)"
                                      Overwrite="true" />
                  </Target>
                """;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadRecordedProperties(string projectDirectory)
    {
        var objDir = Path.Combine(projectDirectory, "obj");
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        if (!Directory.Exists(objDir))
            return result;

        const string Prefix = "recorded.";
        foreach (var file in Directory.EnumerateFiles(objDir, "recorded.*.properties"))
        {
            var stem = Path.GetFileNameWithoutExtension(file);
            if (!stem.StartsWith(Prefix, StringComparison.Ordinal))
                continue;

            var props = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(file))
            {
                var index = line.IndexOf('=');
                if (index > 0)
                    props[line[..index]] = line[(index + 1)..];
            }

            result[stem[Prefix.Length..]] = props;
        }

        return result;
    }
}

internal static class XmlSanitizer
{
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return text.All(IsValidXmlChar) ? text : new string([.. text.Where(IsValidXmlChar)]);
    }

    private static bool IsValidXmlChar(char ch) =>
        ch == 0x09 || ch == 0x0A || ch == 0x0D ||
        (ch >= 0x20 && ch <= 0xD7FF) || (ch >= 0xE000 && ch <= 0xFFFD) ||
        char.IsSurrogate(ch);
}
