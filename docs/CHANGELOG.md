# Changelog

Tous les changements notables de ce projet seront documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

---

## [0.12.2] - 2026-01-03

### 🐛 Corrections (Fixed)

#### Export/Import PML
- **Historique des classements** : Correction du format d'export qui utilisait `.xml` au lieu de `.pml`
  - Refactorisation pour utiliser `PmlImportService.ExportPmlAsync()` avec `exportHistories: true`
  - Fichier exporté maintenant au format `.pml` (standardisé avec le reste de l'application)
  - Validation stricte à l'import : accepte uniquement les fichiers `.pml`
  - Messages d'erreur cohérents avec les autres pages
  - Fichiers modifiés :
    - `Historique.razor.cs` : Ajout injection `PmlImportService`, refonte méthodes export/import
    - `Historique.razor` : Attribut `accept` changé de `.xml` à `.pml`

#### UI/UX - Standardisation des boutons
- **Page Inventaire** : Correction du style du bouton "Importer"
  - Style changé de `btn-outline-info` à `btn-info` (cohérence visuelle)
  - Ajout de la localisation via `LocalizationService.T("inventory.import")`
  
- **Modale détail personnage** : Suppression de l'alerte bloquante après sauvegarde
  - Permet la fermeture directe de la modale après édition d'un personnage
  - Améliore l'expérience utilisateur (moins de clics nécessaires)
  - Fichiers modifiés : `DetailPersonnageModal.razor`, `DetailPersonnage.razor.cs`

#### UI/UX - Page Affection Lucie
- **Chips d'affection** : Correction de la largeur des badges d'affection
  - Ajout `min-width: 88px` pour afficher correctement les valeurs à 3 chiffres
  - Les badges s'adaptent maintenant correctement sur une seule ligne

### ♻️ Refactoring

#### Architecture - Migration Template Editor
- **Déplacement complet de l'éditeur de templates** de la page Inventaire vers la page Templates
  - Migration de 380+ lignes de code (UI + logique métier)
  - Composant `EscouadePreviewEditor` réutilisé
  - Panel de recherche d'inventaire intégré dans Templates
  - Toutes les fonctionnalités migrées :
    - Création/édition de templates
    - Sauvegarde/chargement
    - Export PML
    - Renommer/dupliquer/supprimer
    - Drag-and-drop de personnages
  - Fichiers modifiés :
    - `Templates.razor` : Réécriture complète avec éditeur intégré
    - `Templates.razor.cs` : Tous les gestionnaires d'événements implémentés
    - `Inventaire.razor` : Code de template retiré

#### Documentation
- **Réorganisation de la documentation** : Tous les fichiers `.md` déplacés dans le dossier `docs/`
  - 12 fichiers markdown déplacés de la racine vers `docs/`
  - Structure documentaire centralisée et organisée
  - Fichiers déplacés :
    - `DEPLOYMENT.md`, `DOCUMENTATION.md`, `INNO_SETUP.md`
    - `INSTALLATION_GUIDE.md`, `QUICK_START.md`
    - `README_v0.12.0.md`, `RELEASE_0.12.0.md`, `RELEASE_0.12.1.md`
    - `RELEASE_CHECKLIST.md`, `SCRIPTS.md`
    - `SUMMARY_v0.12.1.md`, `VERSION_MANAGEMENT.md`

### ✨ Améliorations (Improved)

#### Cohérence de l'Interface
- Standardisation des boutons d'export/import sur toutes les pages
  - `btn-success` (vert) pour tous les boutons Export/Télécharger
  - `btn-info` (bleu) pour tous les boutons Import/Upload
  - Localisations cohérentes avec `LocalizationService`

#### Format de Fichiers
- Unification du format d'export/import : **PML** devient le standard
  - Toutes les pages exportent maintenant en `.pml`
  - Validation stricte à l'import (refus des `.xml` génériques)
  - Messages d'erreur uniformisés

### 📝 Documentation
- Ajout de `CHANGELOG.md` avec historique des versions 0.12.1 et 0.12.2
- Documentation détaillée de tous les changements UI/UX

### 🔧 Technique
- ✅ Build vérifié : Tous les projets compilent sans erreur
- ✅ Tests unitaires : Tous les tests passent
- 🏗️ Architecture : Code mieux organisé avec séparation des responsabilités

---

## [0.12.1] - 2026-01-03

### 🎯 Fonctionnalités Majeures (Major Features)

#### 1. **Architecture de Ressources Embarquées (Embedded Resources)**
- Toutes les images de personnages packagées dans `CharacterManager.Resources.Personnages.dll`
- Images organisées par personnage dans des dossiers imbriqués
- Support de jusqu'à 4 images par personnage :
  - Image détail (taille complète)
  - Image header (optionnelle)
  - Petit portrait (miniatures UI)
  - Petit select (état sélectionné)

#### 2. **API REST pour les Ressources**
- Nouveau endpoint : `/api/v1/resources/personnages/{personnage}/{fichier}`
- Livraison efficace des images avec cache HTTP (1 heure)
- Streaming direct depuis les ressources embarquées
- Exemples :
  - `GET /api/v1/resources/personnages/Alexa/alexa_small_portrait.png`
  - `GET /api/v1/resources/personnages/Hunter/hunter_small_select.png`
  - `GET /api/v1/resources/personnages/list` (endpoint de debug)

#### 3. **Gestion Intelligente des Ressources**
- `PersonnageResourceManager` pour accès programmatique
- `PersonnageImageUrlHelper` pour génération d'URLs
- Normalisation automatique des noms de personnages (PascalCase)
- Support des caractères spéciaux (tirets, underscores, apostrophes)

### 📊 Statistiques
- **126 images de personnages** migrées depuis le système de fichiers
- **86 personnages uniques** identifiés et organisés
- **4+ images par personnage** (jusqu'à 4 types)
- **~130 MB** d'images totales embarquées dans la DLL
- **100% de compatibilité** avec v0.12.0

### ✨ Améliorations

#### Performance
- Ressources compilées = démarrage plus rapide
- Pas d'I/O système de fichiers pour récupération d'images
- Cache HTTP réduit la bande passante
- Chargement paresseux des images (lazy loading)

#### Organisation
- Structure claire : un dossier par personnage
- Convention de nommage cohérente
- Facilite l'ajout de nouveaux personnages
- Élimination de la duplication de fichiers

#### Déploiement
- Images embarquées dans la DLL (pas de fichiers séparés)
- Déploiement simplifié
- Moins de risques de fichiers manquants
- Package auto-contenu

### 🔧 Changements Techniques

#### Nouveaux Projets
- `CharacterManager.Resources.Personnages` : DLL de ressources pour images de personnages
- `CharacterManager.Resources.Personnages.Tests` : Tests unitaires pour validation

#### Services Ajoutés
- `PersonnageResourceManager` : Accès aux ressources embarquées
- `PersonnageImageUrlHelper` : Génération d'URLs de ressources
- `PersonnageResourcesController` : API REST pour servir les images

#### Scripts PowerShell
- `Migrate-PersonnageImages.ps1` : Migration automatisée des images
  - Support simulation (`-WhatIf`)
  - Validation pré-migration
  - Rapport détaillé

### 🐛 Corrections
- Normalisation des noms de personnages pour compatibilité URL
- Gestion des cas spéciaux (O-Rinn → ORinn)
- Support des apostrophes et caractères spéciaux

### 📝 Documentation
- `RELEASE_0.12.1.md` : Notes de version complètes
- `SUMMARY_v0.12.1.md` : Résumé des changements
- `VERSION_0.12.1_PLAN.md` : Plan détaillé de développement
- README dans dossier Images avec conventions

### ⚠️ Notes de Migration
- Les anciennes URLs (`/images/personnages/...`) continuent de fonctionner
- Migration progressive possible
- Compatibilité ascendante maintenue

---

## [0.12.0] - 2026-01-02

### 🎯 Fonctionnalités Majeures (Major Features)

#### 1. **Système de Capacités Complet**
- **28 capacités de jeu** avec icônes Bootstrap
- Gestion complète : Ajouter, modifier, supprimer
- Localisations : Français et Anglais
- **PML Import/Export** : Support complet pour `capacites_import.pml`
- CRUD intégré dans l'interface

#### 2. **Resource DLL (CharacterManager.Resources.Interface)**
- **Projet .NET 9.0** dédié aux ressources
- **25 images embarquées** (auto-contenues dans la DLL)
- Pas de dépendance externe aux fichiers wwwroot
- **API REST** pour servir les ressources : `/api/v1/resources/interface/{fileName}`

#### 3. **Déploiement Portable**
- Application **100% auto-contenue**
  - Runtime .NET 9 intégré
  - Toutes les ressources embarquées
  - Base de données SQLite locale
- Fonctionnement sur **clé USB ou dossier quelconque**
- Installeur Windows complet (Inno Setup)

#### 4. **Infrastructure de Déploiement**
- **Scripts PowerShell** : `Deploy-Manager.ps1`, `Publish-Setup.ps1`
- **Scripts Batch/Shell** : `Deploy-Local.bat`, `Deploy-Local.sh`
- **Inno Setup** : `CharacterManager.iss` pour installateur Windows
- **Documentation** : `DEPLOYMENT.md`, `INSTALLATION_GUIDE.md`

### 🔧 Changements Techniques

#### Base de Données
- Migration : `20260102175205_AddCapacitiesTable.cs`
- Nouvelle table : `Capacities`
- Colonne corrigée : `PuissanceTotal` → `PuissanceTotale`
- Support complet du tracking d'historique

#### Architecture
- **PmlExportOptions** remplace 6 paramètres booléens
  - ✅ Export Types : INVENTORY, TEMPLATES, BEST_SQUAD, HISTORIES, LEAGUE_HISTORY, CAPACITES
  - ✅ Extensibilité : Dictionary `CustomExports` pour futurs types
  - ✅ Rétrocompatibilité : Méthode factory `FromBooleans()`

#### API REST
- Nouveau contrôleur : `ResourcesController`
- Endpoints :
  - `GET /api/v1/resources/interface/{fileName}` - Servir image avec type MIME
  - `GET /api/v1/resources/interface` - Lister images disponibles
- Détection MIME : png, jpg, gif, webp, svg

#### UI / Icônes Bootstrap
- Correction de format : `bi @icon` → `bi bi-{iconname}`
- 28 icônes validées et corrigées
- Liste complète des capacités avec icônes cohérentes

### 📊 Statistiques

| Élément | Avant | Après | Notes |
|---------|-------|-------|-------|
| Capacités | 0 | 28 | Nouvelles fonctionnalités |
| Images embarquées | 0% | 100% | Toutes dans DLL |
| Taille app portable | N/A | ~150 MB | Auto-contenu + Runtime |
| Paramètres ExportPmlAsync | 6 boolean | PmlExportOptions | Amélioré |
| Tests unitaires | 60 | 61 | +1 pour Capacités |
| Fichiers script | 2 | 6 | Deploy-Manager, Deploy-Local, etc |

### 🧪 Validation

#### Tests Unitaires
```
61 / 61 ✅ Tous les tests passent en Release
```

#### Build
```
Configuration: Release
Erreurs: 0
Warnings: 9 (file lock warnings, non-bloquants)
Temps de compilation: ~2.6 secondes
```

### 📝 Documentation
- `RELEASE_0.12.0.md` : Notes de version complètes
- `README_v0.12.0.md` : Guide utilisateur
- `DEPLOYMENT.md` : Guide de déploiement
- `INSTALLATION_GUIDE.md` : Guide d'installation
- `VERSION_0.12.0_PLAN.md` : Plan de développement

### ⚙️ Scripts et Outils
- `Deploy-Manager.ps1` : Déploiement automatisé
- `Publish-Setup.ps1` : Publication et setup
- `Deploy-Local.bat` / `Deploy-Local.sh` : Déploiement local multi-plateforme
- `CharacterManager.iss` : Configuration Inno Setup

---

## [0.11.1] - 2026-01-02

### 🎯 Fonctionnalités (Added)

#### Système de Capacités
- Ajout de la colonne `PuissanceTotale` dans `HistoriqueClassement`
- Implémentation complète de la gestion des capacités (Capacite management)
- Nouvelle table de base de données pour les capacités
- CRUD pour les capacités des personnages

### 🔧 Technique
- Migration base de données : Ajout colonne `PuissanceTotale`
- Services de gestion des capacités

---

## [0.11.0] - 2026-01-01

### ✨ Améliorations (Improved)

#### Système de Mise à Jour
- Implémentation de la vérification locale des mises à jour
- Fallback automatique vers GitHub si serveur local indisponible
- Amélioration de la robustesse du système de mise à jour

#### Interface Utilisateur
- Nouveaux styles CSS pour la page **Histoligues**
- Nouveaux styles CSS pour la page **Maison Lucie**
- Amélioration de l'apparence des composants UI
- Meilleure cohérence visuelle entre les pages

---

## [0.10.2] - 2025-12-31

### ♻️ Refactoring (Changed)

#### Documentation
- Restructuration complète de la documentation
- Réorganisation des fichiers de documentation
- Mise à jour de la gestion des classements

### 🐛 Corrections (Fixed)
- Correction des warnings dans le navigateur
- Amélioration de la gestion des classements

---

## [0.10.1] - 2025-12-28

### 🐛 Corrections (Fixed)
- Corrections mineures et optimisations
- Stabilisation de la version 0.10.x

---

## [0.10.0] - 2025-12-28

### 🎯 Version Majeure

#### Nouvelles Fonctionnalités
- Refonte majeure de l'architecture
- Améliorations significatives de performance
- Nouvelles fonctionnalités de gestion

### 📝 Notes
- Version majeure marquant une étape importante du projet
- Base solide pour les versions futures

---

## [0.9.2] - 2025-12-26

### ✨ Améliorations (Improved)

#### Import/Export
- Ajout de l'initialisation par fichier PML par défaut lorsque l'inventaire est vide
- Export de fichier PML pour configuration
- Amélioration du système d'import/export

#### Interface Inventaire
- Image du personnage dans l'écran détail maintenant visible pour les mercenaires non sélectionnés
- **Inventaire triable par puissance** (nouvelle fonctionnalité)
- **Inventaire filtrable par catégorie** (nouvelle fonctionnalité)
- Amélioration de l'ergonomie générale

---

## [0.9.1] - 2025-12-22 à 2025-12-24

### 🎯 Fonctionnalités (Added)

#### Gestion des Classements
- Création d'une modale pour créer des classements
- Amélioration du chargement des puissances dans les pièces de Lucie
- Reprise des chaînes en dur par des constantes (meilleure maintenabilité)

#### Documentation et Roadmap
- Ajout du **changelog** et **release log**
- Chargement du texte roadmap depuis fichier
- Création de roadmap avec sauvegarde
- Amélioration de la transparence du projet

#### Sélection
- Ajout de cases à cocher pour sélection de personnages
- Facilite les opérations en masse

### 🐛 Corrections (Fixed)
- Fix affichage androïdes et top commandant
- Fix chargement des puissances dans les pièces de Lucie
- Corrections diverses de l'interface

### ♻️ Refactoring (Changed)
- Calculs des puissances max et escouade revus
- Ajout de portraits manquants
- Retrait d'images superflues

---

## [0.9.0] - 2025-12-21

### 🎯 Fonctionnalités Majeures (Added)

#### Maison de Lucie (Lucie House)
- **Implémentation complète de la Maison de Lucie**
- Export Lucie inclus dans l'inventaire
- Calcul de puissance incluant les bonus de Lucie
- Nouvelle page dédiée à la gestion de la maison

#### Tests et Scripts
- Mise à jour des tests unitaires
- Ajout d'un script de gestion de version automatique
- Corrections des tests unitaires, export et import

### 🐛 Corrections (Fixed)
- Fix affichage détail des personnages
- Fix localisation
- Corrections diverses d'affichage

### ♻️ Refactoring (Changed)
- Réorganisation du code (reorg)
- Ajout de fichiers PNG manquants
- Nettoyage des duplications
- Amélioration de la gestion des niveaux

### 🔧 Technique
- Adaptation Docker
- Refonte du système adulte
- Nouvelles images ajoutées

---

## [0.8.0] - 2025-12-21

### 🐛 Corrections (Fixed)
- Corrections mineures
- Optimisations diverses

---

## [0.7.1] - 2025-12-20

### 🐛 Corrections (Fixed)

#### Localisation
- Corrections majeures des localisations (français/anglais)
- Correction des warnings de localisation
- Amélioration de la gestion multilingue

#### Import/Export
- Correction import/export avec localisation
- Meilleure gestion des fichiers localisés

---

## [0.7.0] - 2025-12-20

### ✨ Améliorations (Improved)

#### Page Meilleur Escouade
- Correction de l'affichage du seuil par rapport au max escouade
- Amélioration des icônes dans les templates
- Corrections des tests unitaires

#### Interface
- Fix emplacement des cards
- Fix détail incorrect
- Mise à jour des images de personnages

---

## [0.6.0] - 2025-12-20

### 🎯 Fonctionnalité Majeure (Added)

#### Format PML (Personnage Manager Lite)
- **Refonte complète des imports/exports** vers le nouveau format PML
- Format XML standardisé pour l'application
- Meilleure compatibilité et extensibilité
- Base pour tous les futurs exports/imports

### ♻️ Refactoring (Changed)
- Reprise complète du système d'import/export
- Nouvelle fonction limite de puissance (en travaux)
- Refonte des pages de gestion

---

## [0.5.0] - 2025-12-19 à 2025-12-20

### ✨ Améliorations (Improved)

#### Interface Utilisateur
- Refonte des pages
- Adaptation des traductions
- Déplacement du bouton paramètres en haut à gauche
- Correction de la casse du titre historique

### 🔧 Technique
- Mise à jour des références vers le nouveau dossier interface
- Corrections Docker
- Fix workflow environment et notifications Slack

#### CI/CD
- Ajout d'un job de vérification des secrets
- Guide de setup CI/CD
- Création automatique du repo distant
- Corrections YAML de build

---

## [0.4.0] - 2025-12-19

### 🎯 Version Majeure (Major Release)

#### Navigation et Mise en Page
- **Nouvelle navigation** complète
- **Mise en page revue** de toute l'application
- Refonte de l'ergonomie générale
- Amélioration significative de l'expérience utilisateur

### ✨ Nouvelles Fonctionnalités
- Nouveau système de navigation
- Layout modernisé
- Meilleure organisation des pages

---

## [0.3.0] - 2025-12-17 à 2025-12-18

### 🎯 Fonctionnalités (Added)

#### Déploiement et Infrastructure
- **Déploiement Google Cloud** (GCP)
- Configuration pour cloud
- Scripts de déploiement automatisés

#### Gestion de Puissance
- Ajout du champ puissance pour les personnages
- Upload d'image select
- Calcul de puissance intégré

#### Upload d'Images
- Système d'upload d'images pour personnages
- Gestion des ressources visuelles
- Mise à jour des images

#### Notes de Version
- Release notes automatiques
- Génération automatisée de la documentation de version

### 🐛 Corrections (Fixed)
- Corrections des références de version
- Localisation améliorée
- CSS inventaire corrigé

---

## [0.2.0] - 2025-12-15 à 2025-12-16

### 🎯 Fonctionnalités (Added)

#### Authentification et Sécurité
- **Système de profils** utilisateur
- **Authentification** complète
- Gestion des sessions
- Correction du login
- Modification du système d'authentification
- Adaptation des styles pour les pages authentifiées

#### Historique des Classements
- Amélioration majeure de l'historique des classements
- Correction de l'affichage des classements
- Meilleure visualisation des données historiques

#### Localisation
- **Localisation multilingue** complète
- Support français et anglais
- Page de classement localisée
- Réorganisation : CSS séparé, CS séparé (meilleure architecture)

#### Templates
- Interface template revue
- Correction des warnings
- Amélioration de l'ergonomie

---

## [0.1.0] - 2025-12-13 à 2025-12-16

### 🎉 Version Initiale (Initial Release)

#### Fonctionnalités de Base

##### Gestion des Personnages
- **Page inventaire** complète
- **Page détail** des personnages
- Mise à jour de la classe Personnage
- Affichage et gestion des caractéristiques

##### Base de Données
- Intégration **SQLite**
- Correction de la BDD
- Correction de l'archivage
- Gestion persistante des données

##### Import/Export
- **Système d'import** de personnages
- Tri sur le rang
- Correction du bug d'import de rareté
- Correction de l'import général

##### Interface Utilisateur
- Mode adulte (filtrage de contenu)
- Évolution de l'interface
- Amélioration de l'interface générale
- Correction des événements bouton
- Correction des chemins d'accès images
- Retrait de fichiers inutiles

##### Templates et Drag & Drop
- Système de templates d'escouade
- **Drag-and-drop** pour organisation
- Correction de la communication drag-drop
- Layout compact pour templates
- Correction des warnings CS8602

##### Page Meilleur Escouade
- Réorganisation de l'appli (server/front)
- **Nouvelle page Meilleur Escouade**
- Calcul automatique de la meilleure composition

##### Calcul de Puissance
- Ajout de la puissance dans l'interface
- **Implémentation des méthodes de calcul de puissance**
- Métriques de performance pour personnages

##### Docker
- **Dockerisation** de l'application
- Présentation par grille
- Changement du style des titres
- Configuration Docker complète

##### Page À Propos
- Refonte de la page "À propos"
- Nouveau layout
- Fix du style des pages
- Correction de comportements divers

##### Export et Améliorations
- Ajout de l'export de données
- Diverses améliorations d'ergonomie
- Gestion des images (ajout de quelques images fournies)

##### Tests
- **Mise en place des tests** unitaires
- Mise en place des tests de pages
- Framework de tests intégré

##### Infrastructure
- Clean up : Suppression des binaires
- Ajout d'un `.gitignore` approprié
- Gestion propre du versioning Git

### 📝 Premier Commit
- **Initial commit** : Base du projet
- Architecture de base Blazor
- Structure initiale du projet

---

## Notes sur les Versions

### Convention de Numérotation (SemVer)

```
MAJOR.MINOR.PATCH
```

- **MAJOR** (0.x.x → 1.x.x) : Changements majeurs, potentiellement breaking
- **MINOR** (x.0.x → x.1.x) : Nouvelles fonctionnalités rétrocompatibles
- **PATCH** (x.x.0 → x.x.1) : Corrections de bugs et petites améliorations

### Historique des Versions

- **0.1.0 - 0.3.0** : Développement initial, fonctionnalités de base
- **0.4.0** : Première refonte majeure de la navigation
- **0.5.x** : Introduction du format PML
- **0.7.x** : Stabilisation de la localisation
- **0.9.x** : Maison de Lucie et système de classement
- **0.10.x** : Refonte architecturale
- **0.11.x** : Système de capacités
- **0.12.x** : Ressources embarquées et déploiement portable

---

## Légende des Types de Changements

- 🎯 **Major Features** : Nouvelles fonctionnalités majeures
- ✨ **Improved** : Améliorations de fonctionnalités existantes
- 🐛 **Fixed** : Corrections de bugs
- ♻️ **Refactoring** : Restructuration du code sans changement de fonctionnalité
- 🔧 **Technical** : Changements techniques (build, config, dépendances)
- 📝 **Documentation** : Ajouts ou modifications de documentation
- 🚀 **Performance** : Améliorations de performance
- 🔒 **Security** : Corrections de sécurité
- ⚠️ **Breaking Changes** : Changements non rétrocompatibles
