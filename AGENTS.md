# AGENTS.md — ANcpLua.NET.Sdk

This file provides guidance to Claude Code (claude.ai/code) and other AI agents when
working with code in this repository. `CLAUDE.md` is a symlink to this file. Per-directory
`CLAUDE.md` files also exist under `src/`, `tools/`, and `tests/` — read them when working
in those areas.

This is an **opinionated MSBuild SDK** (not an application): it ships standardized defaults,
policy enforcement, and analyzer injection that consuming `.csproj`s opt into with
`<Project Sdk="ANcpLua.NET.Sdk">`. The "code" is mostly `.props`/`.targets`/`.editorconfig`.

## Framework conventions

Branch protection, auto-merge, release flow, and the cross-repo bootstrap rules for
the four ANcpLua framework repos are documented once at
[ANcpLua/github-settings-automation](https://github.com/ANcpLua/github-settings-automation).
This file documents conventions specific to *this* repo only.

## Packages produced

| Package                  | Base SDK              | Purpose                                  |
|--------------------------|-----------------------|------------------------------------------|
| `ANcpLua.NET.Sdk`        | Microsoft.NET.Sdk     | Standard .NET libraries / apps           |
| `ANcpLua.NET.Sdk.Web`    | Microsoft.NET.Sdk.Web | ASP.NET Core web projects                |
| `ANcpLua.NET.Sdk.Test`   | Microsoft.NET.Sdk     | Test projects (xUnit v3 on MTP)          |
| `ANcpLua.NET.Sdk.Templates` | —                  | `dotnet new` templates (not an MSBuild SDK) |

The first three are consumed via `msbuild-sdks` in a consumer's `global.json`.

## How this repo builds itself (important)

This repo **does not build with its own SDK** — that would be a bootstrap cycle. The repo's
own projects use plain `Microsoft.NET.Sdk` and only *dogfood* the shipped editorconfigs:
`Directory.Build.props` injects `src/Config/*.editorconfig` via `<EditorConfigFiles>` (see
`EnableEditorConfigDogfooding`). Consequence: **`dotnet build` does not exercise the SDK's
import chain** — the test suite does, by packing the SDK and building throwaway sample
projects against it (see Testing below).

## Build / test / pack commands

```bash
# Build the repo's own projects + tools (dogfoods editorconfigs, uses Microsoft.NET.Sdk)
dotnet build ANcpLua.NET.Sdk.slnx

# Run the SDK behavior tests (xUnit v3 on Microsoft Testing Platform)
dotnet test tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj
#   Filter (MTP syntax — see tests/ANcpLua.Sdk.Tests/CLAUDE.md):
#     --filter-method "*BannedApi*"     --filter-class "*MtpDetectionTests"

# Pack all 4 NuGet packages to artifacts/ (stamps the version into Version.props first)
pwsh ./build.ps1 -Version 3.4.37
#   or a single package: dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release -o artifacts

# Regenerate GENERATED files after changing their inputs (do not hand-edit the outputs):
dotnet run --project tools/SdkGenerator          # -> src/Sdk/<variant>/Sdk.props|targets
dotnet run --project tools/ConfigFilesGenerator  # -> src/Config/Analyzer.*.editorconfig, BannedSymbols.*.txt
```

`global.json` pins .NET SDK `10.0.300` (rollForward `latestFeature`, prerelease allowed) and
sets the test runner to `Microsoft.Testing.Platform`.

## SDK import chain

Per-variant entry points live in `src/Sdk/<variant>/{Sdk.props,Sdk.targets}` and are
**generated** by `tools/SdkGenerator` — they are intentionally minimal; all real logic lives
in `src/Build/`. `Sdk.props` wires the shared logic in via MSBuild's standard extension hooks
rather than direct imports:

```
src/Sdk/<variant>/Sdk.props
  ├─ sets CustomBeforeDirectoryBuildProps += Build/Common/Common.props
  ├─ sets BeforeMicrosoftNETSdkTargets    += Build/Common/Common.targets
  ├─ Import Build/Common/Version.props          # version constants (source of truth)
  ├─ Import Build/Common/GlobalPackages.props   # analyzer injection (GlobalPackageReference)
  ├─ Import Microsoft.NET.Sdk[.Web] Sdk.props   # base SDK
  ├─ Import Build/Enforcement/Enforcement.props # banned-package policy
  └─ Import Build/Enforcement/DeterminismAndSourceLink.props

Build/Common/Common.props   → imports ContinuousIntegrationBuild.props
Build/Common/Common.targets → imports, in order:
      SourceGenerators.targets
      Tests.targets               (only when IsTestProject == true)
      Npm.targets
      ../Enforcement/VersionEnforcement.targets   # AL0018: verifies Version.props was imported
```

The `Common.props`/`Common.targets` files run *because* the base Microsoft SDK honors the
`CustomBeforeDirectoryBuildProps` / `BeforeMicrosoftNETSdkTargets` hooks — they are not
imported directly by `Sdk.props`. (`Common.props` is imported directly only in the rarer
"inner SDK" case where the project already imports `Microsoft.NET.Sdk`.)

## Auto-detected properties

| Property                    | Detection logic |
|-----------------------------|-----------------|
| `IsTestProject`             | `ANcpLuaSdkName == ANcpLua.NET.Sdk.Test`, or name ends in `.Test`/`.Tests`, or an `AncpLuaTest` ProjectCapability |
| `ANcpLuaSingleFileApp`      | `FileBasedProgram == true` (.NET 10+ `#:sdk` file-based program) |
| `_IsSourceGeneratorProject` | `IsRoslynComponent == true`, or lowercased project name contains `analyzer` or `generator`, or `IsSourceGenerator == true` |
| `IsWebProject` / `IsApiProject` | name ends in `.Web`/`.Api`, or an `AncpLuaWeb`/`AncpLuaApi` ProjectCapability |

Capability-based detection (`ProjectCapability` items) is the preferred path; filename
matching is the fallback. See `Directory.Build.targets`.

## Core defaults

Set in `src/Build/Common/Common.props` (for consumers) and mirrored in `Directory.Build.props`
(for this repo's own dogfooding). All use `Condition="'$(X)' == ''"` so consumers can override.

| Property | Value | Notes |
|----------|-------|-------|
| `LangVersion` | `latest` | |
| `Nullable` | `enable` | |
| `ImplicitUsings` | `enable` | |
| `Deterministic` | `true` | reproducible builds |
| `EnableNETAnalyzers` | `true` | |
| `AnalysisLevel` | `latest-all` | |
| `TreatWarningsAsErrors` | `true` | **only** in CI (`ContinuousIntegrationBuild`) or `Release` |
| `EnforceCodeStyleInBuild` | `true` | same CI/Release gate |
| `NuGetAudit` | `true`, mode `all`, level `low` | audit warnings become errors in CI/Release |
| `ManagePackageVersionsCentrally` | `true` | **CPM is mandatory** (see below) |
| `CentralPackageTransitivePinningEnabled` | `true` | |

## Central Package Management is mandatory

Every consuming repo MUST ship a `Directory.Packages.props` (even an empty one with
`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`). Without it, restore
fails with `NU1015` because the SDK injects its analyzers as `GlobalPackageReference`, which
only resolves under CPM. This is the #1 consumer support issue.

## Analyzer injection & banned packages

Analyzers are injected as `GlobalPackageReference` (immutable under CPM) by
`GlobalPackages.props`. Injection is skipped for the analyzer's own package
(`PackageId == ANcpLua.Analyzers`) and has a non-CPM fallback branch.

| Injected analyzer | Scope |
|-------------------|-------|
| `ANcpLua.Analyzers` | all projects (89 AL00xx/AL18xx rules) |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | all projects (`src/Config/BannedSymbols*.txt`) |
| `AwesomeAssertions.Analyzers` | test projects only |

Opt out entirely with `<DisableANcpLuaAnalyzers>true</DisableANcpLuaAnalyzers>`.

Banned packages (enforced in `Enforcement.props`): `PolySharp` (use
`ANcpLua.Roslyn.Utilities.Polyfills`), `FluentAssertions` (license — use `AwesomeAssertions`),
`Microsoft.NET.Test.Sdk` when MTP is enabled, and `DisableTransitiveProjectReferences`
(breaks CPM).

## Test project behavior

When `IsTestProject == true`, `Tests.targets` sets `OutputType=Exe`, injects `xunit.v3.mtp-v2`
+ `AwesomeAssertions` (+ its analyzer), adds global usings `Xunit` and `AwesomeAssertions`, and
configures TRX reporting. It **auto-detects** TUnit/NUnit/MSTest and sets `SkipXunitInjection`
so those frameworks coexist; you can also set `<SkipXunitInjection>true</SkipXunitInjection>`
manually.

## Source-generator Roslyn pin

Projects detected as source generators/analyzers get `Microsoft.CodeAnalysis.CSharp` pinned to
`SourceGeneratorRoslynVersion` (defaults to `$(RoslynVersion)` from `Version.props`) via
`VersionOverride`, keeping CPM intact. This implicit injection is fragile under IDE language
servers (Rider/Roslyn LSP/OmniSharp evaluating `Sdk.props` before `Common.props`); consumers
hitting that can set `<DisableImplicitRoslynPackageReference>true</DisableImplicitRoslynPackageReference>`
and declare the `PackageReference` explicitly. Override the version with
`<SourceGeneratorRoslynVersion>…</SourceGeneratorRoslynVersion>`.

## First-party library references

The SDK does **not** pin or auto-inject `ANcpLua.Roslyn.Utilities`, `ANcpLua.Agents`, or
`ANcpLua.Analyzers` as runtime deps for *consumers* — consumers reference them like any other
NuGet package (use `…Roslyn.Utilities.Sources` for `netstandard2.0` generators,
`…Roslyn.Utilities` for `net10.0+` runtime). Decoupled release cadence is intentional.

**SDK-internal exception (not part of the SDK contract):** this repo's *own* test project is
an ordinary consumer of `ANcpLua.Roslyn.Utilities.Testing`, so its version is pinned in
**`Directory.Packages.props`** — `ANcpLuaRoslynUtilitiesTestingVersion`, in the
`PropertyGroup Label="SDK-internal first-party pins"`. It lives there, **not** in
`Version.props`, on purpose: `Version.props` ships inside the packages and flows to every
consumer, so a self-test-only dependency must stay out of it. The pin exists so the AL analyzer
lint and CPM (which require a version for every `PackageReference`) stay happy.

## Version.props: the source of truth

`src/Build/Common/Version.props` defines **all** package versions used across the SDK
ecosystem and is shipped *inside* the NuGet packages, flowing into every consumer. Notes:

- `ANcpSdkPackageVersion` is a `999.9.9` placeholder; `build.ps1` stamps the real,
  CI-computed version at pack time (do not hard-code it).
- Versions carry inline comments explaining floors/pins (e.g. `YamlDotNet` floored by a
  transitive `Meziantou.Framework.DependencyScanning` constraint) — read them before bumping.

### Cross-repo awareness — was passiert, wenn du Versionen anfasst

Die vier Repos bilden eine Bootstrap-Kette: `Roslyn.Utilities → NET.Sdk → (Analyzers, Agents)`.
Truth-Source für Paket-Versionen ist **`src/Build/Common/Version.props`**, in die SDK-Packages
gepackt und in jedes Consumer-Projekt geladen. Ein lokales `Version.props` (sofern vorhanden)
wird *nach* der SDK-Datei importiert (last-wins) — um lokal AHEAD der publizierten SDK zu pinnen.

Bevor du eine Variable bumpst:

- **Truth fließt durch GlobalPackageReference.** Zeigt Truth auf eine Version, die noch nicht
  auf nuget.org liegt, scheitert *jeder* Restore mit `NU1102` — auch die SDK-eigenen Tests
  (sie packen ein Sample-Projekt und builden es). Reihenfolge: erst das ausgeschriebene Repo
  taggen + auf NuGet bringen, dann Truth nachziehen.
- **Self-Reference zeigt auf last-PUBLISHED.** Eine Variable für das *eigene* Paket des Repos
  muss auf die zuletzt publizierte Version zeigen, nicht auf die hochkommende — beim Restore
  (vor Pack) existiert die neue Version noch nicht; CI stampt sie per `-p:Version=X.Y.Z`.
- **Bumps haben transitive Konsequenzen unter CPM.** Downgrade ist `NU1109` (Hard-Error). Wenn
  ein Bump nicht greift, steht der Grund in der Restore-Fehlermeldung — lesen.
- **Lokales Override gleich/unter Truth ist Müll** (Doppelpflege bzw. stille Regression) —
  prunen, sobald die SDK mit matching Werten publisht.
- **Publish triggert auf Tag-Push `v*`, gegated durch Tests.** Ein Tag auf einen build-broken
  Commit publisht nicht. Statt remote zu re-assignen (≈ Force-Push), nächste Patch-Version nehmen.
- **Verifiziere Versionen vor dem Bump** via
  `https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json`.

## Repository layout

```
src/
  ANcpLua.NET.Sdk[.Web|.Test|.Templates].csproj / .nuspec   # the packaging projects
  Build/
    Common/        # Version.props (truth), Common.props/.targets, GlobalPackages.props,
                   #   SourceGenerators.targets, Tests.targets, Npm.targets, CI detection
    Enforcement/   # Enforcement.props (banned pkgs), Determinism…, VersionEnforcement.targets
  Config/          # FLAT since v3.3.0: Analyzer.*.editorconfig, *.editorconfig style files,
                   #   BannedSymbols*.txt, default.runsettings   (GENERATED — see tools/)
  Sdk/<variant>/   # GENERATED Sdk.props/Sdk.targets entry points (see tools/SdkGenerator)
tests/ANcpLua.Sdk.Tests/   # SDK behavior tests (pack SDK → build sample projects → assert)
tools/             # SdkGenerator, ConfigFilesGenerator (standalone exes, net10.0, not packed)
templates/         # ancplua-app / ancplua-lib / ancplua-web (dotnet new sources)
```

### Testing architecture

Tests build *real* throwaway projects against the packed SDK. `PackageFixture` packs the SDK
once per run; `SdkProjectBuilder.Create(fixture)` spins up an isolated project you configure
fluently and `BuildAsync()`. Tests parametrize over three import styles — `<Project Sdk="…">`,
`<Sdk Name="…"/>`, and SDK-in-`Directory.Build.props` — to prove all wiring paths work.

## Packing & release discipline

- **nuspec `<file>` entries must be enumerated, not globbed.** `<file src="Config/*"/>`
  expanded to zero matches on Windows pack agents (v3.3.0 regression); see `src/*.nuspec`.
- Git tags use a `v` prefix (`v3.4.x`); CI's `gh release create` makes the tag so it matches
  the CLI push convention. Don't re-assign an existing remote tag — use the next patch version.
- The publish gate watches `'*.nuspec' 'src/**/*' 'tests/**/*'`. Editing **only** a workflow or
  docs leaves `has_changes` false by design (the deploy job reports success with zero steps) —
  touch a watched path to force a publish.
