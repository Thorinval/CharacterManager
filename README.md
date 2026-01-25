# 🎮 Character Manager

> **Gestionnaire de personnages pour Lust Goddess**  
> Version actuelle : **1.1.0** (25/01/2026) 🎉

Application de gestion complète pour suivre et optimiser vos escouades, personnages, capacités et progression dans le jeu Lust Goddess.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Production%20v1.1.0-brightgreen.svg)](docs/RELEASE_NOTES.md)
[![Tests](https://img.shields.io/badge/Tests-78%2F78%20%E2%9C%85-blue.svg)](.github/workflows/tests.yml)

---

## ✨ Fonctionnalités Principales

### 📊 **Gestion d'Inventaire**
- ✅ Gestion complète des **Commandants**, **Mercenaires** et **Androïdes**
- ✅ Profils détaillés avec statistiques (Puissance, PA, PV, Niveau, Rang)
- ✅ **Puissance réelle** automatique pour les commandants (Puissance + Rang × 20) utilisée pour l'affichage et les tris
- ✅ Support de 28 capacités de jeu avec icônes
- ✅ Mode adulte optionnel avec contenu filtrable

### 🏆 **Historique & Suivi**
- ✅ **Historique des classements** avec évolution temporelle
- ✅ **Historique des ligues** pour suivre votre progression
- ✅ **Historique des modifications** (création, modification, suppression)
- ✅ Export JSON de l'historique avec filtres avancés
- ✅ Module admin de **nettoyage des doublons** pour corriger les références incohérentes

### 📈 **Statistiques & Analyse**
- ✅ Graphiques interactifs (Type d'attaque, Faction, Rang)
- ✅ Cartes récapitulatives (Puissance moyenne, extrêmes)
- ✅ Visualisation de l'évolution de votre inventaire

### 🏠 **Maison de Lucie**
- ✅ Gestion des pièces et niveaux
- ✅ Édition directe des pièces avec sauvegarde dans l'historique
- ✅ Calcul de la puissance tactique et stratégique
- ✅ Affichage de l'affection

### 💾 **Import / Export**
- ✅ Format **PML** (XML personnalisé) pour tous vos exports
- ✅ Workflow guidé en 3 étapes : prévisualisation, résolution des conflits, application
- ✅ Logs d'import structurés par catégories avec niveaux ✅/⚠️/❌
- ✅ Détection et résolution des conflits (valeurs anciennes/nouvelles) avant l'application
- ✅ Import d'inventaire, templates, historiques
- ✅ Export de compositions d'escouades optimales
- ✅ Sauvegarde automatique dans SQLite

### 🌍 **Multilingue**
- 🇫🇷 Français
- 🇬🇧 Anglais

### 🔐 **Sécurité**
- ✅ Authentification par utilisateur
- ✅ Gestion des rôles (Admin / Utilisateur)
- ✅ Hachage PBKDF2 avec salt pour les mots de passe
- ✅ Protection contre les attaques par force brute (lockout)

---

## 🚀 Installation

### Option 1 : Windows Installer (Recommandé)

1. **Téléchargez** la dernière version depuis la [page Releases](https://github.com/Thorinval/CharacterManager/releases)
2. **Exécutez** `CharacterManager-Setup.exe`
3. **Suivez** les instructions de l'installateur
4. **Lancez** l'application depuis le menu Démarrer ou le raccourci bureau

### Option 2 : Docker

```bash
# Cloner le repository
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager

# Lancer avec Docker Compose
docker-compose up -d

# Accéder à l'application
# http://localhost:8080
```

### Option 3 : Build depuis les sources

**Prérequis :**
- .NET SDK 9.0 ou supérieur
- Git

```bash
# Cloner le repository
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager

# Restaurer les dépendances
dotnet restore

# Compiler
dotnet build

# Lancer l'application
cd CharacterManager
dotnet run
```

L'application sera accessible à l'adresse : **http://localhost:5000**

---

## 🎯 Démarrage Rapide

### Premier Lancement

1. **Démarrez** l'application
2. **Connectez-vous** avec les identifiants générés automatiquement :
   - 👤 **Utilisateur** : `admin`
   - 🔑 **Mot de passe** : _Consultez les logs de la console au démarrage_
   
   ⚠️ **IMPORTANT** : Changez ce mot de passe immédiatement après la première connexion !

3. **Accédez aux paramètres** (icône ⚙️ en haut à droite)
   - Changez votre mot de passe
   - Choisissez votre langue (FR/EN)
   - Activez/désactivez le mode adulte

### Import de Vos Données

1. **Exportez** vos données depuis le jeu (format PML/XML)
2. Dans Character Manager, cliquez sur **Import/Export PML** (icône ☁️)
3. **Sélectionnez** votre fichier d'export, vérifiez la prévisualisation et **résolvez les conflits** si besoin
4. Appliquez l'import : vos personnages et historiques apparaîtront dans l'**Inventaire**

### Navigation

- 🏠 **Accueil** : Vue d'ensemble et accès rapides
- 📦 **Inventaire** : Gestion de tous vos personnages
- 🏆 **Historique** : Suivi de vos classements
- 📊 **Statistiques** : Graphiques et analyses
- 🏠 **Maison de Lucie** : Gestion des pièces
- 📜 **Historique des modifications** : Journal de toutes les actions

---

## 📚 Documentation

Documentation complète disponible dans le dossier [`docs/`](docs/) :

- 🎉 [**Release Notes 1.1.0**](docs/RELEASE_NOTES.md) - Import assisté & nettoyage des doublons 🚀
- 📖 [**Guide d'installation**](docs/INSTALLATION_GUIDE.md) - Installation détaillée
- 🚀 [**Démarrage rapide**](docs/QUICK_START.md) - Guide pas à pas
- 📘 [**Documentation complète**](docs/DOCUMENTATION.md) - Toutes les fonctionnalités
- 📝 [**Notes de version**](docs/RELEASE_NOTES.md) - Changements et nouveautés
- 🗺️ [**Roadmap**](docs/ROADMAP.md) - Fonctionnalités à venir
- 🐛 [**Changelog**](docs/CHANGELOG.md) - Historique des versions

### Documentation Technique

- 🔧 [**Création de release**](docs/CREATE_RELEASE.md) - Script automatisé
- 🚢 [**Déploiement**](docs/DEPLOYMENT.md) - Docker, GCP, Azure
- 🏗️ [**Inno Setup**](docs/INNO_SETUP.md) - Création d'installateur Windows

---

## 🛠️ Technologies

- **Framework** : [ASP.NET Core 9.0](https://dotnet.microsoft.com/)
- **UI** : [Blazor Server](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) avec InteractiveServer render mode
- **Base de données** : [SQLite](https://www.sqlite.org/) + [Entity Framework Core 9.0](https://docs.microsoft.com/ef/core/)
- **CSS** : [Bootstrap 5](https://getbootstrap.com/) + Material Symbols
- **Graphiques** : [Chart.js 4.4.1](https://www.chartjs.org/)
- **Conteneurisation** : [Docker](https://www.docker.com/)
- **Tests** : [xUnit](https://xunit.net/) + [Moq](https://github.com/moq/moq4)

---

## 🎯 Statut de Production

### v1.1.0 - Production ✅

| Critère | Status |
|---------|--------|
| Tests unitaires | ✅ 78/78 passent |
| Import PML assisté | ✅ Prévisualisation + résolution de conflits |
| Nettoyage doublons | ✅ Module admin `/admin/cleanup-duplicates` |
| UI | ✅ Headers unifiés, cartes classement corrigées |
| Build | ✅ Sans erreurs |
| Documentation | ✅ À jour (RELEASE_NOTES) |

**Release détaillée :** voir [RELEASE_NOTES.md](docs/RELEASE_NOTES.md) (déploiement du 25/01/2026).

### Prochaines Versions (v1.2+)

- Branche [`develop-1.x`](https://github.com/Thorinval/CharacterManager/tree/develop-1.x) : refonte du classement et itérations UX
- Version 1.2.0 préparée pour validation

Consultez la [Roadmap](docs/ROADMAP.md) pour les détails.

---

## 🔄 Mise à Jour

L'application vérifie automatiquement les nouvelles versions au démarrage. Une notification apparaîtra en haut à droite si une mise à jour est disponible.

### Mise à jour manuelle

1. **Sauvegardez** votre base de données `charactermanager.db`
2. **Téléchargez** la nouvelle version
3. **Installez** (l'installateur préserve vos données)
4. **Relancez** l'application

---

## 🧪 Tests

```bash
# Lancer tous les tests
dotnet test

# Lancer avec couverture de code
dotnet test /p:CollectCoverage=true
```

**Couverture actuelle** : 78 tests unitaires ✅

---

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :

1. 🍴 **Fork** le projet
2. 🌿 **Créer** une branche feature (`git checkout -b feature/AmazingFeature`)
3. 💾 **Commit** vos changements (`git commit -m 'Add some AmazingFeature'`)
4. 📤 **Push** vers la branche (`git push origin feature/AmazingFeature`)
5. 🔀 **Ouvrir** une Pull Request

---

## 📝 License

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

---

## 👤 Auteur

**Thorinval**

- 🐙 GitHub : [@Thorinval](https://github.com/Thorinval)
- 📦 Repository : [CharacterManager](https://github.com/Thorinval/CharacterManager)

---

## 🙏 Remerciements

- Communauté Lust Goddess
- [Bootstrap](https://getbootstrap.com/) pour les composants UI
- [Material Symbols](https://fonts.google.com/icons) pour les icônes
- [Chart.js](https://www.chartjs.org/) pour les graphiques
- Tous les contributeurs et testeurs

---

## 📞 Support

- 🐛 **Bug reports** : [Ouvrir une issue](https://github.com/Thorinval/CharacterManager/issues)
- 💡 **Suggestions** : [Ouvrir une discussion](https://github.com/Thorinval/CharacterManager/discussions)
- 📧 **Contact** : Via GitHub

---

<div align="center">

**⭐ Si vous aimez ce projet, n'hésitez pas à lui donner une étoile sur GitHub ! ⭐**

Made with ❤️ by [Thorinval](https://github.com/Thorinval)

</div>
