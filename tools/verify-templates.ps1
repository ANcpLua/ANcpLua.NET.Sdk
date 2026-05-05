#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies the ANcpLua.NET.Sdk.Templates publish pipeline end-to-end.

.DESCRIPTION
    Fails non-zero unless every gate passes:
      1. .github/workflows/nuget-publish.yml publish gate includes 'templates/**/*'
      2. ./build.ps1 -Version <Version> produces all 4 nupkgs
      3. ANcpLua.NET.Sdk.Templates.<Version>.nupkg exists
      4. The Templates package has packageType 'Template'
      5. Templates package contains all 3 short names: ancplua-app, ancplua-lib, ancplua-web
      6. No __PACK_TIME_*__ placeholders remain in any packaged template.json
      7. Each template installs into a hermetic --debug:custom-hive
      8. Each template scaffolds with --skipRestore
      9. Each scaffolded global.json has no ANCPLUA_* placeholders and pins the test version
     10. Each scaffolded solution restores + builds against the local nupkg feed
         using macOS-safe sequential settings

    Run this BEFORE pushing PR changes that affect the templates pipeline. CI parity:
    GitHub macOS uses identical dotnet 10.0.203 + identical fixture, so a green local
    run is high-confidence for green GitHub macOS.

.EXAMPLE
    pwsh tools/verify-templates.ps1 -Version 999.9.9
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$artifacts = Join-Path $repoRoot 'artifacts'
$failures = New-Object System.Collections.Generic.List[string]
$startedAt = Get-Date

function Write-Step([string]$message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Write-Pass([string]$message) {
    Write-Host "    [pass] $message" -ForegroundColor Green
}

function Write-Fail([string]$message) {
    Write-Host "    [FAIL] $message" -ForegroundColor Red
    $failures.Add($message)
}

function Resolve-CanonicalPath([string]$p) {
    # macOS: /var → /private/var, /tmp → /private/tmp. Normalize so NuGet's slnx
    # restore doesn't see the same physical file via two prefixes.
    $info = Get-Item -LiteralPath $p
    if ($info.LinkType -eq 'SymbolicLink' -and $info.Target) {
        return (Get-Item -LiteralPath $info.Target).FullName
    }
    return (Resolve-Path -LiteralPath $p).Path
}

# --------------------------------------------------------------------------
# Gate 1 — publish workflow includes templates/**/*
# --------------------------------------------------------------------------
Write-Step "Gate 1: nuget-publish.yml publish gate includes 'templates/**/*'"
$workflowPath = Join-Path $repoRoot '.github/workflows/nuget-publish.yml'
if (-not (Test-Path $workflowPath)) {
    Write-Fail "Workflow not found at $workflowPath."
}
else {
    $workflow = Get-Content $workflowPath -Raw
    # Regex anchor pitfall: `$ in a PowerShell double-quoted string becomes a literal $,
    # which the regex engine then reads as end-of-line. Use single-quote string + \$
    # (regex literal $) so the pattern actually matches the workflow's $PreviousTag token.
    $publishGatePattern = 'git diff --name-only \$PreviousTag HEAD --[^\r\n]*''templates/\*\*/\*'''
    if ($workflow -match $publishGatePattern) {
        Write-Pass "publish gate covers 'templates/**/*'"
    }
    else {
        Write-Fail "publish gate diff filter does not include 'templates/**/*' — template-only edits would silently skip publish"
    }
}

# --------------------------------------------------------------------------
# Gate 2 — build.ps1 produces all 4 nupkgs
# --------------------------------------------------------------------------
Write-Step "Gate 2: ./build.ps1 -Version $Version produces all 4 nupkgs"
$buildPs1 = Join-Path $repoRoot 'build.ps1'
if (-not (Test-Path $buildPs1)) {
    Write-Fail "build.ps1 not found at $buildPs1."
}
else {
    Push-Location $repoRoot
    try {
        # Clean stale artifacts so we're sure this run produced them
        if (Test-Path $artifacts) {
            Get-ChildItem -Path $artifacts -Filter '*.nupkg' -ErrorAction SilentlyContinue |
                Remove-Item -Force
        }
        & pwsh -NoLogo -NoProfile -File $buildPs1 -Version $Version | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "build.ps1 exit $LASTEXITCODE"
        }
        else {
            $expected = @(
                "ANcpLua.NET.Sdk.$Version.nupkg",
                "ANcpLua.NET.Sdk.Test.$Version.nupkg",
                "ANcpLua.NET.Sdk.Web.$Version.nupkg",
                "ANcpLua.NET.Sdk.Templates.$Version.nupkg"
            )
            foreach ($name in $expected) {
                if (Test-Path (Join-Path $artifacts $name)) {
                    Write-Pass "$name produced"
                }
                else {
                    Write-Fail "$name missing from artifacts/"
                }
            }
        }
    }
    finally {
        Pop-Location
    }
}

