<#
.SYNOPSIS
    Fast SDK validation (<10 seconds)

.DESCRIPTION
    Validates SDK packaging is correct before running full CI (30min).

.PARAMETER BuildOnly
    Build only, skip tests (fastest)

.PARAMETER TestOnly
    Test only, requires prior build

.PARAMETER Diagnostics
    Enable ANcpLua diagnostics output

.PARAMETER BinLog
    Generate MSBuild binlog for debugging

.EXAMPLE
    ./canary.ps1              # Quick validation
    ./canary.ps1 -BuildOnly   # Build only
    ./canary.ps1 -Diagnostics # With debug output
#>

param(
    [switch]$BuildOnly,
    [switch]$TestOnly,
    [switch]$Diagnostics,
    [switch]$BinLog
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $ScriptDir "ANcpLua.Sdk.Canary.csproj"
$StartTime = Get-Date

function Write-Header
{
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " ANcpLua.NET.Sdk Canary - Fast Validation" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success($msg)
{
    Write-Host "✓ $msg" -ForegroundColor Green
}

function Write-Failure($msg)
{
    Write-Host "✗ $msg" -ForegroundColor Red
    exit 1
}

function Write-Duration
{
    $Duration = (Get-Date) - $StartTime
    Write-Host ""
    Write-Success "Completed in $([math]::Round($Duration.TotalSeconds) )s"
}

Write-Header

# Build extra args
$BuildArgs = @("-c", "Release", "-v:q", "--nologo")
if ($Diagnostics)
{
    $BuildArgs += "-p:ANcpLuaDiagnostics=true"
}
if ($BinLog)
{
    $BuildArgs += "-bl:canary.binlog"
}

# Build phase
if (-not $TestOnly)
{
    Write-Host "► Building canary project..."

    # Build net10.0
    Write-Host "  → net10.0"
    $result = dotnet build $Project -f net10.0 @BuildArgs 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host $result
        Write-Failure "net10.0 build failed"
    }

    # Build netstandard2.0
    Write-Host "  → netstandard2.0"
    $result = dotnet build $Project -f netstandard2.0 @BuildArgs 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host $result
        Write-Failure "netstandard2.0 build failed (polyfill issue)"
    }

    Write-Success "Build passed"
}

# Test phase
if (-not $BuildOnly)
{
    Write-Host ""
    Write-Host "► Running canary tests..."

    $result = dotnet test $Project -f net10.0 --no-build -c Release --nologo -v:q 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host $result
        Write-Failure "Tests failed"
    }

    Write-Success "Tests passed"
}

Write-Duration

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host " SDK CANARY PASSED - Safe to run full CI" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""