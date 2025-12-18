# Guide de Déploiement - Character Manager

> ✨ **Guide complet** pour déployer Character Manager localement ou sur Google Cloud avec accès à distance

## 📋 Table des Matières

1. [Déploiement Local](#déploiement-local)
2. [Déploiement Docker](#déploiement-docker)
3. [Déploiement Google Cloud](#déploiement-google-cloud)
4. [Accès à Distance](#accès-à-distance)
5. [Monitoring & Logs](#monitoring--logs)

---

## Déploiement Local

### Windows (Standalone)

```powershell
# Utiliser le script fourni (recommandé)
.\publish.ps1

# Ou manuellement
dotnet publish CharacterManager/CharacterManager.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output publish
```

Accès : `http://localhost:5269`

### Linux

```bash
# Publication Linux x64
dotnet publish CharacterManager/CharacterManager.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --output publish

# Rendre exécutable et lancer
chmod +x publish/CharacterManager
./publish/CharacterManager
```

---

## Déploiement Docker

### Construction de l'image

```bash
docker build -t character-manager:latest .
```

### Lancement avec docker run

```bash
docker run -d \
  --name character-manager \
  -p 5269:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/CharacterManager/wwwroot/images:/app/wwwroot/images \
  character-manager:latest
```

### Lancement avec docker-compose

```bash
docker-compose up -d

# Arrêter
docker-compose down
```

Accès : `http://localhost:5269`

### Variables d'Environnement Docker

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/CharacterManager.db"
```

---

## Déploiement Google Cloud

### 🚀 Prérequis

1. **Compte Google Cloud** : <https://console.cloud.google.com/>
2. **Google Cloud SDK** : <https://cloud.google.com/sdk/docs/install>
3. **dotnet CLI 9.0+**
4. **Docker Desktop** (optionnel, seulement pour build local)

Vérification :

```bash
gcloud --version          # Google Cloud SDK
dotnet --version          # .NET 9.0+

# Optionnel (si build local)
docker --version          # Docker Desktop
```

**Note** : Docker n'est pas obligatoire. Vous pouvez utiliser Google Cloud Build pour construire les images directement dans le cloud.

---

### Option A : Cloud Run (Recommandé pour Démarrage)

**Meilleur pour** : Applications petites à moyennes, auto-scaling, coûts faibles

**Avantages** :

- ✅ Sans serveur (serverless)
- ✅ Auto-scaling automatique
- ✅ Gratuit jusqu'à 2M requêtes/mois
- ✅ Certificat SSL inclus

**Inconvénients** :

- ❌ Stateless (redémarrage après 15 min d'inactivité)
- ❌ SQLite pas persistant → besoin de Cloud SQL

#### Étape 1 : Créer un Projet GCP

```bash
# Créer le projet
gcloud projects create character-manager-prod --name="Character Manager"

# Définir le projet comme actif
gcloud config set project character-manager-prod

# Récupérer l'ID du projet
PROJECT_ID=$(gcloud config get-value project)
echo "Project ID: $PROJECT_ID"
```

#### Étape 2 : Activer les APIs

```bash
gcloud services enable \
  run.googleapis.com \
  artifactregistry.googleapis.com \
  sqladmin.googleapis.com \
  containerregistry.googleapis.com
```

#### Étape 3 : Configurer Artifact Registry (Stockage des images Docker)

```bash
# Créer le repository
gcloud artifacts repositories create character-manager \
  --repository-format=docker \
  --location=europe-west1 \
  --description="Character Manager Docker Images"

# Configurer Docker pour utiliser Artifact Registry
gcloud auth configure-docker europe-west1-docker.pkg.dev
```

#### Étape 4 : Publier l'Application

```bash
# Build en mode Release
dotnet publish CharacterManager/CharacterManager.csproj `
    --configuration Release `
    --output publish

# Construire l'image Docker
$PROJECT_ID = (gcloud config get-value project)
$REGION = "europe-west1"
$IMAGE = "$REGION-docker.pkg.dev/$PROJECT_ID/character-manager/app"

docker build -t "$IMAGE:latest" -f Dockerfile .

# Pousser vers Artifact Registry
docker push "$IMAGE:latest"
```

#### Étape 5 : Déployer sur Cloud Run

```bash
$PROJECT_ID = (gcloud config get-value project)
$REGION = "europe-west1"
$IMAGE = "$REGION-docker.pkg.dev/$PROJECT_ID/character-manager/app:latest"

gcloud run deploy character-manager `
  --image=$IMAGE `
  --region=$REGION `
  --platform=managed `
  --allow-unauthenticated `
  --memory=512Mi `
  --cpu=1 `
  --timeout=3600 `
  --set-env-vars="ASPNETCORE_ENVIRONMENT=Production"
```

#### Étape 6 : Récupérer l'URL

```bash
gcloud run services describe character-manager --region=europe-west1

# Ou simplement
gcloud run services list
```

**URL résultat** : `https://character-manager-xxxxx-ew.a.run.app`

---

### Option B : Compute Engine (Pour Contrôle Total)

**Meilleur pour** : Besoin de SQLite persistant, contrôle serveur, données stateful

**Coût** : ~$13-15 USD/mois

#### Étape 1 : Créer une VM

```bash
gcloud compute instances create character-manager-vm \
  --image-family=debian-11 \
  --image-project=debian-cloud \
  --machine-type=e2-medium \
  --zone=europe-west1-b \
  --boot-disk-size=30GB \
  --metadata-from-file=startup-script=startup-script.sh
```

#### Étape 2 : Créer le Script de Démarrage (`startup-script.sh`)

```bash
#!/bin/bash
set -e

# Installer Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
usermod -aG docker $USER

# Installer git
apt-get update
apt-get install -y git

# Cloner le repository
cd /opt
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager

# Démarrer avec docker-compose
docker-compose up -d

# Configurer firewall
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 5269/tcp
sudo ufw enable
```

#### Étape 3 : Configurer le Firewall

```bash
gcloud compute firewall-rules create allow-web-app \
  --allow tcp:80,tcp:443,tcp:5269 \
  --source-ranges 0.0.0.0/0 \
  --target-tags character-manager

gcloud compute instances add-tags character-manager-vm \
  --tags character-manager \
  --zone=europe-west1-b
```

#### Étape 4 : Récupérer l'IP Publique

```bash
gcloud compute instances describe character-manager-vm \
  --zone=europe-west1-b \
  --format="get(networkInterfaces[0].accessConfigs[0].natIP)"
```

---

### Option C : App Engine (Flexibilité)

Simple mais généralement plus cher que Cloud Run.

```bash
# Créer app.yaml
gcloud app create --region=europe-west1

# Déployer
gcloud app deploy
```

---

## Accès à Distance

### 🌐 Via URL Cloud Run

L'URL est **automatiquement accessible** de partout :

```text
https://character-manager-xxxxx-ew.a.run.app
```

### 🔗 Via Domaine Personnalisé

#### Ajouter un domaine personnalisé à Cloud Run

```bash
gcloud run domain-mappings create \
  --service=character-manager \
  --domain=monapp.com \
  --region=europe-west1
```

#### Configurer DNS chez votre Registrar

Ajouter un enregistrement CNAME :

```text
monapp.com    CNAME    goog-managed-ssl.run.app
```

Vérification après 5-10 minutes :

```bash
# Le certificat SSL est généré automatiquement
# Accédez à https://monapp.com
```

### 🔐 Sécuriser avec Identity-Aware Proxy (IAP)

Pour que **seuls vos utilisateurs autorisés** puissent accéder :

```bash
# Créer un compte de service
gcloud iam service-accounts create character-manager-sa \
  --display-name "Character Manager Service Account"

# Donner les permissions
gcloud run services add-iam-policy-binding character-manager \
  --member="serviceAccount:character-manager-sa@$PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/run.invoker" \
  --region=europe-west1

# Ajouter les utilisateurs autorisés
gcloud run services add-iam-policy-binding character-manager \
  --member="user:exemple@example.com" \
  --role="roles/iam.serviceAccountUser" \
  --region=europe-west1
```

---

## Base de Données

### Option 1 : SQLite sur Cloud Run (Pas Recommandé)

> ⚠️ Les fichiers sont perdus à chaque redémarrage. À utiliser **seulement** pour le développement.

### Option 2 : Cloud SQL + Cloud SQL Proxy (Recommandé)

#### Créer une instance Cloud SQL

```bash
gcloud sql instances create character-manager-db \
  --database-version=POSTGRES_15 \
  --tier=db-f1-micro \
  --region=europe-west1 \
  --backup-start-time=03:00 \
  --enable-bin-log
```

#### Créer la base de données

```bash
gcloud sql databases create character_manager \
  --instance=character-manager-db

# Créer un utilisateur
gcloud sql users create app_user --instance=character-manager-db --password=STRONG_PASSWORD
```

#### Modifier Dockerfile pour Cloud SQL Proxy

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 as base
WORKDIR /app
EXPOSE 8080

# Installer Cloud SQL Proxy
RUN apt-get update && apt-get install -y curl
RUN curl -L -o cloud_sql_proxy https://dl.google.com/cloudsql/cloud_sql_proxy.linux.amd64
RUN chmod +x cloud_sql_proxy

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copier proxy
COPY --from=base /app/cloud_sql_proxy .

# Copier l'application
COPY publish/ .

# Script d'entrée
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["./entrypoint.sh"]
```

#### Script d'Entrée (`entrypoint.sh`)

```bash
#!/bin/bash

# Démarrer Cloud SQL Proxy en background
./cloud_sql_proxy -instances=PROJECT_ID:europe-west1:character-manager-db=tcp:5432 &

# Attendre que le proxy soit prêt
sleep 2

# Démarrer l'app
exec dotnet CharacterManager.dll
```

#### Mettre à Jour appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=character_manager;User Id=app_user;Password=STRONG_PASSWORD;"
  }
}
```

#### Ajouter les Permissions au Service Account

```bash
gcloud sql instances patch character-manager-db \
  --update-default-storage-class
```

---

## Monitoring & Logs

### Consulter les Logs

```bash
# Logs Cloud Run
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=character-manager" \
  --limit 50 \
  --format json

# Logs en temps réel
gcloud logging read --follow "resource.type=cloud_run_revision AND resource.labels.service_name=character-manager"
```

### Créer une Alerte

```bash
# Notification Channel (Mail)
gcloud alpha monitoring channels create \
  --display-name="Email Alert" \
  --type=email \
  --channel-labels=email_address=votre@email.com

# Policy d'alerte (Taux d'erreur > 5%)
gcloud alpha monitoring policies create \
  --notification-channels=CHANNEL_ID \
  --display-name="Character Manager High Error Rate" \
  --condition-display-name="Error Rate > 5%" \
  --condition-threshold-filter='resource.type="cloud_run_revision"' \
  --condition-threshold-value=5
```

### Dashboard Cloud Monitoring

Créer un fichier `dashboard.yaml` :

```yaml
displayName: Character Manager Dashboard
dashboardFilters: []
gridLayout:
  widgets:
  - title: Request Count
    xyChart:
      dataSets:
      - timeSeriesQuery:
          timeSeriesFilter:
            filter: 'metric.type="run.googleapis.com/request_count"'
  
  - title: Error Rate
    xyChart:
      dataSets:
      - timeSeriesQuery:
          timeSeriesFilter:
            filter: 'metric.type="run.googleapis.com/request_latencies"'
  
  - title: Instance Count
    xyChart:
      dataSets:
      - timeSeriesQuery:
          timeSeriesFilter:
            filter: 'metric.type="run.googleapis.com/instance_count"'
```

Appliquer le dashboard :

```bash
gcloud monitoring dashboards create --config-from-file=dashboard.yaml
```

---

## 💰 Estimé des Coûts (Forfait Gratuit Google Cloud)

| Service | Gratuit | Au-delà |
|---------|---------|---------|
| **Cloud Run** | 2M requêtes/mois | $0.40 / million requêtes |
| **Cloud SQL** | Premier mois gratuit | ~$5-8 USD/mois |
| **Artifact Registry** | Premier mois gratuit | $0.10 USD/Go |
| **Cloud Storage** | 5 Go | $0.020 USD/Go |
| **Compute Engine** | - | ~$13-15 USD/mois (e2-medium) |

**Recommandation** : Commencer avec Cloud Run (gratuit) + Cloud SQL (gratuit le premier mois)

---

## ⚠️ Troubleshooting

### Cloud Run : Application redémarre constamment

```bash
# Voir les erreurs
gcloud logging read "resource.type=cloud_run_revision" --limit 20

# Augmenter timeout et CPU
gcloud run deploy character-manager \
  --timeout=3600 \
  --cpu=2 \
  --memory=1Gi
```

### Erreur de Connexion à la Base de Données

```bash
# Tester la connexion Cloud SQL
gcloud sql connect character-manager-db --user=postgres

# Vérifier les logs du proxy
gcloud logging read "resource.labels.function_name=cloud-sql-proxy"
```

### Images n'affichent Pas

**Cause** : Sur Cloud Run, `/app/wwwroot/images/` n'est pas persistant

**Solution** : Utiliser Google Cloud Storage

```bash
# Créer un bucket
gsutil mb -l europe-west1 gs://character-manager-images/

# Modifier le code C# pour uploader vers GCS
```

### Timeout API

```bash
# Augmenter les ressources
gcloud run deploy character-manager \
  --cpu=2 \
  --memory=1Gi \
  --min-instances=1  # Garder au moins 1 instance chaude
```

---

## Système de Mise à Jour

L'application intègre un système de vérification automatique des mises à jour

- Notification visuelle si une nouvelle version est disponible
- Lien direct vers la page de téléchargement
- Affichage des notes de version

## 🚀 Déploiement sur un Serveur

### Option 1: Installation Directe (Windows Server)

1. **Télécharger la dernière release**

   ```powershell
   # Créer un dossier d'installation
   New-Item -Path "C:\Apps\CharacterManager" -ItemType Directory
   cd C:\Apps\CharacterManager
   
   # Télécharger et extraire (remplacer VERSION par la version actuelle)
   Invoke-WebRequest -Uri "https://github.com/Thorinval/CharacterManager/releases/download/vVERSION/CharacterManager-VERSION-win-x64.zip" -OutFile "CharacterManager.zip"
   Expand-Archive -Path "CharacterManager.zip" -DestinationPath .
   ```

2. **Créer un service Windows**

   ```powershell
   # Avec NSSM (Non-Sucking Service Manager)
   nssm install CharacterManager "C:\Apps\CharacterManager\CharacterManager.exe"
   nssm set CharacterManager Start SERVICE_AUTO_START
   nssm start CharacterManager
   ```

### Option 2: Docker sur Linux

```bash
# 1. Installer Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# 2. Créer les dossiers de données
mkdir -p /opt/character-manager/data
mkdir -p /opt/character-manager/images

# 3. Créer docker-compose.yml
cat > /opt/character-manager/docker-compose.yml << 'EOF'
version: '3.8'
services:
  charactermanager:
    image: ghcr.io/thorinval/charactermanager:latest
    container_name: character-manager
    ports:
      - "5269:8080"
    volumes:
      - ./data:/app/data
      - ./images:/app/wwwroot/images
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
EOF

# 4. Démarrer l'application
cd /opt/character-manager
docker-compose up -d

# 5. Vérifier les logs
docker logs -f character-manager
```

### Option 3: Hébergement Cloud

#### Azure App Service

```bash
# Publier sur Azure
az webapp up --name character-manager --resource-group MyResourceGroup --sku F1

# Configurer les variables d'environnement
az webapp config appsettings set --name character-manager --settings ASPNETCORE_ENVIRONMENT=Production
```

#### AWS Elastic Beanstalk

```bash
# Créer un package de déploiement
dotnet publish -c Release -o ./publish

# Créer un fichier ZIP
cd publish
zip -r ../deployment.zip .

# Déployer avec AWS CLI
aws elasticbeanstalk create-application-version --application-name CharacterManager --version-label v1 --source-bundle S3Bucket=my-bucket,S3Key=deployment.zip
```

## 🔒 Configuration de Production

### Sécurisation

1. **Activer HTTPS**

   ```json
   // appsettings.Production.json
   {
     "Kestrel": {
       "Endpoints": {
         "Https": {
           "Url": "https://+:5001"
         }
       }
     }
   }
   ```

2. **Limiter les hôtes autorisés**

   ```json
   {
     "AllowedHosts": "votre-domaine.com"
   }
   ```

3. **Configuration de la base de données**

   - Par défaut: SQLite dans le dossier de l'application
   - Pour production: Utiliser un volume Docker ou un chemin persistant

### Sauvegarde

```bash
# Sauvegarder la base de données
cp /app/data/charactermanager.db /backup/charactermanager-$(date +%Y%m%d).db

# Avec Docker
docker exec character-manager sqlite3 /app/data/charactermanager.db ".backup /app/data/backup.db"
docker cp character-manager:/app/data/backup.db ./backup.db
```

## 📊 Monitoring

### Logs Docker

```bash
# Voir les logs en temps réel
docker logs -f character-manager

# Dernières 100 lignes
docker logs --tail 100 character-manager
```

### Vérifier la santé de l'application

```bash
# Vérifier que l'application répond
curl http://localhost:5269

# Avec Docker
docker ps | grep character-manager
```

## 🔄 Mise à Jour de l'Application

### Méthode 1: Manuelle

1. Télécharger la nouvelle version
2. Arrêter l'application
3. Remplacer les fichiers
4. Redémarrer l'application
5. Conserver la base de données (caractermanager.db)

### Méthode 2: Docker

```bash
# Télécharger la nouvelle image
docker pull ghcr.io/thorinval/charactermanager:latest

# Arrêter et supprimer l'ancien conteneur
docker-compose down

# Démarrer avec la nouvelle image
docker-compose up -d
```

### Méthode 3: Via l'interface

- L'application notifie automatiquement quand une mise à jour est disponible
- Cliquer sur "Télécharger" ouvre la page de release
- Suivre les instructions d'installation

## 🆘 Dépannage

### L'application ne démarre pas

```bash
# Vérifier les logs
docker logs character-manager

# Vérifier les permissions
ls -la /app/data

# Vérifier le port
netstat -tulpn | grep 5269
```

### Problème de base de données

```bash
# Recréer la base
rm charactermanager.db
# Redémarrer l'application (elle recréera la base)
```

### Problème de mise à jour

- Vérifier la connexion internet
- Vérifier que GitHubRepo est configuré dans appsettings.json
- Consulter les logs pour les erreurs HTTP

## 📞 Support

Pour toute question ou problème :

- Créer une issue sur GitHub: <https://github.com/Thorinval/CharacterManager/issues>
- Consulter les releases: <https://github.com/Thorinval/CharacterManager/releases>
