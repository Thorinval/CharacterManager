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
$newEntry = @"
## Version $Version ($Date)

### Nouvelles Fonctionnalites

#- Roadmap integree dans l'application pour suivre les futures fonctionnalites et ameliorations.
#- Information changelog ajoutee

### Ameliorations Techniques

#- aucune

### Corrections de Bugs

#- aucun

### Changements de l'Interface Utilisateur

#- aucun

#---

**Date de Release**: $Date  
**Version**: $Version  
**Auteur**: $Author

#---
"@
    # Insérer la nouvelle entrée juste après le premier séparateur ---
    $splitPoint = $content.IndexOf("---")
    if ($splitPoint -eq -1) {
        Write-Host "ERREUR: Impossible de trouver le separateur --- dans RELEASE_NOTES.md" -ForegroundColor Red
        exit 1
    }
    # Trouver la fin de la ligne du ---
    $endOfLine = $content.IndexOf("`n", $splitPoint)
    if ($endOfLine -eq -1) {
    } else {
    }
    
    # Insérer la nouvelle entrée
    $newContent = $content.Substring(0, $endOfLine) + "`n$newEntry" + $content.Substring($endOfLine)
    
    $newContent = $newContent -replace '> \*\*Version actuelle\*\*: [^\s]+', "> **Version actuelle**: $Version"

    $utf8NoBomEncoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($releaseNotesPath, $newContent, $utf8NoBomEncoding)
    Write-Host "RELEASE_NOTES.md mis a jour avec une nouvelle entree pour v$Version" -ForegroundColor Green
catch {
    Write-Host "❌ Erreur lors de la mise à jour de RELEASE_NOTES.md: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Tous les fichiers ont ete mis a jour avec succes!" -ForegroundColor Green
Write-Host ""
Write-Host "Prochaines etapes:" -ForegroundColor Cyan
Write-Host "  1. Completez les sections '[A remplir]' dans RELEASE_NOTES.md"
Write-Host "  2. Verifiez les mises a jour:" -ForegroundColor Cyan
Write-Host "     - appsettings.json: Version = $Version"
Write-Host "     - CharacterManager.csproj: Version = $Version"
Write-Host "     - RELEASE_NOTES.md: Nouvelle entree pour v$Version"
Write-Host "  3. Committez: git add . && git commit -m 'Preparer version $Version'"
Write-Host "  4. Taggez: git tag -a v$Version -m 'Version $Version'"
Write-Host "  5. Poussez: git push origin v$Version"
