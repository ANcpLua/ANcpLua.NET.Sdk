# CLAUDE.md - tests/

SDK test projects.

## Structure

```
tests/
└── ANcpLua.Sdk.Tests/    # Full SDK behavior tests (~300 tests)
```

## Commands

```bash
# Full test suite
dotnet test

# Specific test project
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj

# With filter (MTP syntax)
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-method "*BannedApi*"
```

## Test Strategy

| Suite | Time | Purpose |
|-------|------|---------|
| Sdk.Tests | ~2-16min | Full SDK behavior validation |

## Workflow

1. Make SDK changes
2. Run full suite (`dotnet test`)
3. If fails, debug with `--binlog`
