<#
.SYNOPSIS
    Migrates .NET solution from filename-based detection to MSBuild Project Capabilities.
.DESCRIPTION
    Scans all .csproj files. If a project matches naming patterns (.Web, .Api, .Tests),
    it injects a <ProjectCapability> item. SDK-style projects use NO XML namespace.
.EXAMPLE
    ./Migrate-Capabilities.ps1 -RootPath "C:\repos\MyProject"
#>

param (
    [string]$RootPath = ".",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Capability mapping: regex pattern -> capability name
$capabilityMap = @{
    '\.Web\.csproj$'    = 'AncpLuaWeb'
    '\.Api\.csproj$'    = 'AncpLuaApi'
    '\.Tests?\.csproj$' = 'AncpLuaTest'
}

$projects = Get-ChildItem -Path $RootPath -Filter "*.csproj" -Recurse

foreach ($proj in $projects) {
    [xml]$xml = Get-Content $proj.FullName -Raw

    # SDK-style projects have NO namespace - don't use namespace manager
    $isDirty = $false
    $capabilitiesToAdd = @()

    # Determine capabilities based on filename patterns
    foreach ($pattern in $capabilityMap.Keys) {
        if ($proj.Name -match $pattern) {
            $capabilitiesToAdd += $capabilityMap[$pattern]
        }
    }

    if ($capabilitiesToAdd.Count -eq 0) {
        continue
    }

    foreach ($capInclude in $capabilitiesToAdd) {
        # Check if capability already exists (no namespace for SDK-style)
        $existing = $xml.SelectNodes("//ProjectCapability[@Include='$capInclude']")

        if ($existing.Count -eq 0) {
            if ($WhatIf) {
                Write-Host "[WhatIf] Would add capability '$capInclude' to $($proj.Name)" -ForegroundColor Yellow
                continue
            }

            Write-Host "Adding capability '$capInclude' to $($proj.Name)" -ForegroundColor Cyan

            # Find existing ItemGroup with ProjectCapability, or create new one
            $itemGroup = $xml.SelectSingleNode("//ItemGroup[ProjectCapability]")

            if ($null -eq $itemGroup) {
                # Create new ItemGroup - SDK-style has no namespace
                $itemGroup = $xml.CreateElement("ItemGroup")

                # Insert after last PropertyGroup for clean formatting
                $lastPropGroup = $xml.SelectNodes("//PropertyGroup") | Select-Object -Last 1
                if ($lastPropGroup) {
                    $xml.Project.InsertAfter($itemGroup, $lastPropGroup) | Out-Null
                } else {
                    $xml.Project.AppendChild($itemGroup) | Out-Null
                }
            }

            # Create ProjectCapability element (no namespace)
            $capNode = $xml.CreateElement("ProjectCapability")
            $capNode.SetAttribute("Include", $capInclude)
            $itemGroup.AppendChild($capNode) | Out-Null

            $isDirty = $true
        } else {
            Write-Host "Capability '$capInclude' already exists in $($proj.Name)" -ForegroundColor DarkGray
        }
    }

    if ($isDirty) {
        # Preserve formatting - use XmlWriterSettings
        $settings = New-Object System.Xml.XmlWriterSettings
        $settings.Indent = $true
        $settings.IndentChars = "  "
        $settings.OmitXmlDeclaration = $true
        $settings.Encoding = [System.Text.UTF8Encoding]::new($false) # No BOM

        $writer = [System.Xml.XmlWriter]::Create($proj.FullName, $settings)
        try {
            $xml.Save($writer)
        } finally {
            $writer.Close()
        }
    }
}

Write-Host "`nMigration complete." -ForegroundColor Green
