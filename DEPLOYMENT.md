# Guide de Déploiement - Character Manager

## 📦 Méthodes de Déploiement

### 1. Publication Locale (Windows)

#### Avec le script PowerShell
```powershell
# Publication pour Windows x64
.\publish.ps1

# Publication pour une autre plateforme
.\publish.ps1 -Runtime linux-x64
```

Cela créera un fichier ZIP prêt à être distribué contenant tout le nécessaire.

#### Manuellement avec .NET CLI
```bash
dotnet publish .\CharacterManager\CharacterManager.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output .\publish `
    -p:PublishSingleFile=true
```

### 2. Déploiement avec Docker

#### Construction de l'image
```bash
docker build -t character-manager .
```

#### Lancement du conteneur
```bash
# Avec docker run
docker run -d \
  --name character-manager \
  -p 5269:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/CharacterManager/wwwroot/images:/app/wwwroot/images \
  character-manager

# Avec docker-compose
docker-compose up -d
```

#### Accès à l'application
Ouvrez votre navigateur à: http://localhost:5269

### 3. Déploiement Automatisé (GitHub Actions)

#### Création d'une release
```bash
# 1. Créer un tag de version
git tag -a v1.0.0 -m "Version 1.0.0"

# 2. Pousser le tag vers GitHub
git push origin v1.0.0
```

GitHub Actions va automatiquement :
- Compiler l'application pour Windows et Linux
- Créer des archives ZIP/TAR.GZ
- Publier une release GitHub avec les fichiers
- Construire et publier l'image Docker

#### Configuration requise sur GitHub
1. Aller dans **Settings** → **Actions** → **General**
2. Activer "Read and write permissions" pour GITHUB_TOKEN

## 🔄 Système de Mise à Jour

L'application intègre un système de vérification automatique des mises à jour.

### Configuration dans appsettings.json
```json
{
  "AppInfo": {
    "Name": "Character Manager",
    "Version": "1.0.0",
    "Author": "Thorinval",
    "GitHubRepo": "Thorinval/CharacterManager"
  }
}
```

### Fonctionnement
- Vérification automatique au démarrage de l'application
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
- Créer une issue sur GitHub: https://github.com/Thorinval/CharacterManager/issues
- Consulter les releases: https://github.com/Thorinval/CharacterManager/releases
