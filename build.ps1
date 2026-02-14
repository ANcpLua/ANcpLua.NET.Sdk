#!/usr/bin/env pwsh
# Build script for CI - Packs all SDK NuGet packages

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(ValueFromRemainingArguments = $true)]$RemainingArgs
)

$ErrorActionPreference = "Stop"

Write-Host "Building version: $Version" -ForegroundColor Cyan

# Stamp the version into Version.props
$VersionPropsPath = "src/Build/Common/Version.props"
if (-not (Test-Path $VersionPropsPath)) {
    throw "Version.props not found at $VersionPropsPath"
}
$content = Get-Content $VersionPropsPath -Raw
$content = $content -replace '<ANcpSdkPackageVersion>[^<]+</ANcpSdkPackageVersion>', "<ANcpSdkPackageVersion>$Version</ANcpSdkPackageVersion>"
Set-Content -Path $VersionPropsPath -Value $content -NoNewline -Encoding UTF8
Write-Host "Stamped ANcpSdkPackageVersion=$Version in $VersionPropsPath"

# Clean old .nupkg files
Get-ChildItem artifacts/*.nupkg -ErrorAction SilentlyContinue | Remove-Item -Force
Write-Host "Cleaned old .nupkg files"

# Pack NuGet packages
dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release -o artifacts "-p:Version=$Version"
dotnet pack src/ANcpLua.NET.Sdk.Web.csproj -c Release -o artifacts "-p:Version=$Version"
dotnet pack src/ANcpLua.NET.Sdk.Test.csproj -c Release -o artifacts "-p:Version=$Version"
