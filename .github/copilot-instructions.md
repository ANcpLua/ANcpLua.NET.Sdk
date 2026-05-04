# Copilot PR-review instructions for ANcpLua.NET.Sdk

Opinionated MSBuild SDK shipping `.props`/`.targets` (not binaries) as three NuGet
packages: `ANcpLua.NET.Sdk` (Library/Console default), `.Web`, `.Test`. SDK targets
`netstandard2.0`; consumer projects default to `net10.0`. Centralized package
management is enforced (`ManagePackageVersionsCentrally=true`). This file scopes
to PR review only.

## Flag

- New `<NoWarn>X</NoWarn>` added to one of the three SDK csproj files but not the
  other two — cross-cutting suppressions belong in `Common.props`. Per-SDK divergence
  here causes silent consumer drift.
- Mismatch between `.csproj` metadata and the matching hand-crafted `.nuspec`
  (version, dependencies, description). Packages use `NuSpecFile` +
  `IncludeBuildOutput=false`; csproj metadata is silently ignored at pack time.
- Imports of `Common.props` outside the `Sdk.props` wrapper, or any change to the
  `Sdk.props` → `Microsoft.NET.Sdk` → `Common.props` → `Enforcement.props` import
  order. The `_MustImportMicrosoftNETSdk` guard exists for this reason.
- Use of banned APIs the SDK enforces via `BannedSymbols.txt`: `DateTime.Now/UtcNow`,
  `DateTimeOffset.Now/UtcNow`, `ArgumentNullException.ThrowIfNull` (use
  `Guard.NotNull` from `ANcpLua.Roslyn.Utilities`), `File/Directory.GetCreationTime`
  (use `Utc` variants), `StringComparison.InvariantCulture` (use `Ordinal`),
  `System.Tuple` (use `ValueTuple`).
- Hardcoded paths in `.props`/`.targets` — must use `$(MSBuildThisFileDirectory)`,
  never absolute paths.
- New build dependencies on `PolySharp`, `Microsoft.NET.Test.Sdk` (MTP replaces it),
  or `FluentAssertions` — `Enforcement.props` blocks these by design.

## sdk-specific

- The SDK ships `.props`/`.targets` only. PRs adding C# code under `src/` need to
  go into `IncludeBuildOutput=false`-friendly shape (i.e. authored as test or
  internal scaffolding, not pack-included assemblies).
- `Common.props` reads the consumer `.csproj` to detect explicit `TargetFramework`
  *before* defaulting it. Don't reorder that probe.
- `ProjectCapability` + filename-suffix detection (`.Web`, `.Api`, `.Test`)
  auto-routes which sub-SDK is loaded. Renaming a project may silently change the
  applied policy.
- `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` only fire when
  `ContinuousIntegrationBuild=true` OR `Configuration=Release`. Local Debug builds
  are intentionally lenient.

## Do not flag

- Allow-listed suppressions: `NU5128` (NuGet packing) on `.Web`/`.Test`,
  `NU5100` (reserved/unused dependency) on the main package, `CS1573`/`CS1591`/`CA1014`
  in `Common.props` (XML doc + assembly attribute, repo-wide silenced).
- Test patterns: xUnit v3 with Microsoft Testing Platform (MTP). Do not suggest
  legacy xUnit v2 / `Microsoft.NET.Test.Sdk` patterns.
- `AnalysisLevel=latest-all`, `AllowUnsafeBlocks=true`, `Deterministic=true` are
  intentional repo defaults, not anti-patterns.

## Project context

Solo-dev repo with downstream consumers under owner control. Breaking changes are
allowed; bump major in the same session and fix consumers in the same session.
Don't suggest backwards-compat shims or dual SDK versions. The SDK injects
`ANcpLua.Analyzers` on consume — that's the source of truth for code-quality rules
in downstream projects, not this repo's own conventions.
