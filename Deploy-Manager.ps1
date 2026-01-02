# Script PowerShell complet de gestion du cycle de déploiement
# Usage:
#   .\Deploy-Manager.ps1 -Action build              # Compiler en Release
#   .\Deploy-Manager.ps1 -Action publish            # Publier l'application
#   .\Deploy-Manager.ps1 -Action installer          # Créer l'installateur
#   .\Deploy-Manager.ps1 -Action run                # Lancer l'app localement
#   .\Deploy-Manager.ps1 -Action test               # Exécuter les tests
#   .\Deploy-Manager.ps1 -Action all                # Faire tout: build, test, publish, installer

param(
    [ValidateSet('build', 'publish', 'installer', 'run', 'test', 'clean', 'all')]
    [string]$Action = 'build',
    
    [int]$Port = 5000,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Couleurs
function Write-Title { Write-Host $args -ForegroundColor Cyan -BackgroundColor Black }
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error_ { Write-Host $args -ForegroundColor Red }

# Chemins
$projectRoot = Get-Location
$solutionFile = Join-Path $projectRoot "CharacterManager.sln"
$csprojFile = Join-Path $projectRoot "CharacterManager\CharacterManager.csproj"
$issFile = Join-Path $projectRoot "CharacterManager.iss"

function Test-Prerequisites {
    Write-Title "`n🔍 Vérification des prérequis..."
    
    # Vérifier .NET
    $dotnetVersion = dotnet --version
    Write-Success "✓ .NET SDK: $dotnetVersion"
    
    # Vérifier la solution
    if (-not (Test-Path $solutionFile)) {
        Write-Error_ "✗ Fichier solution non trouvé: $solutionFile"
        exit 1
    }
    Write-Success "✓ Solution trouvée"
    
    # Vérifier Inno Setup (si nécessaire)
    if ($Action -eq 'installer' -or $Action -eq 'all') {
        $issPath = "C:\Program Files (x86)\Inno Setup 6"
        if (-not (Test-Path "$issPath\iscc.exe")) {
            Write-Warning "⚠ Inno Setup 6 non trouvé. Vous pouvez le télécharger à: https://jrsoftware.org/"
            Write-Warning "⚠ Installez-le pour compiler l'installateur"
        } else {
            Write-Success "✓ Inno Setup 6 trouvé"
        }
    }
}

function Invoke-Build {
    Write-Title "`n🔨 Compilation en $Configuration..."
    
    dotnet build $solutionFile -c $Configuration
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Compilation réussie"
    } else {
        Write-Error_ "✗ Erreur de compilation"
        exit 1
    }
}

function Invoke-Tests {
    Write-Title "`n🧪 Exécution des tests..."
    
    dotnet test $solutionFile -c $Configuration --logger "console;verbosity=minimal"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Tous les tests sont passés"
    } else {
        Write-Error_ "✗ Des tests ont échoué"
        exit 1
    }
}

function Invoke-Publish {
    Write-Title "`n📦 Publication de l'application..."
    
    $publishPath = Join-Path $projectRoot "publish"
    
    Push-Location (Join-Path $projectRoot "CharacterManager")
    dotnet publish -c $Configuration --self-contained -o $publishPath
    Pop-Location
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Publication réussie dans: $publishPath"
    } else {
        Write-Error_ "✗ Erreur lors de la publication"
        exit 1
    }
}

function Invoke-Installer {
    Write-Title "`n📋 Préparation de l'installateur Inno Setup..."
    
    $issPath = "C:\Program Files (x86)\Inno Setup 6\iscc.exe"
    
    if (-not (Test-Path $issPath)) {
        Write-Error_ "✗ Inno Setup 6 n'est pas installé"
        Write-Warning "Téléchargez-le depuis: https://jrsoftware.org/isdl.php"
        exit 1
    }
    
    Write-Host "Compilation du script Inno Setup..."
    & $issPath $issFile
    
    if ($LASTEXITCODE -eq 0) {
        $installerFile = Join-Path $projectRoot "publish\installer\CharacterManager-0.12.0-Setup.exe"
        Write-Success "✓ Installateur créé: $installerFile"
    } else {
        Write-Error_ "✗ Erreur lors de la compilation du setup"
        exit 1
    }
}

function Invoke-Run {
    Write-Title "`n🚀 Lancement de l'application..."
    
    $appPath = Join-Path $projectRoot "CharacterManager\bin\$Configuration\net9.0\CharacterManager.exe"
    
    if (-not (Test-Path $appPath)) {
        Write-Warning "Application non compilée. Compilation en cours..."
        Invoke-Build
    }
    
    Write-Host "`nApplication démarrée sur: http://localhost:$Port" -ForegroundColor Green
    Write-Host "Appuyez sur Ctrl+C pour arrêter`n" -ForegroundColor Yellow
    
    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    & $appPath
}

function Invoke-Clean {
    Write-Title "`n🧹 Nettoyage..."
    
    $dirs = @(
        (Join-Path $projectRoot "CharacterManager\bin"),
        (Join-Path $projectRoot "CharacterManager\obj"),
        (Join-Path $projectRoot "publish"),
        (Join-Path $projectRoot "CharacterManager.Tests\bin"),
        (Join-Path $projectRoot "CharacterManager.Tests\obj"),
        (Join-Path $projectRoot "CharacterManager.Resources\bin"),
        (Join-Path $projectRoot "CharacterManager.Resources\obj"),
        (Join-Path $projectRoot "CharacterManager.Resources.Interface\bin"),
        (Join-Path $projectRoot "CharacterManager.Resources.Interface\obj")
    )
    
    foreach ($dir in $dirs) {
        if (Test-Path $dir) {
            Remove-Item $dir -Recurse -Force
            Write-Host "Supprimé: $dir" -ForegroundColor DarkGray
        }
    }
    
    Write-Success "✓ Nettoyage terminé"
}

# Script principal
switch ($Action) {
    'build' {
        Test-Prerequisites
        Invoke-Build
    }
    'publish' {
        Test-Prerequisites
        Invoke-Build
        Invoke-Tests
        Invoke-Publish
    }
    'installer' {
        Test-Prerequisites
        Invoke-Publish
        Invoke-Installer
    }
    'run' {
        Test-Prerequisites
        Invoke-Run
    }
    'test' {
        Test-Prerequisites
        Invoke-Build
        Invoke-Tests
    }
    'clean' {
        Invoke-Clean
    }
    'all' {
        Test-Prerequisites
        Invoke-Build
        Invoke-Tests
        Invoke-Publish
        Invoke-Installer
        Write-Title "`n✅ Pipeline de déploiement complet réussi!"
    }
}

Write-Host ""
