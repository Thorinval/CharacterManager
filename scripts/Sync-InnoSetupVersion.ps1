# Synchronize version from appsettings.json to CharacterManager.iss
# Usage: .\Sync-InnoSetupVersion.ps1
# This script reads the version from appsettings.json and updates CharacterManager.iss

Write-Host "=== Synchronizing Inno Setup Version ===" -ForegroundColor Cyan
Write-Host ""

# Déterminer le répertoire racine (parent du dossier scripts)
$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent -Path $scriptDir

# Read version from appsettings.json
$appSettingsPath = Join-Path $rootDir "CharacterManager\appsettings.json"
if (-not (Test-Path $appSettingsPath)) {
    Write-Error "appsettings.json not found at $appSettingsPath"
    exit 1
}

$appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
$version = $appSettings.AppInfo.Version

if (-not $version) {
    Write-Error "Could not read version from appsettings.json"
    exit 1
}

Write-Host "Version from appsettings.json: $version" -ForegroundColor Green
Write-Host ""

# Read current Inno Setup file
$innoPath = Join-Path $rootDir "CharacterManager.iss"
if (-not (Test-Path $innoPath)) {
    Write-Error "CharacterManager.iss not found at $innoPath"
    exit 1
}

$innoContent = Get-Content $innoPath -Raw

# Extract current version from .iss file
if ($innoContent -match 'AppVersion=(\d+\.\d+\.\d+)') {
    $currentVersion = $matches[1]
    Write-Host "Current version in CharacterManager.iss: $currentVersion" -ForegroundColor Yellow
} else {
    Write-Host "Could not read current version from CharacterManager.iss" -ForegroundColor Yellow
    $currentVersion = "unknown"
}

Write-Host ""

# If versions match, we're done
if ($version -eq $currentVersion) {
    Write-Host "Versions already match - no update needed" -ForegroundColor Green
    exit 0
}

# Update AppVersion using safe regex pattern
$newContent = $innoContent -replace "AppVersion=\d+\.\d+\.\d+", "AppVersion=$version"

# Update OutputBaseFilename using safe regex pattern
$newContent = $newContent -replace "OutputBaseFilename=CharacterManager-\d+\.\d+\.\d+-Setup", "OutputBaseFilename=CharacterManager-$version-Setup"

# Write back
Set-Content -Path $innoPath -Value $newContent -Encoding UTF8

Write-Host "Updated CharacterManager.iss:" -ForegroundColor Green
Write-Host "  AppVersion: $currentVersion --> $version" -ForegroundColor Green
Write-Host "  OutputBaseFilename: CharacterManager-$currentVersion-Setup --> CharacterManager-$version-Setup" -ForegroundColor Green
Write-Host ""
Write-Host "Synchronization complete!" -ForegroundColor Green
