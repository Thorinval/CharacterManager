# Script de Déploiement Google Cloud pour Character Manager
# Usage: .\Deploy-GoogleCloud.ps1 -ProjectId "character-manager-prod" -Region "europe-west1" -DeploymentType "CloudRun"

param(
    [string]$ProjectId = "character-manager-prod",
    [string]$Region = "europe-west1",
    [ValidateSet("CloudRun", "ComputeEngine")]
    [string]$DeploymentType = "CloudRun",
    [string]$ServiceName = "character-manager",
    [string]$ImageName = "app"
)

$ErrorActionPreference = "Stop"

# Couleurs pour l'output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Blue = [System.ConsoleColor]::Cyan

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )
    
    $timestamp = Get-Date -Format "HH:mm:ss"
    $color = @{
        "Info"    = $Blue
        "Success" = $Green
        "Warning" = $Yellow
        "Error"   = $Red
    }[$Level]
    
    Write-Host "[$timestamp] " -NoNewline -ForegroundColor Gray
    Write-Host "$Message" -ForegroundColor $color
}

function Test-Prerequisites {
    Write-Log "🔍 Vérification des prérequis..." "Info"
    
    $missingTools = @()
    
    if (-not (Get-Command gcloud -ErrorAction SilentlyContinue)) {
        $missingTools += "gcloud"
    }
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        $missingTools += "docker"
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        $missingTools += "dotnet"
    }
    
    if ($missingTools.Count -gt 0) {
        Write-Log "❌ Outils manquants: $($missingTools -join ', ')" "Error"
        Write-Log "   Veuillez installer: $($missingTools -join ', ')" "Info"
        exit 1
    }
    
    Write-Log "✅ Tous les prérequis sont installés" "Success"
}

function Setup-GCPProject {
    Write-Log "📋 Configuration du projet GCP..." "Info"
    
    # Vérifier si le projet existe
    $projects = gcloud projects list --format="value(project_id)" | Select-String "^$ProjectId$"
    
    if (-not $projects) {
        Write-Log "📝 Création du projet GCP: $ProjectId" "Info"
        gcloud projects create $ProjectId --name="Character Manager Production" --set-as-default
        
        # Attendre que le projet soit créé
        Start-Sleep -Seconds 5
    }
    else {
        Write-Log "✅ Projet existant détecté: $ProjectId" "Success"
        gcloud config set project $ProjectId
    }
    
    # Activer les APIs
    Write-Log "🔧 Activation des APIs nécessaires..." "Info"
    
    $apis = @(
        "run.googleapis.com",
        "artifactregistry.googleapis.com",
        "sqladmin.googleapis.com",
        "containerregistry.googleapis.com"
    )
    
    foreach ($api in $apis) {
        Write-Log "   Activation: $api" "Info"
        gcloud services enable $api --quiet
    }
    
    Write-Log "✅ APIs activées" "Success"
}

function Setup-ArtifactRegistry {
    Write-Log "📦 Configuration de l'Artifact Registry..." "Info"
    
    # Vérifier si le repository existe
    $repos = gcloud artifacts repositories list --location=$Region --format="value(name)" | Select-String "^$ServiceName$"
    
    if (-not $repos) {
        Write-Log "📝 Création du repository: $ServiceName" "Info"
        gcloud artifacts repositories create $ServiceName `
            --repository-format=docker `
            --location=$Region `
            --description="Character Manager Docker Images" `
            --quiet
    }
    else {
        Write-Log "✅ Repository existant: $ServiceName" "Success"
    }
    
    # Configurer Docker
    Write-Log "🐳 Configuration de Docker..." "Info"
    gcloud auth configure-docker "$Region-docker.pkg.dev" --quiet
    
    Write-Log "✅ Artifact Registry prêt" "Success"
}

