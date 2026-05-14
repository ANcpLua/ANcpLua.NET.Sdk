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
    "ANcpLua.NET.Sdk.Test": "3.4.31",
    "ANcpLua.NET.Sdk.BitNet": "3.4.31"
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

<!-- BitNet hosting (auto-injects ANcpLua.Agents.Hosting.BitNet, pinned channel) -->
<Project Sdk="ANcpLua.NET.Sdk.BitNet"></Project>
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

### BitNet variant — what it adds

`ANcpLua.NET.Sdk.BitNet` is `ANcpLua.NET.Sdk.Web` (it imports the Web SDK transitively) plus an
implicit `<PackageReference Include="ANcpLua.Agents.Hosting.BitNet" Version="<pinned>" />`. The
pinned version lives in the SDK's `Version.props` and ships in lockstep with releases of
[ANcpLua/ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents). Consumers get the keyed
`IChatClient` registration, the auto-OTel decoration, and the bundled source generator with zero
additional ceremony — call `builder.AddQylBitNetChatClient()` in `Program.cs`. See the
[BitNet hosting README](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.BitNet/#readme-body-tab)
for the four wiring modes (zero-config, named connection, programmatic, source-generator).

## Documentation

**[ancplua.mintlify.app](https://ancplua.mintlify.app/)**

## Related

- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) — Custom Roslyn analyzers (auto-injected)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) — Source generator utilities
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) — MAF runtime helpers + agent test infrastructure

---

*Initial architecture inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).*
