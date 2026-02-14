# CLAUDE.md - eng/

Build engineering infrastructure for projects consuming ANcpLua.NET.Sdk without CPM.

## Files

| File                  | Purpose                                                |
|-----------------------|--------------------------------------------------------|
| `Directory.Build.props` | Imports Version.props for non-CPM projects            |

## When eng/ Is Used

Projects that set `ManagePackageVersionsCentrally=false` cannot use Directory.Packages.props
to import Version.props. Instead, `eng/Directory.Build.props` imports it directly.

## Polyfill and Extension Injection

All injectable source files live under `src/shared/`:

```
src/shared/
  Polyfills/          # BCL polyfills for older TFMs
  Extensions/         # Utility extensions (Comparers, SourceGen, FakeLogger)
  Throw/              # Guard clause utilities
```

Injection is controlled by `src/Build/Common/LegacySupport.props` (property defaults)
and `src/Build/Common/LegacySupport.targets` (conditional Compile includes).

## Analyzer Injection

Analyzers are injected via `src/Build/Common/GlobalPackages.props` using
`GlobalPackageReference` (immutable when CPM enabled) or `PackageReference` fallback.
