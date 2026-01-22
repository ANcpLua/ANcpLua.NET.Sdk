# AOT/Trim Testing Infrastructure Design

**Date:** 2026-01-22
**Status:** Approved
**Author:** Claude (with deep-think analysis)

## Overview

Add AOT and trimming test infrastructure to ANcpLua.NET.Sdk, enabling users to verify their libraries and applications work correctly when published with `PublishAot=true` or `PublishTrimmed=true`.

## Package Structure

```
ANcpLua.AotTesting.Attributes (netstandard2.0)
├── Attributes/
│   ├── AotTestAttribute.cs
│   ├── TrimTestAttribute.cs
│   ├── TrimSafeAttribute.cs
│   └── AotSafeAttribute.cs
├── TrimMode.cs
├── FeatureSwitches.cs
└── TrimAssert.cs

ANcpLua.NET.Sdk.Test (existing)
├── AotTesting/
│   ├── AotTesting.props
│   ├── AotTesting.targets
│   └── ProjectTemplate.csproj.txt
└── auto-references ANcpLua.AotTesting.Attributes when EnableAotTesting=true

ANcpLua.Analyzers (existing)
├── AL0030: AotTestMustReturnInt
├── AL0031: AotTestExitCode100Warning
├── AL0032: TrimSafeViolation
└── AL0033: AotSafeViolation
```

## Attribute Definitions

### Process Isolation Attributes

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AotTestAttribute : Attribute
{
    /// <summary>Platform to skip (e.g., "osx" skips macOS).</summary>
    public string? SkipOnPlatform { get; set; }

    /// <summary>Runtime identifier (e.g., "win-x64"). Auto-detected if null.</summary>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>Feature switches to disable. Use FeatureSwitches constants.</summary>
    public string[]? DisabledFeatureSwitches { get; set; }

    /// <summary>Build configuration. Default "Release".</summary>
    public string Configuration { get; set; } = "Release";

    /// <summary>Publish + execute timeout in seconds. Default 300 (5 min).</summary>
    public int TimeoutSeconds { get; set; } = 300;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TrimTestAttribute : Attribute
{
    public string? SkipOnPlatform { get; set; }
    public string? RuntimeIdentifier { get; set; }
    public string[]? DisabledFeatureSwitches { get; set; }
    public TrimMode TrimMode { get; set; } = TrimMode.Full;
    public string Configuration { get; set; } = "Release";
    public int TimeoutSeconds { get; set; } = 300;
}
```

### TrimMode Enum

```csharp
/// <summary>
/// Trimming mode for PublishTrimmed apps.
/// Note: TrimMode.Link is intentionally excluded (deprecated in .NET 7+).
/// </summary>
public enum TrimMode
{
    /// <summary>Only trim assemblies marked with IsTrimmable=true.</summary>
    Partial,

    /// <summary>Trim all assemblies (default in .NET 8+).</summary>
    Full
}
```

### In-Process Verification Attributes

```csharp
/// <summary>
/// Marks code as verified trim-compatible.
/// Analyzer AL0032 verifies no RequiresUnreferencedCode violations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class TrimSafeAttribute : Attribute { }

/// <summary>
/// Marks code as verified AOT-compatible.
/// Analyzer AL0033 verifies no RequiresDynamicCode violations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AotSafeAttribute : Attribute { }
```

### Feature Switches Constants

```csharp
/// <summary>
/// Compile-time validated feature switch names.
/// Use with DisabledFeatureSwitches property.
/// </summary>
public static class FeatureSwitches
{
    public const string JsonReflection =
        "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault";
    public const string DebuggerSupport =
        "System.Diagnostics.Debugger.IsSupported";
    public const string EventSourceSupport =
        "System.Diagnostics.Tracing.EventSource.IsSupported";
    public const string MetricsSupport =
        "System.Diagnostics.Metrics.Meter.IsSupported";
    public const string XmlSerialization =
        "System.Xml.XmlSerializer.IsEnabled";
    public const string BinaryFormatter =
        "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";
}
```

### Type Existence Assertions

```csharp
/// <summary>
/// Runtime assertions for verifying trim behavior.
/// Use inside [AotTest] or [TrimTest] methods.
/// </summary>
public static class TrimAssert
{
    /// <summary>
    /// Assert that a type was trimmed away (does not exist at runtime).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2057",
        Justification = "Checking if type was trimmed is the intended behavior")]
    public static void TypeTrimmed(string typeName, string assemblyName)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type != null)
        {
            Console.Error.WriteLine($"FAIL: Type '{typeName}' should have been trimmed but exists");
            Environment.Exit(-1);
        }
    }

    /// <summary>
    /// Assert that a type survived trimming (exists at runtime).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2057",
        Justification = "Checking if type exists is the intended behavior")]
    public static void TypePreserved(string typeName, string assemblyName)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type == null)
        {
            Console.Error.WriteLine($"FAIL: Type '{typeName}' should exist but was trimmed");
            Environment.Exit(-2);
        }
    }
}
```

## Test Pattern

Tests are standalone executables that return exit code 100 for success:

```csharp
using ANcpLua.AotTesting;

public class ServiceDefaultsAotTests
{
    [AotTest(DisabledFeatureSwitches = new[] { FeatureSwitches.JsonReflection })]
    public static int UnusedProvidersAreTrimmed()
    {
        // Verify unused OTel exporters are trimmed
        TrimAssert.TypeTrimmed(
            "OpenTelemetry.Exporter.ConsoleExporter",
            "OpenTelemetry.Exporter.Console");

        // Verify core types survive
        TrimAssert.TypePreserved(
            "ANcpSdk.AspNetCore.ServiceDefaults.ANcpSdkServiceDefaults",
            "ANcpSdk.AspNetCore.ServiceDefaults");

        return 100; // Success
    }