$templatesNupkg = Join-Path $artifacts "ANcpLua.NET.Sdk.Templates.$Version.nupkg"
if (-not (Test-Path $templatesNupkg)) {
    Write-Step 'Subsequent gates skipped: Templates nupkg missing.'
    if ($failures.Count -gt 0) {
        Write-Host ""
        Write-Host "VERIFIER FAILED: $($failures.Count) issue(s)" -ForegroundColor Red
        $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        exit 1
    }
    exit 1
}

# --------------------------------------------------------------------------
# Gate 3-6 — Templates nupkg shape (zip inspection only, no dotnet new yet)
# --------------------------------------------------------------------------
Write-Step "Gate 3: Templates package metadata + content"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($templatesNupkg)
try {
    # 4. packageType = Template
    $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
    if (-not $nuspecEntry) {
        Write-Fail "Nuspec entry not found inside $templatesNupkg"
    }
    else {
        $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
        try { $nuspecXml = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
        $hasTemplate = $false
        if ($nuspecXml.package.metadata.packageTypes) {
            foreach ($pt in @($nuspecXml.package.metadata.packageTypes.packageType)) {
                if ($pt.name -eq 'Template') { $hasTemplate = $true }
            }
        }
        if ($hasTemplate) { Write-Pass 'packageType is "Template"' }
        else { Write-Fail 'packageType "Template" not declared in nuspec' }
    }

    # 5. Three short names present
    $shortNames = @('ancplua-app', 'ancplua-lib', 'ancplua-web')
    foreach ($sn in $shortNames) {
        $expected = "content/$sn/.template.config/template.json"
        if ($zip.Entries.FullName -contains $expected) {
            Write-Pass "Template '$sn' present at $expected"
        }
        else {
            Write-Fail "Template '$sn' missing — expected $expected"
        }
    }

    # 6. No __PACK_TIME_*__ placeholders left in any template.json
    $templateJsonEntries = $zip.Entries | Where-Object { $_.FullName -like 'content/*/.template.config/template.json' }
    foreach ($entry in $templateJsonEntries) {
        $reader = New-Object System.IO.StreamReader($entry.Open())
        try { $body = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($body -match '__PACK_TIME_[A-Z_]+__') {
            Write-Fail "$($entry.FullName) still has placeholder $($Matches[0]) — pack-time stamping did not run"
        }
        else {
            Write-Pass "$($entry.FullName) has no leftover __PACK_TIME_*__ placeholders"
        }
        # Sanity check: SdkVersion default should be exactly $Version
        if ($body -notmatch "`"SdkVersion`"\s*:\s*\{[^}]*`"defaultValue`"\s*:\s*`"$([regex]::Escape($Version))`"") {
            Write-Fail "$($entry.FullName) SdkVersion.defaultValue is not '$Version'"
        }
    }
}
finally {
    $zip.Dispose()
}

# --------------------------------------------------------------------------
# Gate 7-10 — install + scaffold + restore + build, hermetic + macOS-safe
# --------------------------------------------------------------------------
Write-Step "Gate 7-10: Install + scaffold + restore + build each template"
$tempRoot = Resolve-CanonicalPath ([System.IO.Path]::GetTempPath())
$verifyRoot = Join-Path $tempRoot "ancplua-verify-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $verifyRoot | Out-Null

try {
    foreach ($shortName in @('ancplua-app', 'ancplua-lib', 'ancplua-web')) {
        Write-Host ""
        Write-Host "  -- $shortName --" -ForegroundColor Yellow
        $hive = Join-Path $verifyRoot "$shortName-hive"
        $output = Join-Path $verifyRoot "$shortName-out"
        New-Item -ItemType Directory -Path $hive | Out-Null
        New-Item -ItemType Directory -Path $output | Out-Null

        # Shut down any prior build server state so this scaffold restore is hermetic
        & dotnet build-server shutdown | Out-Null

        # 7. install hermetically
        & dotnet new install $templatesNupkg --debug:custom-hive $hive | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "$shortName install failed (exit $LASTEXITCODE)"
            continue
        }
        Write-Pass "$shortName installed"

        # 8. scaffold with --skipRestore
        $projectName = "T" + [Guid]::NewGuid().ToString('N').Substring(0, 8)
        & dotnet new $shortName -n $projectName -o $output --debug:custom-hive $hive --skipRestore | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "$shortName scaffold failed (exit $LASTEXITCODE)"
            continue
        }
        Write-Pass "$shortName scaffolded as $projectName"

        # 9. global.json: no placeholders, version pinned
        $globalJson = Get-Content (Join-Path $output 'global.json') -Raw
        if ($globalJson -match 'ANCPLUA_[A-Z_]+_PLACEHOLDER') {
            Write-Fail "$shortName scaffolded global.json has unresolved $($Matches[0])"
        }
        elseif ($globalJson -notmatch "`"ANcpLua\.NET\.Sdk`"\s*:\s*`"$([regex]::Escape($Version))`"") {
            Write-Fail "$shortName scaffolded global.json does not pin ANcpLua.NET.Sdk to '$Version'"
        }
        else {
            Write-Pass "$shortName global.json pins SDK to $Version with no placeholders"
        }

        # 10. restore + build using local feed, macOS-safe sequential settings
        # Override scaffolded nuget.config to use local feed for SDK + nuget.org for analyzers
        $nugetCache = Join-Path $output 'nuget-cache'
        $fixturePackages = Resolve-CanonicalPath $artifacts
        @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$nugetCache" />
  </config>
  <packageSources>
    <clear/>
    <add key="TestSource" value="$fixturePackages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Path (Join-Path $output 'nuget.config') -NoNewline

        $slnx = Join-Path $output "$projectName.slnx"
        Push-Location $output
        try {
            & dotnet restore $slnx --disable-parallel -nodeReuse:false | Out-Host
            if ($LASTEXITCODE -ne 0) {
                Write-Fail "$shortName scaffolded restore failed (exit $LASTEXITCODE)"
                continue
            }
            Write-Pass "$shortName scaffolded restore succeeded"

            & dotnet build $slnx --no-restore --nologo -nodeReuse:false -maxcpucount:1 | Out-Host
            if ($LASTEXITCODE -ne 0) {
                Write-Fail "$shortName scaffolded build failed (exit $LASTEXITCODE)"
                continue
            }
            Write-Pass "$shortName scaffolded build succeeded"
        }
        finally {
            Pop-Location
        }
    }
}
finally {
    if (Test-Path $verifyRoot) {
        Remove-Item -Path $verifyRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# --------------------------------------------------------------------------
# Summary
# --------------------------------------------------------------------------
Write-Host ""
$elapsed = ((Get-Date) - $startedAt).TotalSeconds
if ($failures.Count -eq 0) {
    Write-Host "VERIFIER PASSED ($([math]::Round($elapsed, 1))s)" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "VERIFIER FAILED: $($failures.Count) issue(s) ($([math]::Round($elapsed, 1))s)" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
