[![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-7C3AED)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

# ANcpLua.NET.Sdk

Opinionated MSBuild SDK for .NET projects.

## Quick Start

`global.json`:

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "3.4.31",
    "ANcpLua.NET.Sdk.Web": "3.4.31",
    "ANcpLua.NET.Sdk.Test": "3.4.31"
  }
}
```

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

## Documentation

- **[ANcpLua.Analyzers rules](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/README.md#rules)** — the 89 AL00xx/AL18xx diagnostics this SDK auto-injects, organized by domain band.
- **[Per-rule editorconfig profile](src/Config/Analyzer.ANcpLua.Analyzers.editorconfig)** — the severity table shipped alongside the SDK; consumers override individual rules via the standard `dotnet_diagnostic.{ID}.severity = …` form.

## Related

- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) — Custom Roslyn analyzers (auto-injected)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) — Source generator utilities
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) — MAF runtime helpers + agent test infrastructure

---

*Initial architecture inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).*
