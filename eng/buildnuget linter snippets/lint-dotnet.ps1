<#
.SYNOPSIS
    .NET Architecture Linter - Validates MSBuild/NuGet architecture
.DESCRIPTION
    Checks for common architecture violations that cause CI failures:
    - Rule A: Hardcoded versions in Directory.Packages.props
    - Rule B: Version.props imported outside single owner
    - Rule G: PackageReference with inline Version attribute
.PARAMETER Path
    Repository root path (default: current directory)
.EXAMPLE
    ./lint-dotnet.ps1 .
.OUTPUTS
    Exit code 0 = clean, 1 = violations found
#>

param(
    [Parameter(Position = 0)]
    [string]$Path = "."
)

$ErrorActionPreference = "Stop"
$Violations = 0
$RepoRoot = Resolve-Path $Path

function Write-Violation {
    param([string]$Rule, [string]$File, [int]$Line, [string]$Content, [string]$Message, [string]$Fix)
    
    Write-Host "❌ RULE $Rule VIOLATION: ${File}:$Line" -ForegroundColor Red
    Write-Host "   $Content"
    Write-Host ""
    Write-Host "   $Message" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   FIX: $Fix" -ForegroundColor Cyan
    Write-Host ""
    $script:Violations++
}

Write-Host "════════════════════════════════════════════════════════════════"
Write-Host "  .NET Architecture Linter"
Write-Host "════════════════════════════════════════════════════════════════"
Write-Host ""

# ─────────────────────────────────────────────────────────────────────
# Rule A: No hardcoded versions in Directory.Packages.props
# ─────────────────────────────────────────────────────────────────────
Write-Host "📋 Rule A: Checking for hardcoded versions in Directory.Packages.props..."

$DppPath = Join-Path $RepoRoot "Directory.Packages.props"
if (Test-Path $DppPath) {
    $lineNum = 0
    Get-Content $DppPath | ForEach-Object {
        $lineNum++
        $line = $_
        
        # Match PackageVersion with literal version (not variable)
        if ($line -match 'PackageVersion.*Include="([^"]+)".*Version="(\d+\.\d+[^"]*)"') {
            $package = $Matches[1]
            $version = $Matches[2]
            $varName = ($package -replace '\.', '') + "Version"
            
            Write-Violation -Rule "A" -File "Directory.Packages.props" -Line $lineNum `
                -Content $line.Trim() `
                -Message "Package '$package' has hardcoded version '$version'." `
                -Fix "Use Version=`"`$($varName)`" and define <$varName>$version</$varName> in Version.props"
        }
    }
}
else {
    Write-Host "⚠️  Directory.Packages.props not found" -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────────────
# Rule B: Version.props single owner
# ─────────────────────────────────────────────────────────────────────
Write-Host "📋 Rule B: Checking Version.props import ownership..."

$propsFiles = Get-ChildItem -Path $RepoRoot -Recurse -Include "*.props", "*.targets" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|\.git|node_modules)[\\/]' }

$dppHasImport = $false

foreach ($file in $propsFiles) {
    $lineNum = 0
    $relPath = $file.FullName.Replace($RepoRoot.Path, "").TrimStart('\', '/')
    
    Get-Content $file.FullName -ErrorAction SilentlyContinue | ForEach-Object {
        $lineNum++
        $line = $_
        
        if ($line -match 'Import.*Version\.props') {
            if ($file.Name -eq "Directory.Packages.props") {
                $dppHasImport = $true
            }
            else {
                Write-Violation -Rule "B" -File $relPath -Line $lineNum `
                    -Content $line.Trim() `
                    -Message "Version.props must ONLY be imported by Directory.Packages.props (single owner). This duplicate import causes variable resolution failures during NuGet restore." `
                    -Fix "Delete this Import line. Directory.Packages.props owns Version.props import."
            }
        }
    }
}

if ((Test-Path $DppPath) -and -not $dppHasImport) {
    Write-Violation -Rule "B" -File "Directory.Packages.props" -Line 0 `
        -Content "(missing import)" `
        -Message "Directory.Packages.props must import Version.props to define package versions." `
        -Fix "Add: <Import Project=`"`$(MSBuildThisFileDirectory)src/common/Version.props`"/>"
}

# ─────────────────────────────────────────────────────────────────────
# Rule G: No PackageReference with Version attribute
# ─────────────────────────────────────────────────────────────────────
Write-Host "📋 Rule G: Checking for inline PackageReference versions..."

$csprojFiles = Get-ChildItem -Path $RepoRoot -Recurse -Include "*.csproj" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|\.git)[\\/]' }

foreach ($file in $csprojFiles) {
    $lineNum = 0
    $relPath = $file.FullName.Replace($RepoRoot.Path, "").TrimStart('\', '/')
    
    Get-Content $file.FullName -ErrorAction SilentlyContinue | ForEach-Object {
        $lineNum++
        $line = $_
        
        # Match PackageReference with Version= but not VersionOverride=
        if ($line -match 'PackageReference.*Include="([^"]+)".*Version="([^"]+)"' -and 
            $line -notmatch 'VersionOverride') {
            $package = $Matches[1]
            $version = $Matches[2]
            $varName = ($package -replace '\.', '') + "Version"
            
            Write-Violation -Rule "G" -File $relPath -Line $lineNum `
                -Content $line.Trim() `
                -Message "Projects must use Central Package Management, not inline versions." `
                -Fix @"
1. Add to Directory.Packages.props:
      <PackageVersion Include="$package" Version="`$($varName)"/>
   2. Add to Version.props:
      <$varName>$version</$varName>
   3. Change csproj to:
      <PackageReference Include="$package"/>
"@
        }
    }
}

# ─────────────────────────────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────────────────────────────
Write-Host "════════════════════════════════════════════════════════════════"

if ($Violations -eq 0) {
    Write-Host "✅ All rules passed - safe to commit" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "❌ $Violations violation(s) found" -ForegroundColor Red
    Write-Host ""
    Write-Host "⛔ DO NOT commit or push until all violations are fixed." -ForegroundColor Yellow
    Write-Host "   Fix each violation, run 'dotnet build', then re-run this linter."
    exit 1
}
