#!/usr/bin/env pwsh
# Transform Roslyn.Utilities files for SDK embedding
param([switch]$Verbose)

$ErrorActionPreference = 'Stop'
$sourceDir = "$PSScriptRoot/../submodules/Roslyn.Utilities/ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities"
$outputDir = "$PSScriptRoot/../.generated/SourceGen"

# Clean and create output
if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
New-Item -ItemType Directory -Path "$outputDir/Models" -Force | Out-Null

$files = @(
    "EquatableArray.cs",
    "SymbolExtensions.cs",
    "SyntaxExtensions.cs",
    "SemanticModelExtensions.cs",
    "CompilationExtensions.cs",
    "EnumerableExtensions.cs",
    "SourceProductionContextExtensions.cs",
    "AnalyzerConfigOptionsProviderExtensions.cs",
    "StringExtensions.cs",
    "AttributeDataExtensions.cs",
    "ConvertExtensions.cs",
    "IncrementalValuesProviderExtensions.cs",
    "FileExtensions.cs"
)

$modelFiles = @(
    "Models/LocationInfo.cs",
    "Models/DiagnosticInfo.cs",
    "Models/EquatableMessageArgs.cs"
)

foreach ($file in $files) {
    $sourcePath = Join-Path $sourceDir $file
    if (-not (Test-Path $sourcePath)) {
        Write-Warning "File not found: $file"
        continue
    }

    $content = Get-Content $sourcePath -Raw

    # Transform namespace
    $content = $content -replace 'namespace ANcpLua\.Roslyn\.Utilities', 'namespace ANcpLua.SourceGen'

    # Transform visibility (class/struct/record declarations)
    $content = $content -replace 'public static class', 'internal static class'
    $content = $content -replace 'public readonly struct', 'internal readonly struct'
    $content = $content -replace 'public readonly record struct', 'internal readonly record struct'
    $content = $content -replace 'public sealed class', 'internal sealed class'

    # Add preprocessor guard
    $content = "#if ANCPLUA_SOURCEGEN_HELPERS`n$content`n#endif"

    $outputPath = Join-Path $outputDir (Split-Path $file -Leaf)
    Set-Content $outputPath $content -NoNewline
    if ($Verbose) { Write-Host "Transformed: $file" }
}

foreach ($file in $modelFiles) {
    $sourcePath = Join-Path $sourceDir $file
    if (-not (Test-Path $sourcePath)) {
        Write-Warning "File not found: $file"
        continue
    }

    $content = Get-Content $sourcePath -Raw
    $content = $content -replace 'namespace ANcpLua\.Roslyn\.Utilities\.Models', 'namespace ANcpLua.SourceGen'
    $content = $content -replace 'public readonly record struct', 'internal readonly record struct'
    $content = "#if ANCPLUA_SOURCEGEN_HELPERS`n$content`n#endif"

    $outputPath = Join-Path "$outputDir/Models" (Split-Path $file -Leaf)
    Set-Content $outputPath $content -NoNewline
    if ($Verbose) { Write-Host "Transformed: $file" }
}

Write-Host "Transformed $(($files + $modelFiles).Count) files to $outputDir"
