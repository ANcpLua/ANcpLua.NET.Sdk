# AGENTS.md — ANcpLua.NET.Sdk

Opinionated MSBuild SDK providing standardized defaults, policy enforcement, and analyzer injection for .NET projects. `CLAUDE.md` symlinks here.

## SDK Variants

| Package              | Base SDK              | Purpose                            |
|----------------------|-----------------------|------------------------------------|
| ANcpLua.NET.Sdk      | Microsoft.NET.Sdk     | Standard .NET libraries/apps       |
| ANcpLua.NET.Sdk.Web  | Microsoft.NET.Sdk.Web | ASP.NET Core web projects          |
| ANcpLua.NET.Sdk.Test | Microsoft.NET.Sdk     | Test projects (xUnit v3 MTP)       |

## Import Chain

```
Sdk.props
    |
    +-> Version.props              # Package version constants (source of truth)
    +-> GlobalPackages.props       # GlobalPackageReference (analyzers, SBOM)
    +-> Microsoft.NET.Sdk[.Web]    # Base SDK import
    +-> Enforcement.props          # Policy enforcement
    +-> DeterminismAndSourceLink.props  # Reproducible builds

Common.props  (via CustomBeforeDirectoryBuildProps)
    |
    +-> ContinuousIntegrationBuild.props  # CI detection

Common.targets  (via BeforeMicrosoftNETSdkTargets)
    |
    +-> SourceGenerators.targets    # Auto-pin RoslynVersion for generators
    +-> Tests.targets               # Test framework injection (IsTestProject)
    +-> Npm.targets                 # Opt-in npm integration
    +-> VersionEnforcement.targets  # AL0018 import check
```

## Auto-Detected Properties

| Property                   | Detection Logic                                                          |
|----------------------------|--------------------------------------------------------------------------|
| `IsTestProject`            | `ANcpLuaSdkName == ANcpLua.NET.Sdk.Test`, or root Directory.Build.targets name match |
| `ANcpLuaSingleFileApp`     | `FileBasedProgram == true` (.NET 10+ `#:sdk` directive)                   |
| `_IsSourceGeneratorProject` | Project name contains `generator` or `analyzer` (case-insensitive), or `IsRoslynComponent=true`, or `IsSourceGenerator=true` |

## Core Defaults (Common.props)

| Property                              | Value        | Notes                                    |
|---------------------------------------|--------------|------------------------------------------|
| `LangVersion`                         | `latest`     | Always use latest C# features            |
| `Nullable`                            | `enable`     | NRTs enabled by default                  |
| `ImplicitUsings`                      | `enable`     | Global usings enabled                    |
| `Deterministic`                       | `true`       | Reproducible builds                      |
| `EnableNETAnalyzers`                  | `true`       | .NET analyzers enabled                   |
| `AnalysisLevel`                       | `latest-all` | All analysis rules                       |
| `TreatWarningsAsErrors`               | `true`       | In CI or Release builds                  |
| `ManagePackageVersionsCentrally`      | `true`       | CPM required                             |
| `CentralPackageTransitivePinningEnabled` | `true`    | Transitive pinning enabled               |

## Banned Packages (Policy Enforcement)

| Package                  | Reason                                     | Alternative                          |
|--------------------------|--------------------------------------------|--------------------------------------|
| `PolySharp`              | Polyfills in separate package              | Use `ANcpLua.Roslyn.Utilities.Polyfills` |
| `FluentAssertions`       | License concerns                           | Use `AwesomeAssertions`              |
| `Microsoft.NET.Test.Sdk` | Only when MTP enabled                      | MTP doesn't need VSTest              |
| `DisableTransitiveProjectReferences` | Breaks CPM          | Use `CentralPackageTransitivePinningEnabled` |

## Analyzer Injection

Analyzers are injected via `GlobalPackageReference` (immutable when CPM enabled):

| Analyzer                               | Purpose                          |
|----------------------------------------|----------------------------------|
| `ANcpLua.Analyzers`                    | Custom code quality rules        |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | Banned API enforcement       |
| `AwesomeAssertions.Analyzers`          | Test assertion best practices (test projects only) |

