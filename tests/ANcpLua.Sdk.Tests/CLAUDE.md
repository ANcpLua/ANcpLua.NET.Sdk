# CLAUDE.md - ANcpLua.Sdk.Tests

Full SDK behavior test suite validating all SDK features end-to-end.

## Commands

```bash
# Run all tests
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj

# Run with filter (xUnit v3 MTP syntax)
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-method "*BannedApi*"

# Run specific test class
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-class "*Sdk100RootTests"
```

## Test Architecture

Tests use `SdkProjectBuilder` to create isolated .NET projects and validate SDK behavior:

```csharp
await using var project = SdkProjectBuilder.Create(fixture);
var result = await project
    .WithOutputType(Val.Library)
    .AddSource("sample.cs", "public class Sample { }")
    .BuildAsync();
result.ShouldSucceed();
```

## Test Classes

| Class                            | Purpose                                                           |
|----------------------------------|-------------------------------------------------------------------|
| `Sdk100RootTests` / `Sdk100InnerTests` / `Sdk100DirectoryBuildPropsTests` | Core SDK features per import style |
| `BannedApiTests`                 | BannedApiAnalyzers enforcement                                    |
| `MtpDetectionTests`              | Microsoft Testing Platform auto-detection                         |
| `SourceGeneratorDefaultsTests`   | Auto-pin Microsoft.CodeAnalysis.CSharp for generators             |

## Key Test Patterns

### SdkImportStyle Variants

Tests run with 3 SDK import styles to ensure all work correctly:

- `ProjectElement`: `<Project Sdk="ANcpLua.NET.Sdk">`
- `SdkElement`: `<Sdk Name="ANcpLua.NET.Sdk" />`
- `SdkElementDirectoryBuildProps`: SDK in Directory.Build.props

### Fixture Pattern

`PackageFixture` provides shared SDK packages — built once at test run start,
shared across all tests.
