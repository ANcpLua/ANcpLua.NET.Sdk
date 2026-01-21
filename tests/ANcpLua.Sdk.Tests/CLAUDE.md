# CLAUDE.md - ANcpLua.Sdk.Tests

Full SDK behavior test suite validating all SDK features.

## Commands

```bash
# Run all tests
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj

# Run with filter (xUnit v3 MTP syntax)
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-method "*BannedApi*"

# Run specific test class
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-class "*Sdk100RootTests"

# Run polyfill tests only
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-class "*PolyfillTests"
```

## Test Architecture

Tests use `ProjectBuilder` to create isolated .NET projects and validate SDK behavior:

```csharp
await using var project = CreateProjectBuilder();
project.AddCsprojFile([("OutputType", "Library")]);
project.AddFile("sample.cs", "public class Sample { }");
var result = await project.BuildAsync();
result.AssertSuccess();
```

## Test Classes

| Class                     | Purpose                                                           |
|---------------------------|-------------------------------------------------------------------|
| SdkTests                  | Core SDK features (properties, package injection, build behavior) |
| BannedApiTests            | BannedApiAnalyzers enforcement                                    |
| MtpDetectionTests         | Microsoft Testing Platform auto-detection                         |
| PolyfillInjectionTests    | netstandard2.0 polyfill injection path validation                 |
| PolyfillTests             | Polyfill activation, negative tests, and multi-polyfill combos    |
| FakeLoggerExtensionsTests | FakeLogger test helpers                                           |
| ClaudeBrainTests          | CLAUDE.md generation                                              |
| JonSkeetAnalyzerTests     | Jon Skeet's NodaTime analyzer validation                          |

## Key Test Patterns

### SdkImportStyle Variants

Tests run with 3 SDK import styles to ensure all work correctly:

- `ProjectElement`: `<Project Sdk="ANcpLua.NET.Sdk">`
- `SdkElement`: `<Sdk Name="ANcpLua.NET.Sdk" />`
- `DirectoryBuildProps`: SDK in Directory.Build.props

### Fixture Pattern

`PackageFixture` provides shared SDK packages:

- Builds SDK once at test run start
- All tests share the same built packages
- Speeds up test execution significantly

## Helpers Directory

| File                    | Purpose                        |
|-------------------------|--------------------------------|
| PackageFixture.cs       | Builds and caches SDK packages |
| ProjectBuilder.cs       | Creates isolated test projects |
| SdkBrandingConstants.cs | SDK naming constants           |

## Infrastructure Directory

| File              | Purpose                             |
|-------------------|-------------------------------------|
| RepositoryRoot.cs | Locates repo root for file access   |
| BuildResult.cs    | Build output parsing and assertions |

## Common Test Scenarios

- Banned API detection (legacy time APIs, Newtonsoft)
- Package injection (ANcpLua.Analyzers auto-added)
- MTP detection (xunit.v3.mtp-v2 triggers OutputType=Exe)
- Polyfill compilation (Index/Range on netstandard2.0)
- Deterministic builds (hash consistency)
- SourceLink generation