    [TrimTest(TrimMode = TrimMode.Full)]
    public static int SlimBuilderDoesNotLoadX509()
    {
        var builder = WebApplication.CreateSlimBuilder();
        var app = builder.Build();

        TrimAssert.TypeTrimmed(
            "System.Security.Cryptography.X509Certificates.X509Certificate",
            "System.Security.Cryptography");

        return 100;
    }
}
```

## MSBuild Integration

### Enabling AOT Testing

```xml
<Project Sdk="ANcpLua.NET.Sdk.Test">
  <PropertyGroup>
    <EnableAotTesting>true</EnableAotTesting>
  </PropertyGroup>
</Project>
```

### How It Works

1. **Discovery**: Test SDK discovers methods with `[AotTest]`/`[TrimTest]` attributes
2. **Project Generation**: Creates temporary .csproj for each test method
3. **Publish**: Runs `dotnet publish` with `PublishAot=true` or `PublishTrimmed=true`
4. **Execute**: Runs the published executable
5. **Validate**: Checks exit code (100 = success)

### Project Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <OutputType>Exe</OutputType>
    <RuntimeIdentifier>{RuntimeIdentifier}</RuntimeIdentifier>
    <PublishAot>{PublishAot}</PublishAot>
    <PublishTrimmed>{PublishTrimmed}</PublishTrimmed>
    <TrimMode>{TrimMode}</TrimMode>
    <SelfContained>true</SelfContained>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
  </PropertyGroup>

  <ItemGroup>
    {RuntimeHostConfigurationOptions}
  </ItemGroup>
</Project>
```

## Analyzer Rules

| Rule | Severity | Description |
|------|----------|-------------|
| AL0030 | Error | `[AotTest]`/`[TrimTest]` methods must return `int` |
| AL0031 | Warning | Return value should be 100 for success (when statically detectable) |
| AL0032 | Warning | `[TrimSafe]` code calls method with `[RequiresUnreferencedCode]` |
| AL0033 | Warning | `[AotSafe]` code calls method with `[RequiresDynamicCode]` |

## Strictness Enforcement

All 5 strictness levels enabled:

1. **IL warnings as errors**: `TreatWarningsAsErrors=true` in generated projects
2. **Missing annotations**: AL0032/AL0033 detect unsafe reflection patterns
3. **Exit code enforcement**: AL0030 requires int return, AL0031 warns on non-100
4. **Runtime type checks**: `TrimAssert.TypeTrimmed/TypePreserved` helpers
5. **Feature switch validation**: `FeatureSwitches` constants prevent typos

## Design Decisions

### Why No ExpectTrimmed(typeof(X)) Attribute?

Using `typeof(X)` in an attribute creates a compile-time reference that **roots** the type, preventing the trimmer from removing it. This defeats the purpose of testing whether X is trimmed.

Instead, use `TrimAssert.TypeTrimmed("Namespace.Type", "Assembly")` which uses string-based `Type.GetType()` at runtime.

### Why No TrimMode.Link?

`TrimMode.Link` was deprecated in .NET 7. Use:
- `TrimMode.Partial` for conservative trimming (equivalent to old Link behavior)
- `TrimMode.Full` for aggressive trimming (default in .NET 8+)

### Why Exit Code 100?

Convention from ASP.NET Core and .NET runtime trimming tests. Using 100 (not 0) prevents accidental passes from:
- Unhandled exceptions (usually exit 1 or -1)
- Process crashes (non-zero)
- Forgetting to return (random value)

### Why Separate TrimSafe and AotSafe?

AOT has stricter requirements than trimming:

| Concern | TrimSafe | AotSafe |
|---------|----------|---------|
| Reflection on unknown types | No | No |
| `Expression.Compile()` | OK | No |
| `Reflection.Emit` | OK | No |
| `Assembly.LoadFrom()` | No | No |

Code can be TrimSafe but NOT AotSafe (uses runtime codegen).

## Implementation Plan

### Phase 1: Attributes Package (Week 1)
- [ ] Create `ANcpLua.AotTesting.Attributes` project (netstandard2.0)
- [ ] Implement all attributes and enums
- [ ] Implement `TrimAssert` helper class
- [ ] Implement `FeatureSwitches` constants
- [ ] Add package to solution and CI

### Phase 2: MSBuild Infrastructure (Week 2)
- [ ] Add `AotTesting.props` with `EnableAotTesting` switch
- [ ] Add `AotTesting.targets` with project generation logic
- [ ] Create `ProjectTemplate.csproj.txt`
- [ ] Implement test discovery via reflection
- [ ] Implement publish + execute orchestration

### Phase 3: Analyzers (Week 3)
- [ ] AL0030: AotTestMustReturnInt
- [ ] AL0031: AotTestExitCode100Warning
- [ ] AL0032: TrimSafeViolation
- [ ] AL0033: AotSafeViolation
- [ ] Add tests for all analyzers

### Phase 4: Documentation & Testing (Week 4)
- [ ] Add SDK documentation
- [ ] Create example test project
- [ ] Integration tests for MSBuild targets
- [ ] Performance testing (timeout tuning)

## References

- [ASP.NET Core Trimming Tests](https://github.com/dotnet/aspnetcore/blob/main/docs/Trimming.md)
- [.NET Trimming Options](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
- [Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Runtime Feature Switches](https://github.com/dotnet/runtime/blob/main/docs/workflow/trimming/feature-switches.md)
