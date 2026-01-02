# Character Manager - Guide d'Installation et Déploiement

## 📋 Prérequis

### Pour l'utilisateur final
- **Windows 7 ou plus récent** (64-bit)
- Pas de dépendances externes (l'application est auto-contenue)

### Pour les développeurs
- **.NET 9 SDK** (ou runtime uniquement pour les utilisateurs)
- **Inno Setup 6.x** (pour compiler l'installateur) - Gratuit: https://jrsoftware.org/isdl.php

## 🚀 Installation pour l'utilisateur final

### Option 1: Utiliser l'installateur (Recommandé)

1. **Télécharger** le fichier `CharacterManager-0.12.0-Setup.exe`
2. **Exécuter** l'installateur
3. **Suivre** l'assistant d'installation
4. **Lancer** l'application depuis le menu Démarrer ou le raccourci Bureau
5. L'application s'ouvrira automatiquement dans votre navigateur à `http://localhost:5000`

### Option 2: Installation manuelle (Portable)

1. Créer un dossier: `C:\Apps\CharacterManager`
2. Copier tous les fichiers du dossier `publish` dans `C:\Apps\CharacterManager`
3. Exécuter `CharacterManager.exe` depuis le dossier
4. L'application s'ouvrira à `http://localhost:5000`

## 🔧 Construction de l'installateur (Développeurs)

### Étape 1: Préparer la publication

```powershell
# Exécuter depuis le répertoire racine du projet
.\Publish-Setup.ps1
```

Ou manuellement:

```powershell
cd CharacterManager
dotnet publish -c Release --self-contained -o ../publish
cd ..
```

### Étape 2: Compiler l'installateur

#### Avec Inno Setup GUI (Plus simple):
1. Ouvrir `CharacterManager.iss` dans Inno Setup
2. Cliquer sur **Build** → **Compile**
3. L'installateur `.exe` sera généré dans `publish\installer\`

#### Avec ligne de commande (Automatisé):
```batch
iscc CharacterManager.iss
```

### Résultat

L'installateur généré: `CharacterManager-0.12.0-Setup.exe`
- Localisation: `publish\installer\`
- Taille: ~150 MB (contient .NET Runtime + Application)

## 📁 Structure de l'installation

### Sur la machine de l'utilisateur:
```
C:\Program Files\CharacterManager\
├── CharacterManager.exe          (Application)
├── CharacterManager.dll          (Core)
├── CharacterManager.Resources.Interface.dll  (Images embarquées)
├── wwwroot/                      (Ressources web)
│   ├── css/
│   ├── i18n/
│   └── ...
├── ...autres fichiers .NET...
└── charactermanager.db           (Créé à la première exécution)
```

### Base de données:
- **Emplacement**: `{Install}\charactermanager.db` 
- **Type**: SQLite (auto-contenu)
- **Créée**: À la première exécution
- **Supprimée**: À la désinstallation

## 🔄 Mises à jour

### Méthode 1: Réinstallation (Recommandée)
1. Désinstaller la version actuelle
2. Installer la nouvelle version via l'installateur

> **Note**: Votre base de données `charactermanager.db` n'est pas supprimée par défaut - elle persiste après la désinstallation pour sauvegarder vos données

### Méthode 2: Mise à jour manuelle
1. Télécharger les fichiers publiés
2. Remplacer les fichiers de l'installation (sauf `charactermanager.db`)

## 🎯 Ports et Configuration

### Port par défaut
- **Release**: Port `5000`
- **Debug**: Port `5269`

### Changer le port:
Éditer `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:6000"
      }
    }
  }
}
```

## 🐛 Dépannage

### L'application ne démarre pas
1. Vérifier que le port 5000 est disponible
2. Vérifier les fichiers de journaux dans le dossier installation
3. Lancer directement `CharacterManager.exe` depuis le dossier d'installation

### Port déjà utilisé
```powershell
# Trouver le processus utilisant le port
netstat -ano | findstr :5000

# Ou changer le port dans appsettings.json
```

### Base de données corrompue
```powershell
# Supprimer le fichier database
Remove-Item "C:\Program Files\CharacterManager\charactermanager.db"

# Redémarrer l'application (elle recréera la DB)
```

## 📦 Contenu de chaque version

### Version 0.12.0
- ✅ Resource DLL (images embarquées)
- ✅ API Resources pour servir les images
- ✅ 28 capacités avec icônes Bootstrap
- ✅ Export/Import PML amélioré
- ✅ Migration base de données (PuissanceTotale)
- ✅ Installateur Windows

## 🔐 Sécurité

- **L'application n'accède à Internet que pour télécharger les mises à jour**
- **La base de données est locale et cryptée automatiquement par Entity Framework**
- **Aucune donnée personnelle n'est envoyée**
- **L'application peut fonctionner entièrement hors ligne**

## 📝 Fichiers de configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=charactermanager.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

## 🤝 Support

Pour les problèmes:
1. Vérifier la section **Dépannage**
2. Consulter les logs dans le dossier application
3. Contacter le support ou créer une issue GitHub

## 📄 Licence

Voir le fichier `LICENSE` dans le dossier d'installation.
