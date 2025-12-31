#!/usr/bin/env pwsh
# Build script for CI

param(
    [string]$Version = "1.0.0",
    [Parameter(ValueFromRemainingArguments=$true)]$RemainingArgs
)

$ErrorActionPreference = "Stop"

# =============================================================================
# SUBMODULE AUTO-SYNC - Keep Roslyn.Utilities up to date automatically
# =============================================================================
$SubmodulePath = "eng/submodules/Roslyn.Utilities"
if (Test-Path $SubmodulePath) {
    Write-Host "Checking Roslyn.Utilities submodule..." -ForegroundColor Cyan

    # Fetch latest from remote
    git -C $SubmodulePath fetch origin main --quiet 2>$null

    $LocalCommit = git -C $SubmodulePath rev-parse HEAD
    $RemoteCommit = git -C $SubmodulePath rev-parse origin/main 2>$null

    if ($RemoteCommit -and $LocalCommit -ne $RemoteCommit) {
        Write-Host "Submodule is behind - auto-syncing..." -ForegroundColor Yellow
        git submodule update --remote $SubmodulePath
        Write-Host "Submodule updated to latest" -ForegroundColor Green
    } else {
        Write-Host "Submodule is up to date" -ForegroundColor Green
    }
}

# =============================================================================
# TRANSFORM ROSLYN.UTILITIES - Regenerate embedded source files
# =============================================================================
$TransformScript = "eng/scripts/Transform-RoslynUtilities.ps1"
if (Test-Path $TransformScript) {
    Write-Host "Transforming Roslyn.Utilities source..." -ForegroundColor Cyan
    & $TransformScript
}

# =============================================================================
# VERSION INFO
# =============================================================================
Write-Host "Building version: $Version" -ForegroundColor Cyan

# Generate Version.props with the package version and centralized dependency versions
$VersionPropsPath = "src/common/Version.props"
$VersionPropsContent = @"
<Project>
  <!--
    This file is auto-generated during build.
    DO NOT EDIT MANUALLY - changes will be overwritten.

    The version is set by build.ps1 based on the computed package version.
    This ensures all SDK packages reference the same version.
  -->
  <PropertyGroup>
    <ANcpSdkPackageVersion>$Version</ANcpSdkPackageVersion>
  </PropertyGroup>

  <!--
    Centralized package versions for SDK-injected dependencies.
    Single source of truth - change once, propagate everywhere.
  -->
  <PropertyGroup Label="MTP Extensions">
    <MTPExtensionsVersion>2.0.2</MTPExtensionsVersion>
    <CodeCoverageVersion>18.1.0</CodeCoverageVersion>
    <TestSdkVersion>18.0.1</TestSdkVersion>
    <DiagnosticsTestingVersion>10.0.0</DiagnosticsTestingVersion>
    <GitHubActionsLoggerMTPVersion>3.0.1</GitHubActionsLoggerMTPVersion>
    <GitHubActionsLoggerVSTestVersion>2.4.1</GitHubActionsLoggerVSTestVersion>
  </PropertyGroup>

  <PropertyGroup Label="Analyzers">
    <ANcpLuaAnalyzersVersion>1.0.4</ANcpLuaAnalyzersVersion>
    <SbomTargetsVersion>4.1.5</SbomTargetsVersion>
    <BannedApiAnalyzersVersion>4.14.0</BannedApiAnalyzersVersion>
  </PropertyGroup>

  <PropertyGroup Label="Legacy Polyfills">
    <BclAsyncInterfacesVersion>6.0.0</BclAsyncInterfacesVersion>
    <TasksExtensionsVersion>4.5.4</TasksExtensionsVersion>
  </PropertyGroup>

  <PropertyGroup Label="Test Packages">
    <XunitMtpVersion>3.2.1</XunitMtpVersion>
    <ParallelTestFrameworkVersion>1.0.6</ParallelTestFrameworkVersion>
    <AwesomeAssertionsVersion>9.3.0</AwesomeAssertionsVersion>
    <AwesomeAssertionsAnalyzersVersion>9.0.8</AwesomeAssertionsAnalyzersVersion>
    <MvcTestingVersion>10.0.1</MvcTestingVersion>
  </PropertyGroup>
</Project>
"@
Set-Content -Path $VersionPropsPath -Value $VersionPropsContent -Encoding UTF8
Write-Host "Generated $VersionPropsPath with version $Version"

# ANcpLua.Analyzers now comes from NuGet package (no local build needed)

# Clean old .nupkg files (keep bin/obj for incremental builds)
Get-ChildItem artifacts/*.nupkg -ErrorAction SilentlyContinue | Remove-Item -Force
Write-Host "Cleaned old .nupkg files"

# Pack NuGet packages
dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release -o artifacts "-p:Version=$Version"
dotnet pack src/ANcpLua.NET.Sdk.Web.csproj -c Release -o artifacts "-p:Version=$Version"
dotnet pack src/ANcpLua.NET.Sdk.Test.csproj -c Release -o artifacts "-p:Version=$Version"

# Build ServiceDefaults first (AutoRegister has IncludeBuildOutput=false and manually includes built DLL)
dotnet build eng/ANcpSdk.AspNetCore.ServiceDefaults/ANcpSdk.AspNetCore.ServiceDefaults.csproj -c Release
dotnet build eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj -c Release

# Pack ServiceDefaults packages (required by SDK.Web)
dotnet pack eng/ANcpSdk.AspNetCore.ServiceDefaults/ANcpSdk.AspNetCore.ServiceDefaults.csproj -c Release -o artifacts --no-build "-p:Version=$Version"
dotnet pack eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj -c Release -o artifacts --no-build "-p:Version=$Version"

