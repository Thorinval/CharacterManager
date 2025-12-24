# Synchronise la version de l'application entre appsettings.json et release_notes.md
# Usage: .\SyncReleaseNotesVersion.ps1

$ErrorActionPreference = 'Stop'

$appSettingsPath = "d:\Devs\CharacterManager\CharacterManager\appsettings.json"
$releaseNotesPath = "d:\Devs\CharacterManager\docs\RELEASE_NOTES.md"

# Lire la version dans appsettings.json
$appSettings = Get-Content $appSettingsPath | Out-String | ConvertFrom-Json
$version = $appSettings.Version

if (-not $version) {
    Write-Error "La clé 'Version' n'existe pas dans appsettings.json."
    exit 1
}

# Lire le contenu du fichier release_notes.md
$releaseNotes = Get-Content $releaseNotesPath -Raw

# Remplacer la ligne de version
$pattern = '(\*\*Version actuelle\*\*\s*:\s*)([\w\.-]+)'
$replacement = "`$1$version"
$newReleaseNotes = [System.Text.RegularExpressions.Regex]::Replace($releaseNotes, $pattern, $replacement)

# Écrire le nouveau contenu
Set-Content -Path $releaseNotesPath -Value $newReleaseNotes

Write-Host "Version synchronisée : $version"
