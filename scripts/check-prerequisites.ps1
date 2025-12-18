#!/usr/bin/env pwsh
# Script de Vérification et Installation des Prérequis pour Déploiement Google Cloud
# Usage: .\check-prerequisites.ps1

param(
    [switch]$Install = $false
)

$script:ErrorCount = 0
$script:WarningCount = 0

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
    $script:ErrorCount++
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
    $script:WarningCount++
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Test-InstalledCommand {
    param(
        [string]$Command,
        [string]$DisplayName,
        [string]$MinVersion = "",
        [string]$InstallUrl = ""
    )
    
    Write-Host ""
    Write-Info "Vérification: $DisplayName"
    
    $cmd = Get-Command $Command -ErrorAction SilentlyContinue
    
    if ($cmd) {
        Write-Success "  Installé: $($cmd.Source)"
        
        # Vérifier la version si requise
        if ($MinVersion) {
            try {
                $versionOutput = & $Command --version 2>&1
                $version = [version]($versionOutput -split '\s+' | Select-Object -First 1)
                $minVer = [version]$MinVersion
                
                if ($version -ge $minVer) {
                    Write-Success "  Version: $version (requis: $MinVersion+)"
                    return $true
                }
                else {
                    Write-Error-Custom "  Version: $version (requis: $MinVersion+)"
                    return $false
                }
            }
            catch {
                Write-Warning-Custom "  Impossible de vérifier la version"
                return $true  # Continuer quand même
            }
        }
        
        return $true
    }
    else {
        Write-Error-Custom "  Non trouvé"
        
        if ($InstallUrl -and $Install) {
            Write-Info "  Installation depuis: $InstallUrl"
            try {
                # Logique d'installation simple (à adapter)
                Write-Warning-Custom "  Installation automatique non disponible"
                Write-Warning-Custom "  Veuillez installer manuellement: $InstallUrl"
            }
            catch {
                Write-Error-Custom "  Erreur lors de l'installation: $_"
            }
        }
        elseif ($InstallUrl) {
            Write-Info "  Installer depuis: $InstallUrl"
        }
        
        return $false
    }
}

