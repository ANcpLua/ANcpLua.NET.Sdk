# ANcpLua.NET.Sdk — Architecture

## Diagram 1: SDK Import Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              YOUR PROJECT                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  qyl.telemetry.csproj                                                    │    │
│  │  ════════════════════                                                    │    │
│  │  <Project Sdk="ANcpLua.NET.Sdk">                                         │    │
│  │    <PropertyGroup>                                                       │    │
│  │      <TargetFrameworks>net10.0;net8.0;netstandard2.0</TargetFrameworks>  │    │
│  │      <InjectSharedThrow>true</InjectSharedThrow>  ←── override defaults  │    │
│  │    </PropertyGroup>                                                      │    │
│  │  </Project>                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└───────────────────────────────────┬─────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         global.json (SDK Resolution)                             │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  {                                                                       │    │
│  │    "sdk": { "version": "10.0.100" },                                     │    │
│  │    "msbuild-sdks": {                                                     │    │
│  │      "ANcpLua.NET.Sdk": "1.0.0",      ←── version centralized here       │    │
│  │      "ANcpLua.NET.Sdk.Web": "1.0.0",                                     │    │
│  │      "ANcpLua.NET.Sdk.Test": "1.0.0"                                     │    │
│  │    }                                                                     │    │
│  │  }                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└───────────────────────────────────┬─────────────────────────────────────────────┘
                                    │
           ┌────────────────────────┴────────────────────────┐
           │ NuGet resolves SDK package                       │
           └────────────────────────┬────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    ANcpLua.NET.Sdk NuGet Package                                │
│                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────┐   │
│  │  Package Contents:                                                        │   │
│  │                                                                           │   │
│  │  ANcpLua.NET.Sdk.1.0.0.nupkg                                              │   │
│  │  ├── Sdk/                                                                 │   │
│  │  │   ├── Sdk.props      ←── IMPORTED FIRST (sets defaults)               │   │
│  │  │   └── Sdk.targets    ←── IMPORTED LAST (injects code)                 │   │
│  │  ├── Shared/                                                              │   │
│  │  │   ├── Throw/Throw.cs                                                   │   │
│  │  │   └── Polyfills/                                                       │   │
│  │  │       ├── Lock.cs                                                      │   │
│  │  │       ├── IsExternalInit.cs                                            │   │
│  │  │       ├── RecordAttribute.cs                                           │   │
│  │  │       ├── NullableAttributes.cs                                        │   │
│  │  │       └── CallerArgumentExpressionAttribute.cs                         │   │
│  │  └── Analyzers/                                                           │   │
│  │      └── BannedSymbols.txt                                                │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    │
           ┌────────────────────────┴────────────────────────┐
           │ MSBuild imports Sdk.props BEFORE your .csproj    │
           │ MSBuild imports Sdk.targets AFTER your .csproj   │
           └────────────────────────┬────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           BUILD EXECUTION ORDER                                  │