Opt-out: `<DisableANcpLuaAnalyzers>true</DisableANcpLuaAnalyzers>`

## Test Project Configuration

When `IsTestProject=true`, the SDK automatically:

1. Sets `OutputType=Exe` (for MTP)
2. Injects `xunit.v3.mtp-v2`
3. Injects `AwesomeAssertions` and its analyzer
4. Adds global usings: `Xunit`, `AwesomeAssertions`
5. Configures TRX reporting: `--report-xunit-trx`

Opt-out of xUnit injection (for TUnit/NUnit/MSTest):
```xml
<PropertyGroup>
  <SkipXunitInjection>true</SkipXunitInjection>
</PropertyGroup>
```

## Source Generator Roslyn Pin

Projects whose name contains `generator` or `analyzer` get
`Microsoft.CodeAnalysis.CSharp` pinned to `SourceGeneratorRoslynVersion` (default
`$(RoslynVersion)` from `Version.props`) via `VersionOverride`, keeping CPM
enabled. Override the version:

```xml
<PropertyGroup>
  <SourceGeneratorRoslynVersion>4.11.0</SourceGeneratorRoslynVersion>
</PropertyGroup>
```

### Opt-out: self-describe the Roslyn reference (SDK 3.2.0+)

The implicit injection is fragile under IDE language servers (Rider, Roslyn LSP,
OmniSharp): if the LSP evaluates `Sdk.props` before `Common.props` imports take
effect, `Microsoft.CodeAnalysis.*` disappears from the compilation even though
`dotnet build` succeeds. Consumers hitting this can opt out of the implicit
injection and declare the reference explicitly:

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <DisableImplicitRoslynPackageReference>true</DisableImplicitRoslynPackageReference>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                      VersionOverride="$(SourceGeneratorRoslynVersion)"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
```

The opt-out flag is a no-op on SDK <= 3.1.0 (which always injects the reference);
consumers on older SDKs that need the opt-out must either upgrade to 3.2.0+ or
use `<PackageReference Remove="Microsoft.CodeAnalysis.CSharp"/>` as a bridge
before their explicit `Include`.

## First-Party Library References

The SDK does NOT pin or auto-inject `ANcpLua.Roslyn.Utilities`, `ANcpLua.Agents`,
or `ANcpLua.Analyzers`. Consumers reference them like any other NuGet dep.

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="ANcpLua.Roslyn.Utilities.Sources" Version="$(ANcpLuaRoslynUtilitiesVersion)"/>

<!-- consumer csproj (netstandard2.0) -->
<PackageReference Include="ANcpLua.Roslyn.Utilities.Sources" PrivateAssets="all"/>
```

For runtime consumers (net10.0+) use `ANcpLua.Roslyn.Utilities` instead of `.Sources`.

## Consumer Usage

### global.json

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "<version>",
    "ANcpLua.NET.Sdk.Web": "<version>",
    "ANcpLua.NET.Sdk.Test": "<version>"
  }
}
```

### Project File

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Build Commands

```bash
# Build
dotnet build

