# Wrapper script for Create-Release
# This file allows calling Create-Release.ps1 from the root directory
# It simply delegates to the actual script in the scripts/ folder

param(
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionType = "patch"
)

$scriptPath = Join-Path $PSScriptRoot "scripts\Create-Release.ps1"

if (-not (Test-Path $scriptPath)) {
    Write-Error "Create-Release.ps1 not found at: $scriptPath"
    exit 1
}

# Call the actual script with the same parameters
& $scriptPath -VersionType $VersionType
