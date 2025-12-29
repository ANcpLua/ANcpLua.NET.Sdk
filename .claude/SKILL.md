---
name: dotnet-test-mtp
description: |
  Microsoft Testing Platform (MTP) CLI reference for .NET test projects. Use when:
  (1) Test errors show "Unknown option '--filter'" or "Unknown option '--logger'"
  (2) Exit code 5 from test runner (invalid arguments)
  (3) User asks about dotnet test filtering with xUnit v3, MSTest, NUnit, or TUnit
  (4) Zero tests ran with MTP test projects
  (5) Questions about --filter-query, --filter-class, --filter-method syntax
  (6) VSTest options not working with modern test frameworks
---

# .NET Testing Platform (MTP) CLI Reference

## Critical: VSTest vs MTP Syntax

**VSTest options DO NOT work with MTP.** This is the #1 cause of test CLI failures.

```bash
# ❌ VSTest syntax - FAILS with MTP
dotnet test --filter "FullyQualifiedName~MyTest"
dotnet test --logger "trx"

# ✅ MTP syntax - works
dotnet test --filter-method "*MyTest*"
dotnet test --report-trx
```

## SDK Version Determines Argument Passing

| SDK | MTP Argument Style |
|-----|-------------------|
| .NET 8/9 | `dotnet test -- --filter-class X` (args after `--`) |
| .NET 10+ | `dotnet test --filter-class X` (native, no `--`) |

### Enabling MTP

**.NET 8/9** — Add to csproj:
```xml
<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
```

**.NET 10+** — Add to `global.json`:
```json
{ "test": { "runner": "Microsoft.Testing.Platform" } }
```

## xUnit v3 MTP Filter Options

xUnit v3 with MTP has **its own filter syntax**, different from VSTest AND generic MTP.

```bash
# Filter by class
dotnet test --filter-class "MyNamespace.MyTestClass"

# Filter by method (wildcards supported)
dotnet test --filter-method "*ShouldReturnTrue*"

# Filter by namespace
dotnet test --filter-namespace "MyNamespace.Tests"

# Filter by trait
dotnet test --filter-trait "Category=Unit"

# Exclude patterns (prefix with not-)
dotnet test --filter-not-class "IntegrationTests"
```

### xUnit v3 Query Filter Language

Advanced filtering with path-based queries:

```bash
# Syntax: /<assembly>/<namespace>/<class>/<method>
dotnet test --filter-query "/*/*/*/MyTest*"
dotnet test --filter-query "/MyAssembly/*/*/Test*"
dotnet test --filter-query "/**[Category=Fast]"
```

| Pattern | Meaning |
|---------|---------|
| `*` | Match all at this level |
| `**` | Match all descendants |
| `Name*` | Starts with |
| `*Name` | Ends with |
| `*Name*` | Contains |
| `[Key=Value]` | Property filter |

## MSTest / NUnit with MTP

MSTest and NUnit **do** support VSTest-style `--filter` syntax even with MTP:

```bash
# Works with MSTest/NUnit on MTP
dotnet test -- --filter "FullyQualifiedName~MyTest"
```

## Report Generation

| VSTest | MTP (xUnit v3) |
|--------|----------------|
| `--logger "trx"` | `--report-xunit-trx` |
| `--logger "html"` | `--report-xunit-html` |
| `--logger "junit"` | `--report-junit` |

## xUnit v3 Package Variants

| Package | MTP Version |
|---------|-------------|
| `xunit.v3` | MTP v1 (default) |
| `xunit.v3.mtp-v1` | Explicit MTP v1 |
| `xunit.v3.mtp-v2` | Explicit MTP v2 |
| `xunit.v3.mtp-off` | VSTest only (no MTP) |

## Troubleshooting

### "Unknown option '--filter'" or Exit Code 5
The test project uses MTP but received VSTest arguments. Use MTP syntax above.

### "Zero tests ran"
1. Check filter syntax matches framework (xUnit v3 ≠ MSTest)
2. Verify `--` separator for .NET 8/9
3. Try `--filter-query "/**"` to run all tests

### Force VSTest Mode (Workaround)
```bash
dotnet test -p:TestingPlatformDotnetTestSupport=false --filter "..."
```