# Test
dotnet test
```

## Version.props: Source of Truth

`src/Build/Common/Version.props` defines ALL package versions used across the SDK
ecosystem.

## Cross-Repo Awareness — was passiert, wenn du Versionen anfasst

Diese vier Repos bilden eine Bootstrap-Kette: `Roslyn.Utilities → NET.Sdk → (Analyzers, Agents)`. Truth-Source für Paket-Versionen ist **`ANcpLua.NET.Sdk/src/Build/Common/Version.props`**, in den SDK-NuGet-Packages gepackt und in jedes Consumer-Projekt geladen. Dein lokales `Version.props` (sofern vorhanden) wird *nach* der SDK-Datei importiert (last-wins) — gedacht, um lokal AHEAD der gerade-publizierten SDK zu pinnen.

Bevor du eine Variable in Truth oder im lokalen Override bumpst:

- **Truth fließt durch GlobalPackageReference.** Pakete wie `ANcpLua.Analyzers` werden von der SDK in *jedes* Consumer-Projekt injiziert. Wenn Truth auf eine Version zeigt, die noch nicht auf nuget.org liegt, scheitert jeder Restore mit `NU1102` — auch die SDK-eigenen Tests (sie packen ein Sample.csproj und builden es). Saubere Reihenfolge: zuerst das ausgeschriebene Repo taggen + auf NuGet bringen, dann Truth nachziehen.

- **Self-Reference: die eigene Paket-Version zeigt auf last-PUBLISHED.** Wenn ein lokales `Version.props` eine Variable für das *eigene* Paket des Repos hat (z.B. `ANcpLuaAnalyzersVersion` in `ANcpLua.Analyzers/Version.props`), muss sie auf die zuletzt-publizierte Version zeigen, nicht auf die hochzukommende. csproj/Tests-Files referenzieren das Paket via `PackageReference` und ziehen es beim Restore aus NuGet; während Restore (vor Pack) gibt's die hochzukommende Version noch nicht. CI stampt die neue Version per `-p:Version=X.Y.Z` erst zur Pack-Time.

- **Bumps haben transitive Konsequenzen unter CPM.** Z.B. `Meziantou.Framework.DependencyScanning 2.0.11` zieht `YamlDotNet ≥ 17.0.1`. Bei `ManagePackageVersionsCentrally=true` ist Downgrade ein Hard-Error (`NU1109`). Wenn ein Bump nicht greift, steht der Grund in der Restore-Fehlermeldung — vor dem nächsten Versuch lesen.

- **Lokales Override gleich/unter Truth ist Müll.** Gleich = Doppelpflege, unter = stille Regression. Pruning sinnvoll, sobald die SDK mit matching Werten publisht.

- **Publish triggert auf Tag-Push `v*`, gegated durch Tests.** Ein Tag auf einen build-broken Commit publisht nicht, bleibt aber als Ghost-Tag remote. Statt remote zu re-assignen (≈ Force-Push), nächste Patch-Version verwenden.

- **Verifiziere Versionen vor dem Bump.** Ein Tippfehler (`2.0.20` statt `2.0.11`) bricht die Topo-Kette, weil Truth in alle Konsumenten fließt. NuGet-API: `https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json`.

## Directory Structure

```
src/
  Build/
    Common/                          # Core props/targets
    Enforcement/                     # Policy, determinism, version checks
  Config/                            # Flat since v3.3.0 — was nested
    Analyzer.*.editorconfig          # per-analyzer rule tuning
    BannedSymbols.txt                # BannedApiAnalyzers lists
    BannedSymbols.NewtonsoftJson.txt
    ANcpLua.NET.Sdk.*.editorconfig   # SDK-variant-specific style
    Global.editorconfig              # SDK global (is_global=true)
    CodingStyle.editorconfig
    NamingConvention.editorconfig
    Compiler.editorconfig
    GeneratedFiles.editorconfig
    default.runsettings
  Sdk/                               # SDK entry points per variant
    ANcpLua.NET.Sdk/
    ANcpLua.NET.Sdk.Test/
    ANcpLua.NET.Sdk.Web/
tests/
  ANcpLua.Sdk.Tests/                 # SDK behavior tests
tools/
  SdkGenerator/                      # Generates Sdk.props/Sdk.targets
  ConfigFilesGenerator/              # Generates Config/*.editorconfig
```

**Packing discipline**: nuspec `<file>` entries must be **enumerated**, not
globbed (`<file src="Config/*"/>` expanded to zero matches under Windows pack
agents in v3.3.0 — see `src/*.nuspec` for the enumerated pattern as of v3.3.1).

## Release workflow notes

- Git tags use `v`-prefix (`v3.3.1`). CI's `gh release create` creates the tag
  with `v` prefix so it matches the human/CLI push convention — no more duplicate
  bare `3.x.x` tags. Historical duplicates before v3.3.1 (`v3.1.0` vs `3.1.0`,
  `v3.2.0` vs `3.2.0`) are left in place; only the workflow going forward is fixed.
- `Must Publish Packages` gate checks `'*.nuspec' 'src/**/*' 'tests/**/*'`. Pre-3.3.1
  runs omitted `tests/**/*`, so test-only fixes silently skipped publish (deploy
  job still reported "success" with zero steps executed). If you edit ONLY a
  workflow file or docs, `has_changes` goes false by design — touch a file under
  one of the watched paths to force a publish.