│                                                                                  │
│   ┌─────────────────────────────────────────────────────────────────────────┐   │
│   │  1. Sdk.props                    2. Your .csproj        3. Sdk.targets  │   │
│   │  ══════════════                  ═══════════════        ═══════════════ │   │
│   │                                                                          │   │
│   │  • Import Microsoft.NET.Sdk      • Your <PropertyGroup>  • Detect TFM   │   │
│   │  • Set LangVersion=latest        • Your <ItemGroup>      • Add Throw.cs │   │
│   │  • Set Nullable=enable           • Override defaults     • Add Lock.cs  │   │
│   │  • Set InjectSharedThrow=true                            • Add BannedAPI│   │
│   │  • Set EnableBannedApiAnalyzer                           • Touch CLAUDE │   │
│   │                                                                          │   │
│   └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Diagram 2: Polyfill Injection Matrix

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                     POLYFILL INJECTION MATRIX                                    │
│                                                                                  │
│   Sdk.targets detects $(TargetFramework) and conditionally adds <Compile> items │
│                                                                                  │
│   ┌────────────────────────────────────────────────────────────────────────┐    │
│   │                                                                         │    │
│   │  <PropertyGroup Condition="'$(InjectPolyfillsAuto)' == 'true'">         │    │
│   │    <_NeedsLock Condition="!$([MSBuild]::IsTargetFrameworkCompatible(    │    │
│   │                '$(TargetFramework)', 'net9.0'))">true</_NeedsLock>      │    │
│   │    <_NeedsInit Condition="'$(TFM)' == 'netstandard2.0'">true</_Needs>   │    │
│   │  </PropertyGroup>                                                       │    │
│   │                                                                         │    │
│   └────────────────────────────────────────────────────────────────────────┘    │
│                                                                                  │
│   INJECTION RULES:                                                               │
│   ┌────────────────┬────────┬────────┬────────┬─────────────────────────────┐   │
│   │ File           │ net10  │ net9   │ net8   │ netstandard2.0              │   │
│   ├────────────────┼────────┼────────┼────────┼─────────────────────────────┤   │
│   │ Throw.cs       │   ✓    │   ✓    │   ✓    │         ✓                   │   │
│   │ Lock.cs        │   —    │   —    │   ✓    │         ✓                   │   │
│   │ IsExternalInit │   —    │   —    │   —    │         ✓                   │   │
│   │ RecordAttr     │   —    │   —    │   —    │         ✓                   │   │
│   │ NullableAttrs  │   —    │   —    │   —    │         ✓                   │   │
│   │ CallerArgExpr  │   —    │   —    │   —    │         ✓                   │   │
│   └────────────────┴────────┴────────┴────────┴─────────────────────────────┘   │
│                                                                                  │
│   ALWAYS INJECTED:                                                               │
│   ┌────────────────┬────────────────────────────────────────────────────────┐   │
│   │ BannedSymbols  │ DateTime.Now → error RS0030                            │   │
│   │                │ DateTime.UtcNow → error RS0030                         │   │
│   │                │ Thread.Sleep → error RS0030                            │   │
│   ├────────────────┼────────────────────────────────────────────────────────┤   │
│   │ CLAUDE.md      │ Touch if missing (never overwrite)                     │   │
│   └────────────────┴────────────────────────────────────────────────────────┘   │
│                                                                                  │
│   INJECTION XML:                                                                 │
│   ┌────────────────────────────────────────────────────────────────────────┐    │
│   │ <ItemGroup Condition="'$(InjectSharedThrow)' == 'true'">               │    │
│   │   <Compile Include="$(_SharedRoot)Throw\Throw.cs"                      │    │
│   │            Link="$(IntermediateOutputPath)Shared\Throw.cs"             │    │
│   │            Visible="false" />                                          │    │
│   │ </ItemGroup>                                                           │    │
│   │                                                                        │    │
│   │ <ItemGroup Condition="'$(_NeedsLock)' == 'true'">                      │    │
│   │   <Compile Include="$(_SharedRoot)Polyfills\Lock.cs"                   │    │
│   │            Link="$(IntermediateOutputPath)Polyfills\Lock.cs"           │    │
│   │            Visible="false" />                                          │    │
│   │ </ItemGroup>                                                           │    │
│   └────────────────────────────────────────────────────────────────────────┘    │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Diagram 3: Full Repository Structure

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              qyl/ REPOSITORY                                     │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  global.json                        ←── SDK version management           │    │
│  │  ════════════                                                            │    │
│  │  {                                                                       │    │
│  │    "sdk": { "version": "10.0.100", "rollForward": "latestMinor" },       │    │
│  │    "msbuild-sdks": {                                                     │    │
│  │      "ANcpLua.NET.Sdk": "1.0.0",                                         │    │
│  │      "ANcpLua.NET.Sdk.Web": "1.0.0",                                     │    │
│  │      "ANcpLua.NET.Sdk.Test": "1.0.0"                                     │    │
│  │    }                                                                     │    │
│  │  }                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                  │
│  FILE TREE:                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  qyl/                                                                    │    │
│  │  ├── global.json                  # SDK versions                         │    │
│  │  ├── qyl.slnx                     # Solution file                        │    │
│  │  │                                                                       │    │
│  │  ├── core/                                                               │    │
│  │  │   ├── specs/                   # TypeSpec definitions                 │    │
│  │  │   │   └── main.tsp                                                    │    │
│  │  │   └── generated/               # Kiota output                         │    │
│  │  │                                                                       │    │
│  │  ├── src/                                                                │    │
│  │  │   ├── qyl.collector/           ←── Sdk="ANcpLua.NET.Sdk.Web"          │    │
│  │  │   │   ├── qyl.collector.csproj                                        │    │
│  │  │   │   ├── CLAUDE.md            # auto-generated (empty stub)          │    │
│  │  │   │   └── Program.cs                                                  │    │
│  │  │   │                                                                   │    │
│  │  │   ├── qyl.telemetry/           ←── Sdk="ANcpLua.NET.Sdk"              │    │
│  │  │   │   ├── qyl.telemetry.csproj                                        │    │
│  │  │   │   ├── CLAUDE.md            # auto-generated                       │    │
│  │  │   │   └── GenAiAttributes.cs                                          │    │
│  │  │   │                                                                   │    │
│  │  │   └── qyl.dashboard/           ←── Sdk="ANcpLua.NET.Sdk" + npm        │    │
│  │  │       ├── qyl.dashboard.csproj                                        │    │
│  │  │       ├── claude.md            # lowercase for TypeScript             │    │
│  │  │       └── package.json                                                │    │
│  │  │                                                                       │    │
│  │  ├── instrumentation/                                                    │    │
│  │  │   ├── dotnet/                  ←── Sdk="ANcpLua.NET.Sdk" (NuGet)      │    │
│  │  │   │   └── Qyl.Instrumentation.csproj                                  │    │
│  │  │   ├── python/                                                         │    │
│  │  │   └── typescript/                                                     │    │
│  │  │                                                                       │    │
│  │  └── tests/                                                              │    │
│  │      └── qyl.tests/               ←── Sdk="ANcpLua.NET.Sdk.Test"         │    │
│  │          ├── qyl.tests.csproj                                            │    │
│  │          └── CLAUDE.md                                                   │    │
│  │                                                                          │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                  │
│  EXAMPLE .csproj FILES:                                                          │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  <!-- qyl.collector.csproj - CLEAN, no boilerplate -->                  │    │
│  │  <Project Sdk="ANcpLua.NET.Sdk.Web">                                    │    │
│  │    <PropertyGroup>                                                      │    │
│  │      <PublishAot>true</PublishAot>                                      │    │
│  │    </PropertyGroup>                                                     │    │
│  │    <ItemGroup>                                                          │    │
│  │      <PackageReference Include="DuckDB.NET.Data" Version="1.2.1" />     │    │
│  │    </ItemGroup>                                                         │    │
│  │  </Project>                                                             │    │
│  │                                                                         │    │
│  │  <!-- qyl.telemetry.csproj - multi-target -->                           │    │
│  │  <Project Sdk="ANcpLua.NET.Sdk">                                        │    │
│  │    <PropertyGroup>                                                      │    │
│  │      <TargetFrameworks>net10.0;net8.0;netstandard2.0</TargetFrameworks> │    │
│  │    </PropertyGroup>                                                     │    │
│  │  </Project>                                                             │    │
│  │                                                                         │    │
│  │  <!-- qyl.tests.csproj - test project -->                               │    │
│  │  <Project Sdk="ANcpLua.NET.Sdk.Test">                                   │    │
│  │    <ItemGroup>                                                          │    │
│  │      <ProjectReference Include="..\src\qyl.collector\*.csproj" />       │    │
│  │    </ItemGroup>                                                         │    │
│  │  </Project>                                                             │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Property Reference