function Build-DotNetApp {
    Write-Log "🏗️  Build de l'application .NET..." "Info"
    
    $appPath = Join-Path (Split-Path $PSScriptRoot -Parent) "CharacterManager" "CharacterManager.csproj"
    
    if (-not (Test-Path $appPath)) {
        Write-Log "❌ Fichier projet non trouvé: $appPath" "Error"
        exit 1
    }
    
    dotnet publish $appPath `
        --configuration Release `
        --output publish `
        --no-self-contained
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Erreur lors de la compilation" "Error"
        exit 1
    }
    
    Write-Log "✅ Application compilée" "Success"
}

function Build-DockerImage {
    Write-Log "🐳 Construction de l'image Docker..." "Info"
    
    $imageUri = "$Region-docker.pkg.dev/$ProjectId/$ServiceName/$ImageName"
    $imageTag = "$imageUri:latest"
    
    # Vérifier si Docker est disponible
    $dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
    
    if (-not $dockerAvailable) {
        Write-Log "ℹ️  Docker non trouvé, utilisation de Google Cloud Build" "Info"
        
        # Utiliser Cloud Build
        gcloud builds submit --tag $imageTag --project=$ProjectId
        
        if ($LASTEXITCODE -ne 0) {
            Write-Log "❌ Erreur lors de la construction avec Cloud Build" "Error"
            exit 1
        }
        
        Write-Log "✅ Image construite avec Cloud Build: $imageTag" "Success"
        return $imageTag
    }
    
    # Build avec Docker local
    $dockerfilePath = Join-Path (Split-Path $PSScriptRoot -Parent) "Dockerfile"
    
    if (-not (Test-Path $dockerfilePath)) {
        Write-Log "❌ Dockerfile non trouvé" "Error"
        exit 1
    }
    
    docker build -t $imageTag -f $dockerfilePath .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Erreur lors de la construction Docker" "Error"
        exit 1
    }
    
    Write-Log "✅ Image Docker construite: $imageTag" "Success"
    return $imageTag
}

function Push-DockerImage {
    param([string]$ImageTag)
    
    # Si Cloud Build a été utilisé, l'image est déjà pushée
    $dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerAvailable) {
        Write-Log "✅ Image déjà dans Artifact Registry (Cloud Build)" "Success"
        return
    }
    
    Write-Log "⬆️  Envoi de l'image vers Artifact Registry..." "Info"
    
    docker push $ImageTag
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Erreur lors de l'envoi de l'image" "Error"
        exit 1
    }
    
    Write-Log "✅ Image envoyée" "Success"
}

function Deploy-CloudRun {
    param([string]$ImageTag)
    
    Write-Log "🚀 Déploiement sur Cloud Run..." "Info"
    
    gcloud run deploy $ServiceName `
        --image=$ImageTag `
        --region=$Region `
        --platform=managed `
        --allow-unauthenticated `
        --memory=512Mi `
        --cpu=1 `
        --timeout=3600 `
        --set-env-vars="ASPNETCORE_ENVIRONMENT=Production" `
        --quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Erreur lors du déploiement Cloud Run" "Error"
        exit 1
    }
    
    # Récupérer l'URL du service
    $serviceUrl = gcloud run services describe $ServiceName `
        --region=$Region `
        --format="value(status.url)"
    
    Write-Log "✅ Déploiement Cloud Run réussi" "Success"
    Write-Log "🌐 URL de l'application: $serviceUrl" "Success"
}

function Show-Completion {
    Write-Log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Info"
    Write-Log "✅ DÉPLOIEMENT COMPLÉTÉ AVEC SUCCÈS" "Success"
    Write-Log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Info"
    Write-Log "" "Info"
    Write-Log "📊 Informations du Déploiement:" "Info"
    Write-Log "   Projet GCP: $ProjectId" "Info"
    Write-Log "   Service: $ServiceName" "Info"
    Write-Log "   Région: $Region" "Info"
    Write-Log "   Type: $DeploymentType" "Info"
    Write-Log "" "Info"
    Write-Log "Prochaines Étapes:" "Info"
    Write-Log "   1. Vérifier les logs: gcloud logging read --limit 50" "Info"
    Write-Log "   2. Ouvrir le dashboard: gcloud console" "Info"
    Write-Log "   3. Ajouter un domaine personnalisé (optionnel)" "Info"
}

# Exécution principale
try {
    Write-Log "════════════════════════════════════════════════════════" "Info"
    Write-Log "🌐 Character Manager - Google Cloud Deployment Script" "Info"
    Write-Log "════════════════════════════════════════════════════════" "Info"
    Write-Log "" "Info"
    
    # Phase 1: Vérifications
    Test-Prerequisites
    Write-Log "" "Info"
    
    # Phase 2: Configuration GCP
    Setup-GCPProject
    Write-Log "" "Info"
    
    # Phase 3: Setup Artifact Registry
    Setup-ArtifactRegistry
    Write-Log "" "Info"
    
    # Phase 4: Build
    Build-DotNetApp
    Write-Log "" "Info"
    
    # Phase 5: Docker
    $imageTag = Build-DockerImage
    Push-DockerImage $imageTag
    Write-Log "" "Info"
    
    # Phase 6: Déploiement
    if ($DeploymentType -eq "CloudRun") {
        Deploy-CloudRun $imageTag
    }
    
    Write-Log "" "Info"
    Show-Completion
}
catch {
    Write-Log "❌ Erreur: $_" "Error"
    exit 1
}