function Test-EnvironmentPrerequisites {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "🔍 Vérification des Prérequis - Déploiement Google Cloud" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    $results = @{}
    
    # Vérification des outils essentiels
    Write-Host ""
    Write-Host "📋 Outils Essentiels" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $results.gcloud = Test-InstalledCommand "gcloud" "Google Cloud SDK" "450.0" "https://cloud.google.com/sdk/docs/install"
    $results.dotnet = Test-InstalledCommand "dotnet" ".NET CLI" "9.0" "https://dotnet.microsoft.com/en-us/download/dotnet/9.0"
    
    Write-Host ""
    Write-Info "Note: Docker peut utiliser Google Cloud Build au lieu de Docker local"
    $results.docker = Test-InstalledCommand "docker" "Docker (optionnel pour build local)" "20.0" "https://www.docker.com/products/docker-desktop"
    
    # Vérification des outils optionnels
    Write-Host ""
    Write-Host "📦 Outils Optionnels" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $results.git = Test-InstalledCommand "git" "Git" "2.0" "https://git-scm.com/download/win"
    $results.terraform = Test-InstalledCommand "terraform" "Terraform" "1.0" "https://www.terraform.io/downloads.html"
    $results.node = Test-InstalledCommand "node" "Node.js (optionnel)" "16.0" "https://nodejs.org/en/download/"
    
    # Vérification de la configuration GCP
    Write-Host ""
    Write-Host "☁️  Google Cloud Configuration" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    if ($results.gcloud) {
        try {
            $project = gcloud config get-value project 2>&1
            if ($project -and $project -ne "null") {
                Write-Success "  Projet actif: $project"
            }
            else {
                Write-Warning-Custom "  Aucun projet GCP configuré"
                Write-Info "  Exécuter: gcloud init"
                $results.gcp_project = $false
            }
            
            # Vérifier l'authentification
            $auth = gcloud auth list 2>&1 | Select-String "ACTIVE"
            if ($auth) {
                Write-Success "  Authentifié: $auth"
            }
            else {
                Write-Error-Custom "  Non authentifié"
                Write-Info "  Exécuter: gcloud auth login"
                $results.gcp_auth = $false
            }
        }
        catch {
            Write-Warning-Custom "  Impossible de vérifier la configuration GCP"
        }
    }
    
    # Vérification des ports
    Write-Host ""
    Write-Host "🔌 Ports Réseau" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $ports = @(5269, 80, 443, 8080)
    foreach ($port in $ports) {
        try {
            $connection = Test-NetConnection -ComputerName 127.0.0.1 -Port $port -ErrorAction SilentlyContinue
            if ($connection.TcpTestSucceeded) {
                Write-Warning-Custom "  Port $port déjà utilisé"
            }
            else {
                Write-Success "  Port $port disponible"
            }
        }
        catch {
            Write-Success "  Port $port disponible"
        }
    }
    
    # Vérification de l'espace disque
    Write-Host ""
    Write-Host "💾 Espace Disque" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $drive = Get-PSDrive C -ErrorAction SilentlyContinue
    if ($drive) {
        $freeGB = [math]::Round($drive.Free / 1GB, 2)
        $totalGB = [math]::Round($drive.Used / 1GB + $drive.Free / 1GB, 2)
        
        if ($drive.Free -gt 10GB) {
            Write-Success "  Espace libre: $freeGB GB (total: $totalGB GB)"
        }
        else {
            Write-Warning-Custom "  Espace libre: $freeGB GB (recommandé: 10+ GB)"
        }
    }
    
    # Résumé
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "📊 Résumé" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    $passed = ($results.Values | Where-Object { $_ -eq $true }).Count
    $total = $results.Count
    
    Write-Host ""
    Write-Info "Vérifications: $passed/$total réussies"
    Write-Info "Avertissements: $WarningCount"
    
    if ($script:ErrorCount -eq 0) {
        Write-Success "✅ Tous les prérequis sont satisfaits!"
        Write-Info ""
        Write-Info "Prochaines étapes:"
        Write-Info "  1. Configurer le projet GCP: gcloud init"
        Write-Info "  2. Lancer le déploiement: .\scripts\Deploy-GoogleCloud.ps1"
        return $true
    }
    else {
        Write-Error-Custom "❌ $ErrorCount erreur(s) détectée(s)"
        Write-Info ""
        Write-Info "À faire:"
        Write-Info "  1. Installer les outils manquants (voir liens ci-dessus)"
        Write-Info "  2. Vérifier la configuration GCP"
        Write-Info "  3. Re-exécuter cette vérification"
        Write-Info ""
        Write-Info "Pour plus d'aide: https://cloud.google.com/docs"
        return $false
    }
}

# Affichage du menu d'aide
function Show-Help {
    Write-Host ""
    Write-Host "📖 Guide des Prérequis" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔴 Obligatoires:" -ForegroundColor Red
    Write-Host "  • Google Cloud SDK 450.0+"
    Write-Host "  • Docker Desktop 20.0+"
    Write-Host "  • .NET CLI 9.0+"
    Write-Host ""
    Write-Host "🟡 Fortement Recommandés:" -ForegroundColor Yellow
    Write-Host "  • Git 2.0+"
    Write-Host "  • Terraform 1.0+ (pour IaC)"
    Write-Host ""
    Write-Host "🟢 Optionnels:" -ForegroundColor Green
    Write-Host "  • Node.js 16.0+ (pour development tools)"
    Write-Host "  • VS Code (pour éditer config)"
    Write-Host ""
}

# Exécution principale
$success = Test-EnvironmentPrerequisites

Show-Help

Write-Host ""

if ($success) {
    exit 0
}
else {
    exit 1
}
