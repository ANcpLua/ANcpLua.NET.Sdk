# ANcpLua.Sdk.Canary

**Fast SDK validation in <10 seconds** - catches packaging errors before waiting 30min for full CI.

## Quick Start

```bash
# From repo root
cd tests/ANcpLua.Sdk.Canary
./canary.sh

# Or from anywhere
./tests/ANcpLua.Sdk.Canary/canary.sh
```

## What It Validates

| Check                | Time     | Catches                             |
|----------------------|----------|-------------------------------------|
| net10.0 build        | ~3s      | MSB4019, MSB4057, import errors     |
| netstandard2.0 build | ~2s      | Polyfill compilation failures       |
| MTP tests            | ~3s      | Test host issues, package injection |
| **Total**            | **<10s** | SDK packaging errors                |

## Usage

```bash
# Full validation (build + test)
./canary.sh

# Build only (fastest - ~5s)
./canary.sh --build

# Test only (requires prior build)
./canary.sh --test

# With diagnostics (debug SDK detection)
./canary.sh --diag

# Generate binlog for MSBuild debugging
./canary.sh --binlog
```

## Windows (PowerShell)

```powershell
./canary.ps1
./canary.ps1 -BuildOnly
./canary.ps1 -Diagnostics
```

## What Each Test Validates

### SdkStructureTests

- `Sdk_Props_Imported` - SDK .props files import without errors
- `Sdk_Targets_Imported` - SDK .targets files import without errors
- `MTP_OutputType_IsExe` - MTP detection sets OutputType=Exe

### MtpDetectionTests

- `XunitV3MtpV2_Detected` - xunit.v3.mtp-v2 triggers MTP mode
- `AwesomeAssertions_Available` - Package injection works

### LanguageFeatureTests

- `CSharp14_PrimaryConstructors` - LangVersion is correct
- `CSharp14_CollectionExpressions` - Modern syntax works
- `CSharp14_PatternMatching` - Pattern matching works

### ThrowHelperTests

- `Throw_IfNull_Works` - Throw helpers are injected
- `Throw_IfNull_ThrowsOnNull` - Throw helpers work correctly
- `Throw_IfNullOrEmpty_Works` - String helpers available

### PolyfillValidation (netstandard2.0 - compile only)

- Index/Range syntax (`arr[^1]`, `arr[1..3]`)
- Nullable attributes (`NotNullWhen`, `NotNull`)
- `CallerArgumentExpression`
- Init-only properties
- Required members
- `StackTraceHidden` attribute

## CI Integration

Add to your workflow before the full test run:

```yaml
jobs:
  canary:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: SDK Canary
        run: ./tests/ANcpLua.Sdk.Canary/canary.sh

  full-tests:
    needs: canary  # Only run if canary passes
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test  # Full 30min test suite
```

## When Canary Fails

| Error            | Cause                   | Fix                             |
|------------------|-------------------------|---------------------------------|
| MSB4019          | Missing .targets import | Check SDK package structure     |
| MSB4057          | Target not found        | Check target names in .targets  |
| CS0246           | Type not found          | Polyfill not injected correctly |
| Test host failed | OutputType=Library      | MTP detection broken            |
| NU1xxx           | Package resolution      | Check Directory.Packages.props  |

## Local Development Workflow

```bash
# 1. Make SDK changes
vim src/common/Common.targets

# 2. Run canary (~10s)
./tests/ANcpLua.Sdk.Canary/canary.sh

# 3. If canary passes, run full tests
dotnet test

# 4. If canary fails, debug with binlog
./tests/ANcpLua.Sdk.Canary/canary.sh --binlog
# Open canary.binlog with MSBuild Structured Log Viewer
```