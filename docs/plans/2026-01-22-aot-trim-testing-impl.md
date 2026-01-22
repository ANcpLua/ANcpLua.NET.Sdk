# AOT/Trim Testing Implementation Plan

## File-by-File Tasks

### Package 1: ANcpLua.AotTesting.Attributes

| File | Description |
|------|-------------|
| `src/AotTesting/ANcpLua.AotTesting.Attributes.csproj` | Project file (netstandard2.0, no deps) |
| `src/AotTesting/AotTestAttribute.cs` | Process isolation AOT test attribute |
| `src/AotTesting/TrimTestAttribute.cs` | Process isolation trim test attribute |
| `src/AotTesting/TrimMode.cs` | Enum (Full, Partial only) |
| `src/AotTesting/TrimSafeAttribute.cs` | In-process trim-safe marker |
| `src/AotTesting/AotSafeAttribute.cs` | In-process AOT-safe marker |
| `src/AotTesting/FeatureSwitches.cs` | Compile-time validated switch constants |
| `src/AotTesting/TrimAssert.cs` | Runtime type existence assertions |

### Package 2: MSBuild Targets (in ANcpLua.NET.Sdk.Test)

| File | Description |
|------|-------------|
| `src/Testing/AotTesting.props` | EnableAotTesting switch and defaults |
| `src/Testing/AotTesting.targets` | Project generation and execution logic |
| `src/Testing/AotTesting/ProjectTemplate.csproj.txt` | Template for generated test projects |
| `src/Testing/AotTesting/Directory.Build.props.txt` | Support file for generated projects |

### Package 3: Analyzers (in ANcpLua.Analyzers - downstream)

| Rule | File | Description |
|------|------|-------------|
| AL0030 | `AotTestMustReturnIntAnalyzer.cs` | [AotTest]/[TrimTest] must return int |
| AL0031 | `AotTestExitCodeAnalyzer.cs` | Warn if return != 100 detected |
| AL0032 | `TrimSafeViolationAnalyzer.cs` | [TrimSafe] + RequiresUnreferencedCode |
| AL0033 | `AotSafeViolationAnalyzer.cs` | [AotSafe] + RequiresDynamicCode |

### Integration

| File | Description |
|------|-------------|
| `src/common/Version.props` | Add AotTestingVersion property |
| `src/Sdk/ANcpLua.NET.Sdk.Test/Sdk.targets` | Import AotTesting.targets |
| `Directory.Packages.props` | Add package reference entry |

## Parallel Execution Groups

### Group A (Attributes - can run in parallel)
1. `ANcpLua.AotTesting.Attributes.csproj`
2. `TrimMode.cs`
3. `FeatureSwitches.cs`
4. `AotTestAttribute.cs`
5. `TrimTestAttribute.cs`
6. `TrimSafeAttribute.cs`
7. `AotSafeAttribute.cs`
8. `TrimAssert.cs`

### Group B (MSBuild - depends on Group A design)
1. `AotTesting.props`
2. `AotTesting.targets`
3. `ProjectTemplate.csproj.txt`
4. `Directory.Build.props.txt`

### Group C (Integration - depends on Group A)
1. `Version.props` update
2. `Sdk.targets` update
3. `Directory.Packages.props` update