```
┌────────────────────────────────────┬─────────┬────────────────────────────────────┐
│ Property                           │ Default │ Description                        │
├────────────────────────────────────┼─────────┼────────────────────────────────────┤
│ InjectSharedThrow                  │ true    │ Adds Throw.cs (guard clauses)      │
│ InjectPolyfillsAuto                │ true    │ Auto-detects TFM, injects polyfills│
│ InjectDiagnosticClassesOnLegacy    │ true    │ Nullable attrs for netstandard2.0  │
│ GenerateClaudeMd                   │ true    │ Touches CLAUDE.md if missing       │
│ EnableBannedApiAnalyzer            │ true    │ Adds BannedSymbols.txt analyzer    │
├────────────────────────────────────┼─────────┼────────────────────────────────────┤
│ DisableOtelDefaults                │ false   │ Web SDK: skip OTel packages        │
│ DisableResilientHttp               │ false   │ Web SDK: skip resilient HTTP       │
│ DisableHealthChecks                │ false   │ Web SDK: skip health check UI      │
│ InjectServiceDefaults              │ true    │ Web SDK: add service defaults      │
├────────────────────────────────────┼─────────┼────────────────────────────────────┤
│ CollectCoverage                    │ true    │ Test SDK: enable coverlet          │
│ EnableBlame                        │ true    │ Test SDK: crash/hang dumps         │
│ DisableTestcontainers              │ false   │ Test SDK: skip Testcontainers      │
└────────────────────────────────────┴─────────┴────────────────────────────────────┘
```

