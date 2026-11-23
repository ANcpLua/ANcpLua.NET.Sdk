[![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)
[![.NET 11](https://img.shields.io/badge/.NET-11.0-7C3AED)](https://dotnet.microsoft.com/download/dotnet/11.0)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

# ANcpLua.NET.Sdk

Opinionated MSBuild SDK for .NET projects.

## Quick Start

`global.json`:

```json
{
  "sdk": {
    "version": "11.0.100",
    "rollForward": "latestFeature"
  },
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "4.0.0",
    "ANcpLua.NET.Sdk.Web": "4.0.0",
    "ANcpLua.NET.Sdk.Test": "4.0.0"
  }
}
```

Requires .NET SDK 11.0.100 or later; the build stops with a `POLICY` error on an older SDK.

```xml
<!-- Library/Console/Worker -->
<Project Sdk="ANcpLua.NET.Sdk"></Project>

<!-- Web API -->
<Project Sdk="ANcpLua.NET.Sdk.Web"></Project>

<!-- Test -->
<Project Sdk="ANcpLua.NET.Sdk.Test"></Project>
```

### Central Package Management is mandatory

All variants force `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` (and the
enforcement target errors if a consumer overrides it). Every consuming repo must ship a
`Directory.Packages.props` at or above the consumer's directory — even an empty one suffices:

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

Without it, restore fails with `NU1015: PackageReference items do not have a version specified`
on the SDK-injected analyzers (`ANcpLua.Analyzers`, `Microsoft.CodeAnalysis.BannedApiAnalyzers`)
because the SDK switches them to `GlobalPackageReference`, which only resolves through CPM.

## Moving to 4.0

4.0 moves the version truth to .NET 11: .NET SDK 11.0.100, Roslyn 5.12.0, ANcpLua.Analyzers 3.0.0,
the 11.0 runtime and ASP.NET Core packages (`DotNetRuntimeVersion` 11.0.0), Microsoft.Extensions 11.0.1,
Basic.Reference.Assemblies 1.8.15, and a `net11.0` default for projects that declare no target framework.
ANcpLua.Analyzers 3.0.0 is compiled against Roslyn 5.12, so an older compiler reports CS9057 and runs
none of its rules — that is why the SDK now requires SDK 11.

Renovate opens one `dotnet-11` PR per consumer that moves `sdk.version` and the `msbuild-sdks` entries
together. Finish that PR on its branch:

1. **Target framework.** Change `net10.0` to `net11.0` (keep `netstandard2.0` for analyzers and
   generators). Packages pinned to `$(AspNetCoreVersion)` or `$(DotNetRuntimeVersion)` are 11.0 now and
   restore only for `net11.0` (NU1202 otherwise). `net11.0` defaults to C# 15; don't add `LangVersion`.
2. **Other .NET 10 pins.** A `mise.toml` `dotnet` line, `dotnet-version`/container images in CI and
   Dockerfiles (`mcr.microsoft.com/dotnet/*:10.0` to `11.0`).
3. **Local overrides.** In a repo-local `Version.props`, delete entries that are now equal to or below
   the SDK's values.
4. **New findings.** Build with `-c Release` (warnings are errors there). The usual ones:
   - `IDE0028`: collection initializers with constructor arguments become C# 15 collection expression
     arguments: `new HashSet<string>(StringComparer.Ordinal) { "a" }` becomes
     `[with(StringComparer.Ordinal), "a"]`. A `Dictionary` takes `with(...)` only with no elements
     (`CS9215` otherwise), so fill it after creating it. `dotnet format style --diagnostics IDE0028`
     applies the fix.
   - New .NET 11 CA rules ship as suggestions (see `src/Config/Analyzer.Microsoft.CodeAnalysis.NetAnalyzers.editorconfig`).
   - Analyzer or generator projects that compile against Roslyn 5.12 get `ITypeSymbol.IsUnion`,
     `IsClosed`, `GetClosedDerivedTypeInfo` and `UnionCaseTypes` as stable APIs; drop
     `RSEXPERIMENTAL006` suppressions and `LanguageVersion.Preview` workarounds for C# 15.

## Documentation

- **[ANcpLua.Analyzers rules](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/README.md#rules)** — the 89 AL00xx/AL18xx diagnostics this SDK auto-injects, organized by domain band.
- **[Per-rule editorconfig profile](src/Config/Analyzer.ANcpLua.Analyzers.editorconfig)** — the severity table shipped alongside the SDK; consumers override individual rules via the standard `dotnet_diagnostic.{ID}.severity = …` form.

## Related

- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) — Custom Roslyn analyzers (auto-injected)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) — Source generator utilities
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) — MAF runtime helpers + agent test infrastructure

---

*Initial architecture inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).*
