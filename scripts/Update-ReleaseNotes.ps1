#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Met à jour automatiquement le fichier RELEASE_NOTES.md avec une nouvelle entrée de version.

.DESCRIPTION
    Ce script ajoute une nouvelle section de version au fichier RELEASE_NOTES.md
    et met à jour également appsettings.json et CharacterManager.csproj avec la nouvelle version.

.PARAMETER Version
    Le numéro de version à ajouter (ex: 0.3.0)

.PARAMETER Date
    La date de la release (ex: "Janvier 2026"). Par défaut, c'est le mois/année actuel.

.PARAMETER Author
    L'auteur de la release. Par défaut "Thorinval".

.EXAMPLE
    .\Update-ReleaseNotes.ps1 -Version "0.3.0" -Date "Janvier 2026"

.EXAMPLE
    .\Update-ReleaseNotes.ps1 -Version "0.3.0"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Date = "$(Get-Culture).DateTimeFormat.GetMonthName($(Get-Date).Month) $(Get-Date).Year",
    
    [Parameter(Mandatory=$false)]
    [string]$Author = "Thorinval"
)

# Chemins des fichiers
$rootPath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$releaseNotesPath = Join-Path $rootPath "RELEASE_NOTES.md"
$appSettingsPath = Join-Path $rootPath "CharacterManager" "appsettings.json"
$csprojPath = Join-Path $rootPath "CharacterManager" "CharacterManager.csproj"

Write-Host "🚀 Mise à jour des Release Notes pour la version $Version" -ForegroundColor Green
Write-Host ""

# Vérifier que les fichiers existent
if (-not (Test-Path $releaseNotesPath)) {
    Write-Host "❌ Erreur: $releaseNotesPath n'existe pas" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $appSettingsPath)) {
    Write-Host "❌ Erreur: $appSettingsPath n'existe pas" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $csprojPath)) {
    Write-Host "❌ Erreur: $csprojPath n'existe pas" -ForegroundColor Red
    exit 1
}

# Valider le format de version
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "❌ Erreur: Format de version invalide. Utilisez X.Y.Z (ex: 0.3.0)" -ForegroundColor Red
    exit 1
}

# 1. Mettre à jour appsettings.json
Write-Host "📝 Mise à jour de appsettings.json..." -ForegroundColor Yellow
try {
    $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
    $appSettings.AppInfo.Version = $Version
    $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
    Write-Host "✅ appsettings.json mis à jour: Version = $Version" -ForegroundColor Green
} catch {
    Write-Host "❌ Erreur lors de la mise à jour de appsettings.json: $_" -ForegroundColor Red
    exit 1
}

# 2. Mettre à jour le .csproj
Write-Host "📝 Mise à jour de CharacterManager.csproj..." -ForegroundColor Yellow
try {
    $csproj = Get-Content $csprojPath
    $csproj = $csproj -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
    $csproj = $csproj -replace '<InformationalVersion>[^<]+</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>"
    $csproj | Set-Content $csprojPath
    Write-Host "✅ CharacterManager.csproj mis à jour: Version = $Version" -ForegroundColor Green
} catch {
    Write-Host "❌ Erreur lors de la mise à jour de CharacterManager.csproj: $_" -ForegroundColor Red
    exit 1
}

# 3. Ajouter une entrée dans RELEASE_NOTES.md
Write-Host "📝 Mise à jour de RELEASE_NOTES.md..." -ForegroundColor Yellow
try {
    $content = Get-Content $releaseNotesPath -Raw
    
    # Créer la nouvelle entrée de version
    $newEntry = @"
## Version $Version ($Date)

### ✨ Nouvelles Fonctionnalités

- [À remplir]

### 🔧 Améliorations Techniques

- [À remplir]

### 🐛 Corrections de Bugs

- [À remplir]

### 📋 Changements de l'Interface Utilisateur

[À remplir si applicable]

---

**Date de Release**: $Date  
**Version**: $Version  
**Auteur**: $Author

---

"@

    # Insérer après le premier ---
    $pattern = '---\s*(\r?\n)'
    $replacement = "---`n`n$newEntry"
    
    # Remplacer en trouvant la position du premier ---
    $splitPoint = $content.IndexOf("---")
    if ($splitPoint -eq -1) {
        Write-Host "❌ Erreur: Impossible de trouver le séparateur --- dans RELEASE_NOTES.md" -ForegroundColor Red
        exit 1
    }
    
    # Trouver la fin de la ligne du ---
    $endOfLine = $content.IndexOf("`n", $splitPoint)
    if ($endOfLine -eq -1) {
        $endOfLine = $content.Length
    } else {
        $endOfLine++ # Inclure la newline
    }
    
    # Insérer la nouvelle entrée
    $newContent = $content.Substring(0, $endOfLine) + "`n$newEntry" + $content.Substring($endOfLine)
    
    # Mettre à jour aussi le numéro de version en haut
    $newContent = $newContent -replace '> \*\*Version actuelle\*\*: [^\s]+', "> **Version actuelle**: $Version"
    
    Set-Content $releaseNotesPath $newContent
    Write-Host "✅ RELEASE_NOTES.md mis à jour avec une nouvelle entrée pour v$Version" -ForegroundColor Green
} catch {
    Write-Host "❌ Erreur lors de la mise à jour de RELEASE_NOTES.md: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Tous les fichiers ont été mis à jour avec succès!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Prochaines étapes:" -ForegroundColor Cyan
Write-Host "  1. Complétez les sections '[À remplir]' dans RELEASE_NOTES.md"
Write-Host "  2. Vérifiez les mises à jour:" -ForegroundColor Cyan
Write-Host "     - appsettings.json: Version = $Version"
Write-Host "     - CharacterManager.csproj: Version = $Version"
Write-Host "     - RELEASE_NOTES.md: Nouvelle entrée pour v$Version"
Write-Host "  3. Committez: git add . && git commit -m 'Préparer version $Version'"
Write-Host "  4. Taggez: git tag -a v$Version -m 'Version $Version'"
Write-Host "  5. Poussez: git push origin v$Version"
