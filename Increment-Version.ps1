# Script pour incrémenter manuellement la version du projet
# Source unique : appsettings.json -> AppInfo.Version
# Usage : .\Increment-Version.ps1 [-Type major|minor|patch] (par défaut: patch)

param(
    [ValidateSet("major", "minor", "patch")]
    [string]$Type = "patch"
)

$CsprojFile = "CharacterManager\CharacterManager.csproj"
$AppSettingsFile = "CharacterManager\appsettings.json"

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

# Mettre à jour appsettings
$appJson.AppInfo.Version = $newVersion
$appJson | ConvertTo-Json -Depth 10 | Out-File $AppSettingsFile -Encoding utf8 -NoNewline

# Mettre à jour csproj
$csprojContent = Get-Content $CsprojFile -Raw
$csprojContent = $csprojContent -replace '<Version>.*?</Version>', "<Version>$newVersion</Version>"
$csprojContent = $csprojContent -replace '<InformationalVersion>.*?</InformationalVersion>', "<InformationalVersion>$newVersion</InformationalVersion>"
$csprojContent | Out-File $CsprojFile -Encoding utf8 -NoNewline

Write-Host "Version incrémentée ($Type): $currentVersion -> $newVersion" -ForegroundColor Green

# Proposer de commiter
$response = Read-Host "Voulez-vous commiter cette modification? (o/n)"
if ($response -eq 'o' -or $response -eq 'O') {
    git add $AppSettingsFile $CsprojFile
    git commit -m "chore: bump version to $newVersion"
    Write-Host "Commit effectué!" -ForegroundColor Green
}
