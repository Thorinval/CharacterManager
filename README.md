# Character Manager

Application de gestion de personnages développée avec Blazor Server et .NET 9.0.

## 📋 Fonctionnalités

- ✅ **Gestion d'inventaire** - Ajout, modification, suppression de personnages
- ✅ **Mode d'affichage flexible** - Vue grille (jusqu'à 10 par ligne) et vue liste compacte
- ✅ **Sélection multiple** - Édition et suppression par lots
- ✅ **Filtres avancés** - Recherche par nom, tri par rareté, niveau, rang, type
- ✅ **Composition d'escouade** - Sélection de personnages pour constituer une équipe
- ✅ **Calcul de puissance** - Puissance totale et maximale de l'escouade
- ✅ **Import/Export CSV** - Sauvegarde et restauration des données
- ✅ **Système d'images dynamiques** - Support de 3 types d'images par personnage
- ✅ **Versionnement Git** - Numéro de build basé sur les commits
- ✅ **Mises à jour automatiques** - Notification des nouvelles versions disponibles

## 🚀 Démarrage Rapide

### Prérequis

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Un navigateur moderne (Chrome, Firefox, Edge)

### Installation Locale

```bash
# Cloner le dépôt
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager

# Restaurer les dépendances
dotnet restore

# Lancer l'application
dotnet run --project CharacterManager/CharacterManager.csproj
```

Ouvrez votre navigateur à : **http://localhost:5269**

### Avec Docker

```bash
# Lancer avec docker-compose
docker-compose up -d

# Accéder à l'application
# http://localhost:5269
```

## 📦 Publication et Déploiement

### Publication Automatique (Recommandé)

Utilisez le script PowerShell inclus :

```powershell
# Publier pour Windows x64
.\publish.ps1

# Publier pour Linux
.\publish.ps1 -Runtime linux-x64
```

Cela créera une archive ZIP prête à distribuer.

### Créer une Release GitHub

```bash
# 1. Mettre à jour la version dans appsettings.json
# 2. Créer un tag Git
git tag -a v1.0.1 -m "Version 1.0.1"
git push origin v1.0.1

# GitHub Actions créera automatiquement :
# - Packages Windows et Linux
# - Image Docker
# - Release GitHub avec notes de version
```

📖 **Documentation complète** : Consultez [DEPLOYMENT.md](DEPLOYMENT.md) et [RELEASE.md](RELEASE.md)

## 🔄 Système de Mise à Jour

L'application vérifie automatiquement les nouvelles versions au démarrage en interrogeant l'API GitHub. Si une mise à jour est disponible, une notification apparaît en haut à droite avec :

- Numéro de la nouvelle version
- Lien direct de téléchargement
- Notes de version

Configuration dans `appsettings.json` :
```json
{
  "AppInfo": {
    "GitHubRepo": "Thorinval/CharacterManager"
  }
}
```

## 🏗️ Architecture

- **Frontend** : Blazor Server avec InteractiveServer render mode
- **Backend** : ASP.NET Core 9.0
- **Base de données** : SQLite (fichier local `charactermanager.db`)
- **ORM** : Entity Framework Core 9.0
- **CSS** : Bootstrap 5 + CSS personnalisé
- **Conteneurisation** : Docker + Docker Compose

## 📂 Structure du Projet

```
CharacterManager/
├── Components/
│   ├── Layout/         # MainLayout, NavMenu
│   ├── Pages/          # Pages Blazor (Home, Escouade, Inventaire, etc.)
│   ├── UpdateNotification.razor  # Système de mise à jour
│   └── AboutModal.razor
├── Data/               # DbContext
├── Models/             # Entités (Personnage, Capacite, AppSettings)
├── Services/           # Services métier
│   ├── PersonnageService.cs
│   ├── CsvImportService.cs
│   ├── AppVersionService.cs
│   └── UpdateService.cs
├── wwwroot/            # Fichiers statiques
│   ├── images/         # Images des personnages
│   └── app.css
├── appsettings.json    # Configuration
└── Program.cs          # Point d'entrée

Racine/
├── Dockerfile                    # Image Docker
├── docker-compose.yml            # Orchestration
├── publish.ps1                   # Script de publication
├── DEPLOYMENT.md                 # Guide de déploiement
├── RELEASE.md                    # Guide de release
└── .github/workflows/release.yml # CI/CD
```

## 🛠️ Développement

### Commandes utiles

```bash
# Restaurer les dépendances
dotnet restore

# Compiler
dotnet build

# Lancer en mode développement
dotnet run

# Publier
dotnet publish -c Release

# Créer une migration
dotnet ef migrations add NomMigration --project CharacterManager

# Appliquer les migrations
dotnet ef database update --project CharacterManager
```

### Hot Reload

L'application supporte le hot reload. Les modifications dans les fichiers `.razor` et `.cs` sont automatiquement rechargées.

## 🐳 Docker

### Construction

```bash
docker build -t character-manager .
```

### Exécution

```bash
docker run -d \
  --name character-manager \
  -p 5269:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/images:/app/wwwroot/images \
  character-manager
```

### Docker Compose (Recommandé)

```bash
# Démarrer
docker-compose up -d

# Voir les logs
docker-compose logs -f

# Arrêter
docker-compose down

# Reconstruire
docker-compose build --no-cache
```

## 📊 Base de Données

L'application utilise SQLite avec Entity Framework Core. La base de données est créée automatiquement au premier démarrage.

### Tables

- **Personnages** - Informations des personnages
- **Capacites** - Capacités liées aux personnages (relation 1-N)
- **AppSettings** - Paramètres de l'application (dernier fichier importé, etc.)

### Sauvegarde

```bash
# Copier la base de données
cp charactermanager.db charactermanager-backup-$(date +%Y%m%d).db

# Avec Docker
docker exec character-manager sqlite3 /app/data/charactermanager.db ".backup /app/data/backup.db"
docker cp character-manager:/app/data/backup.db ./backup.db
```

## 🎨 Images

Les personnages supportent 3 types d'images :

1. **Detail** : `{nom}.png` - Vue détaillée (grande)
2. **Preview** : `{nom}_small_portrait.png` - Vignettes (listes/tables)
3. **Selected** : `{nom}_small_select.png` - État sélectionné (escouade)

Les images doivent être placées dans `wwwroot/images/interface/`. Un fichier `default.png` sert de fallback.

## 🔐 Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AppInfo": {
    "Name": "Character Manager",
    "Version": "1.0.0",
    "Author": "Thorinval",
    "Description": "Application de gestion des personnages",
    "GitHubRepo": "Thorinval/CharacterManager"
  }
}
```

### Variables d'environnement (Docker)

- `ASPNETCORE_ENVIRONMENT` - `Development` ou `Production`
- `ASPNETCORE_URLS` - URL d'écoute (par défaut: `http://+:8080`)

## 📞 Support et Contribution

- **Issues** : https://github.com/Thorinval/CharacterManager/issues
- **Releases** : https://github.com/Thorinval/CharacterManager/releases
- **Documentation** : Voir [DEPLOYMENT.md](DEPLOYMENT.md)

## 📄 Licence

Copyright © 2025 Thorinval. Tous droits réservés.

## 🙏 Remerciements

- ASP.NET Core Team
- Bootstrap
- SQLite
- GitHub Actions
