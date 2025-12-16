#!/usr/bin/env pwsh
# Build script for CI

param([Parameter(ValueFromRemainingArguments=$true)]$Args)

$ErrorActionPreference = "Stop"

# Extract version from args (e.g., "-p:Version=1.1.7")
$Version = "1.0.0"
foreach ($arg in $Args) {
    if ($arg -match '-p:Version=(.+)') {
        $Version = $Matches[1]
        Write-Host "Detected version: $Version"
    }
}

# Generate Version.props with the package version
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
</Project>
"@
Set-Content -Path $VersionPropsPath -Value $VersionPropsContent -Encoding UTF8
Write-Host "Generated $VersionPropsPath with version $Version"

# Build Analyzers first
dotnet build eng/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release

# Pack NuGet packages
dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release -o artifacts @Args
dotnet pack src/ANcpLua.NET.Sdk.Web.csproj -c Release -o artifacts @Args
dotnet pack src/ANcpLua.NET.Sdk.Test.csproj -c Release -o artifacts @Args

# Build ServiceDefaults first (AutoRegister has IncludeBuildOutput=false and manually includes built DLL)
dotnet build eng/ANcpSdk.AspNetCore.ServiceDefaults/ANcpSdk.AspNetCore.ServiceDefaults.csproj -c Release
dotnet build eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj -c Release

# Pack ServiceDefaults packages (required by SDK.Web)
dotnet pack eng/ANcpSdk.AspNetCore.ServiceDefaults/ANcpSdk.AspNetCore.ServiceDefaults.csproj -c Release -o artifacts --no-build @Args
dotnet pack eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj -c Release -o artifacts --no-build @Args

