# Script de publication de Character Manager
# Ce script crée un package déployable autonome

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = ".\publish"
)

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
dotnet publish .\CharacterManager\CharacterManager.csproj `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Publication réussie!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Fichiers publiés dans: $OutputPath" -ForegroundColor Cyan
    
    # Créer un fichier ZIP pour distribution
    $version = (Get-Content .\CharacterManager\appsettings.json | ConvertFrom-Json).AppInfo.Version
    $zipName = "CharacterManager-v$version-$Runtime.zip"
    
    Write-Host ""
    Write-Host "Création de l'archive de distribution..." -ForegroundColor Yellow
    
    if (Test-Path $zipName) {
        Remove-Item $zipName -Force
    }
    
    Compress-Archive -Path "$OutputPath\*" -DestinationPath $zipName
    
    Write-Host "✓ Archive créée: $zipName" -ForegroundColor Green
    Write-Host ""
    Write-Host "Taille de l'archive: $([math]::Round((Get-Item $zipName).Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Pour déployer:" -ForegroundColor Yellow
    Write-Host "  1. Extraire $zipName sur la machine cible" -ForegroundColor White
    Write-Host "  2. Exécuter CharacterManager.exe" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "✗ Erreur lors de la publication" -ForegroundColor Red
    exit 1
}
