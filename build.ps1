#!/usr/bin/env pwsh
# Build script for CI

param([Parameter(ValueFromRemainingArguments=$true)]$Args)

$ErrorActionPreference = "Stop"

# Build Analyzers first
dotnet build eng/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release

# Pack NuGet packages
dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release -o artifacts @Args
dotnet pack src/ANcpLua.NET.Sdk.Web.csproj -c Release -o artifacts @Args

# Debug: List package contents
Write-Host "=== Package Contents ==="
Get-ChildItem artifacts/*.nupkg | ForEach-Object {
    Write-Host "Package: $($_.Name)"
    $tempDir = Join-Path $env:TEMP "nupkg-inspect"
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    Expand-Archive $_.FullName -DestinationPath $tempDir
    Get-ChildItem $tempDir -Recurse -File | ForEach-Object {
        Write-Host "  $($_.FullName.Replace($tempDir, ''))"
    }
}

