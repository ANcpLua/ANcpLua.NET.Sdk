# CLAUDE.md - tests/

SDK test projects.

## Structure

```
tests/
├── ANcpLua.Sdk.Tests/    # Full SDK behavior tests (~100 tests)
└── ANcpLua.Sdk.Canary/   # Fast smoke tests (<10s)
```

## Commands

```bash
# Full test suite
dotnet test

# Canary only (fast validation)
./tests/ANcpLua.Sdk.Canary/canary.sh

# Specific test project
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj
```

## Test Strategy

| Suite | Time | Purpose |
|-------|------|---------|
| Canary | <10s | Catch SDK packaging errors early |
| Sdk.Tests | ~2min | Full SDK behavior validation |

## Workflow

1. Make SDK changes
2. Run canary (`./tests/ANcpLua.Sdk.Canary/canary.sh`)
3. If canary passes, run full suite (`dotnet test`)
4. If canary fails, debug with `--binlog`
