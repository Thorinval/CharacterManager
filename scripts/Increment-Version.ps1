# Script pour incrémenter manuellement la version du projet
# Source unique : appsettings.json -> AppInfo.Version
# Usage : .\Increment-Version.ps1 [-Type major|minor|patch] [-Test] (par défaut: patch)
# Mode test: .\Increment-Version.ps1 -Type patch -Test (simule l'incrémentation sans modifier les fichiers)

param(
    [ValidateSet("major", "minor", "patch")]
    [string]$Type = "patch",
    [switch]$Test = $false
)

# Déterminer le répertoire racine (parent du dossier scripts)
$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent -Path $scriptDir

$CsprojFile = Join-Path $rootDir "CharacterManager\CharacterManager.csproj"
$AppSettingsFile = Join-Path $rootDir "CharacterManager\appsettings.json"

if (-not (Test-Path $CsprojFile)) { Write-Error "Erreur: $CsprojFile introuvable"; exit 1 }
if (-not (Test-Path $AppSettingsFile)) { Write-Error "Erreur: $AppSettingsFile introuvable"; exit 1 }

$appJson = Get-Content $AppSettingsFile -Raw | ConvertFrom-Json
$currentVersion = $appJson.AppInfo.Version
if (-not $currentVersion) { Write-Error "Erreur: AppInfo.Version introuvable"; exit 1 }

$parts = $currentVersion.Split('.')
switch ($Type) {
    "major" { $parts[0] = [string]([int]$parts[0] + 1); $parts[1] = '0'; $parts[2] = '0' }
    "minor" { $parts[1] = [string]([int]$parts[1] + 1); $parts[2] = '0' }
    "patch" { $parts[2] = [string]([int]$parts[2] + 1) }
}
$newVersion = ($parts -join '.')

# Mode test: afficher le résultat sans modifier les fichiers
if ($Test) {
    Write-Host ""
    Write-Host "========== MODE TEST ==========" -ForegroundColor Yellow
    Write-Host "Aucun fichier n'a été modifié" -ForegroundColor Yellow
    Write-Host "Version actuelle: $currentVersion" -ForegroundColor Cyan
    Write-Host "Version simulée ($Type): $newVersion" -ForegroundColor Green
    Write-Host "==============================" -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

# Mettre à jour appsettings
$appJson.AppInfo.Version = $newVersion
$appJson | ConvertTo-Json -Depth 10 | Out-File $AppSettingsFile -Encoding utf8 -NoNewline

# Mettre à jour csproj
$csprojContent = Get-Content $CsprojFile -Raw
$csprojContent = $csprojContent -replace '<Version>.*?</Version>', "<Version>$newVersion</Version>"
$csprojContent = $csprojContent -replace '<InformationalVersion>.*?</InformationalVersion>', "<InformationalVersion>$newVersion</InformationalVersion>"
$csprojContent | Out-File $CsprojFile -Encoding utf8 -NoNewline

Write-Host "Version incrémentée ($Type): $currentVersion -> $newVersion" -ForegroundColor Green