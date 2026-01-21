#!/usr/bin/env pwsh
# Build script for CI

param(
    [string]$Version = "1.6.21",
    [Parameter(ValueFromRemainingArguments = $true)]$RemainingArgs
)

$ErrorActionPreference = "Stop"

# =============================================================================
# VERSION INFO
# =============================================================================
Write-Host "Building version: $Version" -ForegroundColor Cyan

# Generate Version.props with the package version and centralized dependency versions
$VersionPropsPath = "src/common/Version.props"
$VersionPropsContent = @"
<Project>
  <!--
    ═══════════════════════════════════════════════════════════════════════════
    CENTRALIZED PACKAGE VERSIONS - Single Source of Truth
    ═══════════════════════════════════════════════════════════════════════════

    This file is auto-generated during build by build.ps1.
    To update versions, modify the heredoc in build.ps1.

    This file defines ALL shared package versions used across:
    - ANcpLua.NET.Sdk (this SDK)
    - ANcpLua.Roslyn.Utilities (via symlink)
    - ANcpLua.Analyzers (via symlink)

    USAGE IN CONSUMING PROJECTS' Directory.Packages.props:
      <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="`$(RoslynVersion)"/>
  -->
  <PropertyGroup Label="SDK Version (Auto-generated)">
    <ANcpSdkPackageVersion>$Version</ANcpSdkPackageVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       ROSLYN / CODE ANALYSIS
       Used by: SDK, Roslyn.Utilities, Analyzers
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Roslyn">
    <RoslynVersion>5.0.0</RoslynVersion>
    <RoslynAnalyzersVersion>3.11.0</RoslynAnalyzersVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       ROSLYN.UTILITIES (Runtime package)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Roslyn.Utilities">
    <ANcpLuaRoslynUtilitiesVersion>1.14.0</ANcpLuaRoslynUtilitiesVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       ROSLYN.UTILITIES.SOURCES (Source-only package for generators)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Roslyn.Utilities.Sources">
    <ANcpLuaRoslynUtilitiesSourcesVersion>1.14.0</ANcpLuaRoslynUtilitiesSourcesVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       ROSLYN.UTILITIES.TESTING (Analyzer/CodeFix/Generator test infrastructure)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Roslyn.Utilities.Testing">
    <ANcpLuaRoslynUtilitiesTestingVersion>1.14.0</ANcpLuaRoslynUtilitiesTestingVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       ROSLYN ANALYZER TESTING (Beta from dotnet-tools feed)
       Used by: Roslyn.Utilities.Testing, Analyzers.Tests
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Analyzer Testing">
    <AnalyzerTestingVersion>1.1.3</AnalyzerTestingVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       REFERENCE ASSEMBLIES
       Used by: Roslyn.Utilities.Testing, Analyzers.Tests
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Reference Assemblies">
    <BasicReferenceAssembliesVersion>1.8.4</BasicReferenceAssembliesVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       MEZIANTOU PACKAGES
       Used by: SDK, Roslyn.Utilities
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Meziantou">
    <MeziantouFrameworkVersion>5.0.7</MeziantouFrameworkVersion>
    <MeziantouFullPathVersion>1.1.12</MeziantouFullPathVersion>
    <MeziantouTemporaryDirectoryVersion>1.0.30</MeziantouTemporaryDirectoryVersion>
    <MeziantouThreadingVersion>2.0.3</MeziantouThreadingVersion>
    <MeziantouDependencyScanningVersion>2.0.5</MeziantouDependencyScanningVersion>
    <MeziantouAnalyzerVersion>2.0.188</MeziantouAnalyzerVersion>
    <MeziantouParallelTestFrameworkVersion>1.0.6</MeziantouParallelTestFrameworkVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       TESTING FRAMEWORKS
       Used by: SDK, Roslyn.Utilities.Testing, Analyzers.Tests
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Testing">
    <XunitV3Version>3.2.2</XunitV3Version>
    <AwesomeAssertionsVersion>9.3.0</AwesomeAssertionsVersion>
    <AwesomeAssertionsAnalyzersVersion>9.0.8</AwesomeAssertionsAnalyzersVersion>
    <GitHubActionsTestLoggerVersion>3.0.1</GitHubActionsTestLoggerVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       MICROSOFT TESTING PLATFORM (MTP) EXTENSIONS
       Used by: SDK (auto-injected for TUnit, NUnit MTP, MSTest MTP)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="MTP Extensions">
    <MTPExtensionsVersion>2.0.2</MTPExtensionsVersion>
    <CodeCoverageVersion>18.1.0</CodeCoverageVersion>
    <TestSdkVersion>18.0.1</TestSdkVersion>
    <DiagnosticsTestingVersion>10.0.0</DiagnosticsTestingVersion>
    <GitHubActionsLoggerMTPVersion>3.0.1</GitHubActionsLoggerMTPVersion>
    <GitHubActionsLoggerVSTestVersion>2.4.1</GitHubActionsLoggerVSTestVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       ANALYZERS (SDK-injected)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Analyzers">
    <ANcpLuaAnalyzersVersion>1.6.16</ANcpLuaAnalyzersVersion>
    <SbomTargetsVersion>4.1.5</SbomTargetsVersion>
    <BannedApiAnalyzersVersion>3.3.4</BannedApiAnalyzersVersion>
    <JonSkeetAnalyzersVersion>1.0.0-beta.6</JonSkeetAnalyzersVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       MICROSOFT.EXTENSIONS (ASP.NET Core / Hosting)
       Used by: SDK (ServiceDefaults), Web projects
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Microsoft.Extensions">
    <MicrosoftExtensionsVersion>10.2.0</MicrosoftExtensionsVersion>
    <AspNetCoreVersion>10.0.2</AspNetCoreVersion>
    <MicrosoftBclAsyncInterfacesVersion>10.0.2</MicrosoftBclAsyncInterfacesVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       OPENTELEMETRY
       Used by: SDK (ServiceDefaults)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="OpenTelemetry">
    <OpenTelemetryVersion>1.14.0</OpenTelemetryVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       NUGET / BUILD TOOLS
       Used by: SDK
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Build Tools">
    <NuGetVersion>7.0.1</NuGetVersion>
    <MSBuildStructuredLoggerVersion>2.3.113</MSBuildStructuredLoggerVersion>
    <MicrosoftSourceLinkVersion>8.0.0</MicrosoftSourceLinkVersion>
    <MicrosoftDeploymentDotNetReleasesVersion>1.0.1</MicrosoftDeploymentDotNetReleasesVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       LEGACY POLYFILLS (netstandard2.0 support)
       Used by: SDK (LegacySupport)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Legacy Polyfills">
    <BclAsyncInterfacesVersion>6.0.0</BclAsyncInterfacesVersion>
    <TasksExtensionsVersion>4.5.4</TasksExtensionsVersion>
    <BclHashCodeVersion>6.0.0</BclHashCodeVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       MISCELLANEOUS
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Misc">
    <JetBrainsAnnotationsVersion>2025.2.4</JetBrainsAnnotationsVersion>
  </PropertyGroup>

  <!-- ═══════════════════════════════════════════════════════════════════════
       LEGACY ALIASES (backward compatibility)
       ═══════════════════════════════════════════════════════════════════════ -->
  <PropertyGroup Label="Aliases">
    <XunitMtpVersion>`$(XunitV3Version)</XunitMtpVersion>
    <ParallelTestFrameworkVersion>`$(MeziantouParallelTestFrameworkVersion)</ParallelTestFrameworkVersion>
    <MvcTestingVersion>`$(AspNetCoreVersion)</MvcTestingVersion>
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