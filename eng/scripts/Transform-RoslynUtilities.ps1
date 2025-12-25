#!/usr/bin/env pwsh
# Transform ALL Roslyn.Utilities files for SDK embedding
# NO MANUAL FILE LIST - automatically discovers all .cs files
param([switch]$Verbose)

$ErrorActionPreference = 'Stop'
$sourceDir = "$PSScriptRoot/../submodules/Roslyn.Utilities/ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities"
$outputDir = "$PSScriptRoot/../.generated/SourceGen"

if (-not (Test-Path $sourceDir)) {
    Write-Error "Source directory not found: $sourceDir"
    Write-Error "Run: git submodule update --init --recursive"
    exit 1
}

# Clean and create output
if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

# Auto-discover ALL .cs files (no hardcoded list!)
$allFiles = Get-ChildItem $sourceDir -Filter "*.cs" -Recurse | Where-Object { $_.Name -ne "AssemblyInfo.cs" }
$count = 0

foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Substring($sourceDir.Length + 1)
    $content = Get-Content $file.FullName -Raw

    # Transform namespace
    $content = $content -replace 'namespace ANcpLua\.Roslyn\.Utilities\.Models', 'namespace ANcpLua.SourceGen'
    $content = $content -replace 'namespace ANcpLua\.Roslyn\.Utilities', 'namespace ANcpLua.SourceGen'

    # Transform visibility (public -> internal)
    $content = $content -replace 'public static class', 'internal static class'
    $content = $content -replace 'public readonly struct', 'internal readonly struct'
    $content = $content -replace 'public readonly record struct', 'internal readonly record struct'
    $content = $content -replace 'public sealed class', 'internal sealed class'
    $content = $content -replace 'public record struct', 'internal record struct'
    $content = $content -replace 'public static partial class', 'internal static partial class'

    # Add preprocessor guard
    $content = "#if ANCPLUA_SOURCEGEN_HELPERS`n$content`n#endif"

    # Preserve directory structure
    $outputPath = Join-Path $outputDir $relativePath
    $outputDirPath = Split-Path $outputPath -Parent
    if (-not (Test-Path $outputDirPath)) {
        New-Item -ItemType Directory -Path $outputDirPath -Force | Out-Null
    }

    Set-Content $outputPath $content -NoNewline
    $count++
    if ($Verbose) { Write-Host "Transformed: $relativePath" }
}

Write-Host "Transformed $count files to $outputDir"
