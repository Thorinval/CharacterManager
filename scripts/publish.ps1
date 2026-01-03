# Script de publication de Character Manager
# Ce script crée un package déployable autonome

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = $null
)

# Déterminer le répertoire racine (parent du dossier scripts)
$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent -Path $scriptDir

# Définir le chemin de sortie par défaut si non fourni
if (-not $OutputPath) {
    $OutputPath = Join-Path $rootDir "publish"
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Character Manager - Publication" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Nettoyer le dossier de sortie
if (Test-Path $OutputPath) {
    Write-Host "Nettoyage du dossier de publication..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

Write-Host "Configuration: $Configuration" -ForegroundColor Green
Write-Host "Runtime: $Runtime" -ForegroundColor Green
Write-Host "Dossier de sortie: $OutputPath" -ForegroundColor Green
Write-Host ""

# Publier l'application
Write-Host "Publication de l'application..." -ForegroundColor Yellow
$csprojPath = Join-Path $rootDir "CharacterManager\CharacterManager.csproj"
dotnet publish $csprojPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "[OK] Publication reussie!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Fichiers publiés dans: $OutputPath" -ForegroundColor Cyan
    
    # Créer un fichier ZIP pour distribution
    $appSettingsPath = Join-Path $rootDir "CharacterManager\appsettings.json"
    $version = (Get-Content $appSettingsPath | ConvertFrom-Json).AppInfo.Version
    $zipName = "CharacterManager-v$version-$Runtime.zip"
    
    Write-Host ""
    Write-Host "Création de l'archive de distribution..." -ForegroundColor Yellow
    
    if (Test-Path $zipName) {
        Remove-Item $zipName -Force
    }
    
    Compress-Archive -Path "$OutputPath\*" -DestinationPath $zipName
    
    Write-Host "[OK] Archive creee: $zipName" -ForegroundColor Green
    Write-Host ""
    Write-Host "Taille de l'archive: $([math]::Round((Get-Item $zipName).Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Pour déployer:" -ForegroundColor Yellow
    Write-Host "  1. Extraire $zipName sur la machine cible" -ForegroundColor White
    Write-Host "  2. Exécuter CharacterManager.exe" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "[ERREUR] Erreur lors de la publication" -ForegroundColor Red
    exit 1
}