## ANcpLua Ecosystem

| Repo | Purpose | NuGet | CI checks required |
|---|---|---|---|
| [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) | Opinionated MSBuild SDK — standardized defaults, policy enforcement, analyzer injection | [nuget.org](https://www.nuget.org/packages/ANcpLua.NET.Sdk) | `compute_version`, `lint_config`, `test (ubuntu/windows/macos)`, `create_nuget` |
| [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) | Custom Roslyn analyzers (auto-injected by the SDK) | [nuget.org](https://www.nuget.org/packages/ANcpLua.Analyzers) | `build`, `test (ubuntu/windows/macos)` |
| [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) | Source generator utilities, TryParse extensions, polyfills | [nuget.org](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities) | `build (ubuntu/windows)`, `version` |
| [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) | MAF runtime helpers + agent test infrastructure | [nuget.org](https://www.nuget.org/packages/ANcpLua.Agents) | `build (ubuntu/windows/macos)`, `version` |

### Branch protection (all 4 repos)

- PR required to merge into `main` (0 approvals, squash preferred)
- Required status checks must pass (CI jobs listed above)
- Branch must be up-to-date with `main` before merge
- Force push and branch deletion blocked on `main`
- Optional checks (CodeRabbit, GitGuardian, Copilot review, auto-merge) do not block merges

### Dependency graph

```
ANcpLua.NET.Sdk
  ├── injects ANcpLua.Analyzers (compile-time)
  └── ships Version.props (version truth for all consumers)

ANcpLua.Analyzers
  └── consumes ANcpLua.Roslyn.Utilities.Sources (source-only, internal)

ANcpLua.Roslyn.Utilities
  └── standalone (no first-party deps)

ANcpLua.Agents
  └── standalone (no first-party deps)
```

### Release flow

The four repos use two different patterns. Don't assume what works in one applies to the others.

**This repo (ANcpLua.NET.Sdk) — auto-bump-on-merge:**

1. PR to `main` — CI runs, auto-merge bots handle dep bumps
2. On merge, the `publish` workflow:
   - `compute_version` reads the latest `v*` tag and bumps the patch (`v3.4.14` → `3.4.15`); reuses the tag's version when HEAD is exactly the tag
   - `Must Publish Packages` gate fires only if `*.nuspec`, `src/**/*`, or `tests/**/*` changed (docs-only PRs skip deploy)
   - `deploy` job pushes packages to NuGet via trusted publishing, then `gh release create v$VERSION` creates the GitHub release **and the tag** in one step
3. NuGet indexes in ~4-8 minutes — downstream repos pick up via Renovate

To force a minor/major bump instead of auto-patch, push the tag manually before the workflow runs (compute_version honors the tag if HEAD points at it).

**ANcpLua.Roslyn.Utilities, ANcpLua.Agents — manual-tag-triggers-publish:**

1. PR to `main` — CI runs (build + test only on push, no publish; `publish` job gated by `is_release=true`)
2. After merge, push a tag manually: `git tag vX.Y.Z && git push --tags`
3. Tag push triggers the workflow's release path: version comes from `${GITHUB_REF_NAME#v}`, packages publish, `gh release create` creates the GitHub release

**ANcpLua.Analyzers — manual-tag-publish-only:**

Same as Roslyn.Utilities/Agents on the trigger side (workflow runs only on `push: tags v*` or `workflow_dispatch`), but the workflow does **not** call `gh release create` — only the NuGet push. The tag itself is the release marker; if you want a GitHub release entry, create it manually after.
