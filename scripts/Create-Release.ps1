#!/usr/bin/env pwsh
<#
.SYNOPSIS
Crée une nouvelle version de Character Manager - Automatise tout le processus de release

.DESCRIPTION
Ce script automatise le processus complet de création d'une nouvelle version:
1. Incrémente le numéro de version (patch, minor, ou major)
2. Synchronise la version avec Inno Setup
3. Publie l'application
4. Compile l'installateur

.PARAMETER VersionType
Type d'incrémentation de version: 'patch' (défaut), 'minor', ou 'major'
- patch: 0.12.0 → 0.12.1 (corrections)
- minor: 0.12.0 → 0.13.0 (nouvelles fonctionnalités)
- major: 0.12.0 → 1.0.0 (ruptures majeures)

.EXAMPLE
.\Create-Release.ps1
# Incrémente patch (défaut)

.\Create-Release.ps1 -VersionType minor
# Incrémente minor

.\Create-Release.ps1 -VersionType major
# Incrémente major

.NOTES
Prérequis:
- Inno Setup 6 doit être installé
- Git doit être configuré
- Dotnet SDK 9.0+

Le script s'arrête à la première erreur et affiche les détails.
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('patch', 'minor', 'major')]
    [string]$VersionType = 'patch'
)

$ErrorActionPreference = 'Stop'

# Configuration
$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$rootDir = Split-Path -Parent -Path $scriptDir
$scripts = @(
    @{
        Name = "Incrementer la version";
        Script = "$scriptDir\Increment-Version.ps1";
        Args = @($VersionType)
    },
    @{
        Name = "Synchroniser version Inno Setup";
        Script = "$scriptDir\Sync-InnoSetupVersion.ps1";
        Args = @()
    },
    @{
        Name = "Publier l'application";
        Script = "$scriptDir\publish.ps1";
        Args = @()
    },
    @{
        Name = "Compiler l'installateur";
        Script = "$scriptDir\Build-Installer.ps1";
        Args = @()
    }
)

# Couleurs
$colors = @{
    Header = 'Cyan'
    Success = 'Green'
    Error = 'Red'
    Warning = 'Yellow'
    Info = 'White'
}

function Write-StyledOutput {
    param(
        [string]$Message,
        [string]$Color = 'White',
        [switch]$Header,
        [switch]$Blank
    )
    
    if ($Blank) {
        Write-Host ""
        return
    }
    
    if ($Header) {
        Write-Host "================================================================" -ForegroundColor $Color
        Write-Host $Message -ForegroundColor $Color
        Write-Host "================================================================" -ForegroundColor $Color
    }
    else {
        Write-Host $Message -ForegroundColor $Color
    }
}

function Get-CurrentVersion {
    $appsettingsPath = Join-Path $rootDir "CharacterManager\appsettings.json"
    if (Test-Path $appsettingsPath) {
        $content = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        return $content.AppInfo.Version
    }
    return "inconnue"
}

# Start
Clear-Host
Write-StyledOutput "[RELEASE] CREATION D'UNE NOUVELLE VERSION" -Header -Color $colors.Header
Write-Host ""

# Afficher les informations
$currentVersion = Get-CurrentVersion
Write-Host "[INFO] Informations:" -ForegroundColor $colors.Info
Write-Host "  Repertoire: $rootDir" -ForegroundColor $colors.Info
Write-Host "  Version actuelle: $currentVersion" -ForegroundColor $colors.Info
Write-Host "  Type d'incrementation: $VersionType" -ForegroundColor $colors.Info
Write-Host ""

# Exécuter les scripts
$successCount = 0
$errorScript = $null

foreach ($script in $scripts) {
    Write-StyledOutput "[$($successCount + 1)/$($scripts.Count)] $($script.Name)..." -Color $colors.Header
    Write-Host ""
    
    try {
        Push-Location $scriptDir
        
        # Construire la commande
        $cmd = "& '$($script.Script)'"
        if ($script.Args.Count -gt 0) {
            $argsStr = ($script.Args | ForEach-Object { "'$_'" }) -join ' '
            $cmd += " $argsStr"
        }
        
        # Exécuter
        Invoke-Expression $cmd
        
        Pop-Location
        
        Write-StyledOutput "[OK] $($script.Name) - Succes" -Color $colors.Success
        Write-StyledOutput "" -Blank
        $successCount++
    }
    catch {
        Pop-Location
        Write-StyledOutput "[ERREUR] $($script.Name)" -Color $colors.Error
        Write-Host ""
        Write-Host $_.Exception.Message -ForegroundColor $colors.Error
        Write-Host ""
        $errorScript = $script.Name
        break
    }
}

# Résumé final
Write-StyledOutput "" -Blank
if ($successCount -eq $scripts.Count) {
    Write-StyledOutput "[SUCCES] NOUVELLE VERSION CREEE" -Header -Color $colors.Success
    Write-Host ""
    Write-Host "[ARTEFACTS] Fichiers generes:" -ForegroundColor $colors.Success
    Write-Host "  OK Version incrementee" -ForegroundColor $colors.Success
    Write-Host "  OK Inno Setup synchronise" -ForegroundColor $colors.Success
    Write-Host "  OK Application publiee" -ForegroundColor $colors.Success
    Write-Host "  OK Installateur compile" -ForegroundColor $colors.Success
    Write-Host ""
    $newVersion = Get-CurrentVersion
    Write-Host "Nouvelle version: $newVersion" -ForegroundColor $colors.Success
    Write-Host ""
    Write-Host "[DOSSIERS] Fichiers disponibles a:" -ForegroundColor $colors.Success
    Write-Host "  publish/ - Application publiee" -ForegroundColor $colors.Success
    Write-Host "  publish/installer/ - Installateur Windows" -ForegroundColor $colors.Success
    Write-Host ""
}
else {
    Write-StyledOutput "[ERREUR] ERREUR LORS DE L'EXECUTION" -Header -Color $colors.Error
    Write-Host ""
    Write-Host "Etapes completees: $successCount/$($scripts.Count)" -ForegroundColor $colors.Warning
    Write-Host "Erreur a: $errorScript" -ForegroundColor $colors.Warning
    Write-Host ""
    Write-Host "Pour corriger et relancer:" -ForegroundColor $colors.Warning
    Write-Host "1. Verifiez les erreurs ci-dessus" -ForegroundColor $colors.Warning
    Write-Host "2. Corrigez les problemes" -ForegroundColor $colors.Warning
    Write-Host "3. Relancez: .\Create-Release.ps1 -VersionType $VersionType" -ForegroundColor $colors.Warning
    Write-Host ""
    exit 1
}

exit 0