---

## Build Output

```bash
$ dotnet build src/qyl.telemetry/qyl.telemetry.csproj

[ANcpLua.NET.Sdk] net10.0 ← Throw
[ANcpLua.NET.Sdk] net8.0 ← Throw Lock
[ANcpLua.NET.Sdk] netstandard2.0 ← Throw Lock Init Nullable

Build succeeded.
    0 Warning(s)
    0 Error(s)

# If you use DateTime.Now:
$ dotnet build

error RS0030: The symbol 'DateTime.Now' is banned: Use TimeProvider.System.GetLocalNow() for testability
```

---

## NuGet Package Structure

```
ANcpLua.NET.Sdk.1.0.0.nupkg
├── Sdk/
│   ├── Sdk.props           # Imported FIRST
│   └── Sdk.targets         # Imported LAST
├── Shared/
│   ├── Throw/
│   │   └── Throw.cs        # Microsoft.Shared.Diagnostics.Throw
│   └── Polyfills/
│       ├── Lock.cs         # System.Threading.Lock (< net9.0)
│       ├── IsExternalInit.cs
│       ├── RecordAttribute.cs
│       ├── NullableAttributes.cs
│       └── CallerArgumentExpressionAttribute.cs
├── Analyzers/
│   └── BannedSymbols.txt
└── ANcpLua.NET.Sdk.nuspec

ANcpLua.NET.Sdk.Web.1.0.0.nupkg
├── Sdk/
│   ├── Sdk.props           # Imports Microsoft.NET.Sdk.Web + ANcpLua.NET.Sdk
│   └── Sdk.targets         # Adds ServiceDefaults like Aspire to the project
└── ANcpLua.NET.Sdk.Web.nuspec

ANcpLua.NET.Sdk.Test.1.0.0.nupkg
├── Sdk/
│   ├── Sdk.props           # Sets IsTestProject, coverage
│   └── Sdk.targets         # Adds xUnit.v3, Testcontainers, coverage
// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;

namespace Microsoft.Shared.SampleUtilities;

/// <summary>
/// A logger that writes to the Xunit test output
/// </summary>
internal sealed class XunitLogger(ITestOutputHelper output) : ILoggerFactory, ILogger, IDisposable
{
    private object? _scopeState;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var localState = state?.ToString();
        var line = this._scopeState is not null ? $"{this._scopeState} {localState}" : localState;
        output.WriteLine(line);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        this._scopeState = state;
        return this;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // This class is marked as disposable to support the BeginScope method.
        // However, there is no need to dispose anything.
    }

    public ILogger CreateLogger(string categoryName) => this;

    public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
}
// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Shared.SampleUtilities;

/// <summary>
/// Extensions for <see cref="ITestOutputHelper"/> to make it more Console friendly.
/// </summary>
public static class TextOutputHelperExtensions
{
    /// <summary>
    /// Current interface ITestOutputHelper does not have a WriteLine method that takes an object. This extension method adds it to make it analogous to Console.WriteLine when used in Console apps.
    /// </summary>
    /// <param name="testOutputHelper">Target <see cref="ITestOutputHelper"/></param>
    /// <param name="target">Target object to write</param>
    public static void WriteLine(this ITestOutputHelper testOutputHelper, object target) =>
        testOutputHelper.WriteLine(target.ToString());

    /// <summary>
    /// Current interface ITestOutputHelper does not have a WriteLine method that takes no parameters. This extension method adds it to make it analogous to Console.WriteLine when used in Console apps.
    /// </summary>
    /// <param name="testOutputHelper">Target <see cref="ITestOutputHelper"/></param>
    public static void WriteLine(this ITestOutputHelper testOutputHelper) =>
        testOutputHelper.WriteLine(string.Empty);

    /// <summary>
    /// Current interface ITestOutputHelper does not have a Write method that takes no parameters. This extension method adds it to make it analogous to Console.Write when used in Console apps.
    /// </summary>
    /// <param name="testOutputHelper">Target <see cref="ITestOutputHelper"/></param>
    public static void Write(this ITestOutputHelper testOutputHelper) =>
        testOutputHelper.WriteLine(string.Empty);

    /// <summary>
    /// Current interface ITestOutputHelper does not have a Write method. This extension method adds it to make it analogous to Console.Write when used in Console apps.
    /// </summary>
    /// <param name="testOutputHelper">Target <see cref="ITestOutputHelper"/></param>
    /// <param name="target">Target object to write</param>
    public static void Write(this ITestOutputHelper testOutputHelper, object target) =>
        testOutputHelper.WriteLine(target.ToString());
}
// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Shared.Samples;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

/// <summary>
/// Provides access to application configuration settings.
/// </summary>
public sealed class TestConfiguration
{
    /// <summary>Gets the configuration settings for the OpenAI integration.</summary>
    public static OpenAIConfig OpenAI => LoadSection<OpenAIConfig>();

    /// <summary>Represents the configuration settings required to interact with the OpenAI service.</summary>
    public class OpenAIConfig
    {
        /// <summary>Gets or sets the identifier for the chat completion model used in the application.</summary>
        public string ChatModelId { get; set; }

        /// <summary>Gets or sets the API key used for authentication with the OpenAI service.</summary>
        public string ApiKey { get; set; }
    }
  // Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Samples;

namespace Microsoft.Shared.SampleUtilities;

/// <summary>
/// Provides a base class for test implementations that integrate with xUnit's <see cref="ITestOutputHelper"/>  and
/// logging infrastructure. This class also supports redirecting <see cref="System.Console"/> output  to the test output
/// for improved debugging and test output visibility.
/// </summary>
/// <remarks>
/// This class is designed to simplify the creation of test cases by providing access to logging and
/// configuration utilities, as well as enabling Console-friendly behavior for test samples. Derived classes can use
/// the <see cref="Output"/> property for writing test output and the <see cref="LoggerFactory"/> property for creating
/// loggers.
/// </remarks>
public abstract class BaseSample : TextWriter
{
    /// <summary>
    /// Gets the output helper used for logging test results and diagnostic messages.
    /// </summary>
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// Gets the <see cref="ILoggerFactory"/> instance used to create loggers for logging operations.
    /// </summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// This property makes the samples Console friendly. Allowing them to be copied and pasted into a Console app, with minimal changes.
    /// </summary>
    public BaseSample Console => this;

    /// <inheritdoc />
    public override Encoding Encoding => Encoding.UTF8;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSample"/> class, setting up logging, configuration, and
    /// optionally redirecting <see cref="System.Console"/> output to the test output.
    /// </summary>
    /// <remarks>This constructor initializes logging using an <see cref="XunitLogger"/> and sets up
    /// configuration from multiple sources, including a JSON file, environment variables, and user secrets.
    /// If <paramref name="redirectSystemConsoleOutput"/> is <see langword="true"/>, calls to <see cref="System.Console"/>
    /// will be redirected to the test output provided by <paramref name="output"/>.
    /// </remarks>
    /// <param name="output">The <see cref="ITestOutputHelper"/> instance used to write test output.</param>
    /// <param name="redirectSystemConsoleOutput">
    /// A value indicating whether <see cref="System.Console"/> output should be redirected to the test output. <see langword="true"/> to redirect; otherwise, <see langword="false"/>.
    /// </param>
    protected BaseSample(ITestOutputHelper output, bool redirectSystemConsoleOutput = true)
    {
        this.Output = output;
        this.LoggerFactory = new XunitLogger(output);

        IConfigurationRoot configRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();namespace A2A;

/// <summary>
/// Provides a declaration of a combination of target URL and supported transport to interact with an agent.
/// </summary>
public sealed class AgentInterface
{
    /// <summary>
    /// The transport supported by this URL.
    /// </summary>
    /// <remarks>
    /// This is an open form string, to be easily extended for many transport protocols.
    /// The core ones officially supported are JSONRPC, GRPC, and HTTP+JSON.
    /// </remarks>
    [JsonPropertyName("transport")]
    [JsonRequired]
    public required AgentTransport Transport { get; set; }

    /// <summary>
    /// The target URL for the agent interface.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonRequired]
    public required string Url { get; set; }
}

        TestConfiguration.Initialize(configRoot);

        // Redirect System.Console output to the test output if requested
        if (redirectSystemConsoleOutput)
        {
            System.Console.SetOut(this);
        }
    }

    /// <summary>
    /// Writes a user message to the console.
    /// </summary>
    /// <param name="message">The text of the message to be sent. Cannot be null or empty.</param>
    protected void WriteUserMessage(string message) =>
        this.WriteMessageOutput(new ChatMessage(ChatRole.User, message));

    /// <summary>
    /// Processes and writes the latest agent chat response to the console, including metadata and content details.
    /// </summary>
    /// <remarks>This method formats and outputs the most recent message from the provided <see
    /// cref="AgentRunResponse"/> object. It includes the message role, author name (if available), text content, and
    /// additional content such as images, function calls, and function results. Usage statistics, including token
    /// counts, are also displayed.</remarks>
    /// <param name="response">The <see cref="AgentRunResponse"/> object containing the chat messages and usage data.</param>
    /// <param name="printUsage">The flag to indicate whether to print usage information. Defaults to <see langword="true"/>.</param>
    protected void WriteResponseOutput(AgentRunResponse response, bool? printUsage = true)
    {
        if (response.Messages.Count == 0)
        {
            // If there are no messages, we can skip writing the message.
            return;
        }

        var message = response.Messages.Last();
        this.WriteMessageOutput(message);

        WriteUsage();

        void WriteUsage()
        {
            if (!(printUsage ?? true) || response.Usage is null) { return; }

            UsageDetails usageDetails = response.Usage;

            Console.WriteLine($"  [Usage] Tokens: {usageDetails.TotalTokenCount}, Input: {usageDetails.InputTokenCount}, Output: {usageDetails.OutputTokenCount}");
        }
    }

    /// <summary>
    /// Writes the given chat message to the console.
    /// </summary>
    /// <param name="message">The specified message</param>
    protected void WriteMessageOutput(ChatMessage message)
    {
        string authorExpression = message.Role == ChatRole.User ? string.Empty : FormatAuthor();
        string contentExpression = message.Text.Trim();
        const bool IsCode = false; //message.AdditionalProperties?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
        const string CodeMarker = IsCode ? "\n  [CODE]\n" : " ";
        Console.WriteLine($"\n# {message.Role}{authorExpression}:{CodeMarker}{contentExpression}");

        // Provide visibility for inner content (that isn't TextContent).
        foreach (AIContent item in message.Contents)
        {
            if (item is DataContent image && image.HasTopLevelMediaType("image"))
            {
                Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.Uri ?? $"{image.Data.Length} bytes"}");
            }
            else if (item is FunctionCallContent functionCall)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {functionCall.CallId}");
            }
            else if (item is FunctionResultContent functionResult)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId} - {AsJson(functionResult.Result) ?? "*"}");
            }
        }

        string FormatAuthor() => message.AuthorName is not null ? $" - {message.AuthorName ?? " * "}" : string.Empty;
    }

    /// <summary>
    /// Writes the streaming agent response updates to the console.
    /// </summary>
    /// <remarks>This method formats and outputs the most recent message from the provided <see
    /// cref="AgentRunResponseUpdate"/> object. It includes the message role, author name (if available), text content, and
    /// additional content such as images, function calls, and function results. Usage statistics, including token
    /// counts, are also displayed.</remarks>
    /// <param name="update">The <see cref="AgentRunResponseUpdate"/> object containing the chat messages and usage data.</param>
    protected void WriteAgentOutput(AgentRunResponseUpdate update)
    {
        if (update.Contents.Count == 0)
        {
            // If there are no contents, we can skip writing the message.
            return;
        }

        string authorExpression = update.Role == ChatRole.User ? string.Empty : FormatAuthor();
        string contentExpression = string.IsNullOrWhiteSpace(update.Text) ? string.Empty : update.Text;
        const bool IsCode = false; //message.AdditionalProperties?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
        const string CodeMarker = IsCode ? "\n  [CODE]\n" : " ";
        Console.WriteLine($"\n# {update.Role}{authorExpression}:{CodeMarker}{contentExpression}");

        // Provide visibility for inner content (that isn't TextContent).
        foreach (AIContent item in update.Contents)
        {
            if (item is DataContent image && image.HasTopLevelMediaType("image"))
            {
                Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.Uri ?? $"{image.Data.Length} bytes"}");
            }
            else if (item is FunctionCallContent functionCall)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {functionCall.CallId}");
            }
            else if (item is FunctionResultContent functionResult)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId} - {AsJson(functionResult.Result) ?? "*"}");
            }
            else if (item is UsageContent usage)
            {
                Console.WriteLine("  [Usage] Tokens: {0}, Input: {1}, Output: {2}",
                usage?.Details?.TotalTokenCount ?? 0,
                usage?.Details?.InputTokenCount ?? 0,
                usage?.Details?.OutputTokenCount ?? 0);
            }
        }

        string FormatAuthor() => update.AuthorName is not null ? $" - {update.AuthorName ?? " * "}" : string.Empty;
    }

    private static readonly JsonSerializerOptions s_jsonOptionsCache = new() { WriteIndented = true };

    private static string? AsJson(object? obj)
    {
        if (obj is null) { return null; }
        return JsonSerializer.Serialize(obj, s_jsonOptionsCache);
    }

    /// <inheritdoc/>
    public override void WriteLine(object? value = null)
        => this.Output.WriteLine(value ?? string.Empty);

    /// <inheritdoc/>
    public override void WriteLine(string? format, params object?[] arg)
        => this.Output.WriteLine(format ?? string.Empty, arg);

    /// <inheritdoc/>
    public override void WriteLine(string? value)
        => this.Output.WriteLine(value ?? string.Empty);

    /// <inheritdoc/>
    public override void Write(object? value = null)
        => this.Output.WriteLine(value ?? string.Empty);

    /// <inheritdoc/>
    public override void Write(char[]? buffer)
        => this.Output.WriteLine(new string(buffer));
}

using System.Text;

namespace PaperlessServices.Tests;

public static class FakeLoggerExtensions
{
	public static string GetFullLoggerText(
		this FakeLogCollector source,
		Func<FakeLogRecord, string>? formatter = null)
	{
		StringBuilder sb = new();
		IReadOnlyList<FakeLogRecord> snapshot = source.GetSnapshot();
		formatter ??= record => $"{record.Level} - {record.Message}";

		foreach (FakeLogRecord record in snapshot)
		{
			sb.AppendLine(formatter(record));
		}

		return sb.ToString();
	}

	public static async Task<bool> WaitForLogAsync(
		this FakeLogCollector source,
		Func<IReadOnlyList<FakeLogRecord>, bool> condition,
		TimeSpan? timeout = null,
		TimeSpan? pollInterval = null,
		CancellationToken cancellationToken = default)
	{
		timeout ??= TimeSpan.FromSeconds(5);
		pollInterval ??= TimeSpan.FromMilliseconds(25);

		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(timeout.Value);

		try
		{
			while (!cts.Token.IsCancellationRequested)
			{
				if (condition(source.GetSnapshot()))
				{
					return true;
				}

				await Task.Delay(pollInterval.Value, cts.Token).ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
		}

		return condition(source.GetSnapshot());
	}

	public static Task<bool> WaitForLogCountAsync(
		this FakeLogCollector source,
		Func<FakeLogRecord, bool> predicate,
		int expectedCount,
		TimeSpan? timeout = null,
		CancellationToken cancellationToken = default) =>
		source.WaitForLogAsync(
			logs => logs.Count(predicate) >= expectedCount,
			timeout,
			cancellationToken: cancellationToken);
}
// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0005 // Using directive is unnecessary. - need to suppress this, since this file is used in both projects with implicit usings and without.

using System;
using System.Collections;
using SystemEnvironment = System.Environment;

namespace SampleHelpers;

internal static class SampleEnvironment
{
    public static string? GetEnvironmentVariable(string key)
        => GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);

    public static string? GetEnvironmentVariable(string key, EnvironmentVariableTarget target)
    {
        // Allows for opting into showing all setting values in the console output, so that it is easy to troubleshoot sample setup issues.
        var showAllSampleValues = SystemEnvironment.GetEnvironmentVariable("AF_SHOW_ALL_DEMO_SETTING_VALUES", target);
        var shouldShowValue = showAllSampleValues?.ToUpperInvariant() == "Y";

        var value = SystemEnvironment.GetEnvironmentVariable(key, target);
        if (string.IsNullOrWhiteSpace(value))
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Setting '");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(key);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("' is not set in environment variables.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Please provide the setting for '");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(key);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'. Just press enter to accept the default. > ");
            Console.ForegroundColor = color;
            value = Console.ReadLine();
            value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            Console.WriteLine();
        }
        else if (shouldShowValue)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Using setting: Source=");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("EnvironmentVariables");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(", Key='");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(key);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("', Value='");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(value);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("'");
            Console.ForegroundColor = color;

            Console.WriteLine();
        }

        return value;
    }

    // Methods that directly call System.Environment

    public static IDictionary GetEnvironmentVariables()
        => SystemEnvironment.GetEnvironmentVariables();

    public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        => SystemEnvironment.GetEnvironmentVariables(target);

    public static void SetEnvironmentVariable(string variable, string? value)
        => SystemEnvironment.SetEnvironmentVariable(variable, value);

    public static void SetEnvironmentVariable(string variable, string? value, EnvironmentVariableTarget target)
        => SystemEnvironment.SetEnvironmentVariable(variable, value, target);

    public static string[] GetCommandLineArgs()
        => SystemEnvironment.GetCommandLineArgs();

    public static string CommandLine
        => SystemEnvironment.CommandLine;

    public static string CurrentDirectory
    {
        get => SystemEnvironment.CurrentDirectory;
        set => SystemEnvironment.CurrentDirectory = value;
    }

    public static string ExpandEnvironmentVariables(string name)
        => SystemEnvironment.ExpandEnvironmentVariables(name);

    public static string GetFolderPath(SystemEnvironment.SpecialFolder folder)
        => SystemEnvironment.GetFolderPath(folder);

    public static string GetFolderPath(SystemEnvironment.SpecialFolder folder, SystemEnvironment.SpecialFolderOption option)
        => SystemEnvironment.GetFolderPath(folder, option);

    public static int ProcessorCount
        => SystemEnvironment.ProcessorCount;

    public static bool Is64BitProcess
        => SystemEnvironment.Is64BitProcess;

    public static bool Is64BitOperatingSystem
        => SystemEnvironment.Is64BitOperatingSystem;

    public static string MachineName
        => SystemEnvironment.MachineName;

    public static string NewLine
        => SystemEnvironment.NewLine;

    public static OperatingSystem OSVersion
        => SystemEnvironment.OSVersion;

    public static string StackTrace
        => SystemEnvironment.StackTrace;

    public static int SystemPageSize
        => SystemEnvironment.SystemPageSize;

    public static bool HasShutdownStarted
        => SystemEnvironment.HasShutdownStarted;

#if NET
    public static int ProcessId
        => SystemEnvironment.ProcessId;

    public static string? ProcessPath
        => SystemEnvironment.ProcessPath;

    public static bool IsPrivilegedProcess
        => SystemEnvironment.IsPrivilegedProcess;
#endif
}
# Integration Tests

Common Integration test files.

To use this in your project, add the following to your `.csproj` file:

```xml
<PropertyGroup>
  <InjectSharedIntegrationTestCode>true</InjectSharedIntegrationTestCode>
</PropertyGroup>
```

    /// <summary>
    /// Initializes the configuration system with the specified configuration root.
    /// </summary>
    /// <param name="configRoot">The root of the configuration hierarchy used to initialize the system. Must not be <see langword="null"/>.</param>
    public static void Initialize(IConfigurationRoot configRoot) =>
        s_instance = new TestConfiguration(configRoot);

    #region Private Members

    private readonly IConfigurationRoot _configRoot;
    private static TestConfiguration? s_instance;

    private TestConfiguration(IConfigurationRoot configRoot)
    {
        this._configRoot = configRoot;
    }

    private static T LoadSection<T>([CallerMemberName] string? caller = null)
    {
        if (s_instance is null)
        {
            throw new InvalidOperationException(
                "TestConfiguration must be initialized with a call to Initialize(IConfigurationRoot) before accessing configuration values.");
        }

        if (string.IsNullOrEmpty(caller))
        {
            throw new ArgumentNullException(nameof(caller));
        }

        return s_instance._configRoot.GetSection(caller).Get<T>() ??
               throw new InvalidOperationException(caller);
    }

    #endregion
}

└── ANcpLua.NET.Sdk.Test.nuspec
```


