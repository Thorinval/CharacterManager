# Script PowerShell pour publier et préparer le setup
# Usage: .\Publish-Setup.ps1

param(
    [string]$Version = "0.12.0",
    [string]$Configuration = "Release"
)

Write-Host "Character Manager - Setup Builder" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host ""

# Définir les chemins
$projectPath = Get-Location
$publishDir = Join-Path $projectPath "CharacterManager\bin\$Configuration\net9.0\publish"
$installerDir = Join-Path $projectPath "publish\installer"
$issFile = Join-Path $projectPath "CharacterManager.iss"

Write-Host "📦 Étape 1: Nettoyage des anciennes publications..." -ForegroundColor Yellow
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
    Write-Host "✓ Dossier publication nettoyé" -ForegroundColor Green
}

Write-Host ""
Write-Host "🔨 Étape 2: Publication de l'application..." -ForegroundColor Yellow
Push-Location CharacterManager
dotnet publish -c $Configuration --self-contained
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Publication réussie" -ForegroundColor Green
} else {
    Write-Host "✗ Erreur lors de la publication" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📁 Étape 3: Création du dossier installer..." -ForegroundColor Yellow
if (-not (Test-Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
    Write-Host "✓ Dossier créé: $installerDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "✅ Préparation complète!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Prochaines étapes:" -ForegroundColor Cyan
Write-Host "1. Installer Inno Setup depuis: https://jrsoftware.org/isdl.php"
Write-Host "2. Ouvrir le fichier: $issFile"
Write-Host "3. Compiler le setup: Build > Compile"
Write-Host ""
Write-Host "💾 L'installateur sera généré dans: $installerDir" -ForegroundColor Cyan
