# CLAUDE.md - ANcpLua.Sdk.Canary

Fast SDK validation in <10 seconds - catches packaging errors before waiting 30min for full CI.

## Commands

```bash
# Full validation (build + test)
./canary.sh

# Build only (~5s)
./canary.sh --build

# Test only (requires prior build)
./canary.sh --test

# With diagnostics
./canary.sh --diag

# Generate binlog for MSBuild debugging
./canary.sh --binlog
```

## What It Validates

| Check | Time | Catches |
|-------|------|---------|
| net10.0 build | ~3s | MSB4019, MSB4057, import errors |
| netstandard2.0 build | ~2s | Polyfill compilation failures |
| MTP tests | ~3s | Test host issues, package injection |
| **Total** | **<10s** | SDK packaging errors |

## Test Classes

| Class | Purpose |
|-------|---------|
| SdkStructureTests | SDK .props/.targets import correctly |
| MtpDetectionTests | xunit.v3.mtp-v2 triggers MTP mode |
| LanguageFeatureTests | C# 14 features work |
| ThrowHelperTests | Throw.IfNull injected correctly |
| PolyfillValidation | netstandard2.0 polyfills compile |

## Common Failures

| Error | Cause | Fix |
|-------|-------|-----|
| MSB4019 | Missing .targets | Check SDK package structure |
| MSB4057 | Target not found | Check target names in .targets |
| CS0246 | Type not found | Polyfill not injected |
| Test host failed | OutputType=Library | MTP detection broken |

## Workflow

```bash
# 1. Make SDK changes
vim src/common/Common.targets

# 2. Run canary (~10s)
./tests/ANcpLua.Sdk.Canary/canary.sh

# 3. If passes, run full tests
dotnet test

# 4. If fails, debug with binlog
./tests/ANcpLua.Sdk.Canary/canary.sh --binlog
```
