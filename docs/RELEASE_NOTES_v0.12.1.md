# CharacterManager v0.12.1 - Notes de Version

Date de sortie : 2 janvier 2026

## 🎯 Objectif de cette version

Centralisation des images de personnages dans une DLL dédiée (`CharacterManager.Resources.Personnages`) pour une meilleure organisation et gestion des ressources.

## ✨ Nouveautés

### Architecture de Ressources Embarquées

- **Nouvelle DLL** : `CharacterManager.Resources.Personnages.dll` 
  - Gère toutes les images de personnages
  - Structure organisée par dossier de personnage
  - Support jusqu'à 4 images PNG par personnage

### Organisation des Images

Les images sont maintenant groupées par personnage dans des sous-dossiers :

```
CharacterManager.Resources.Personnages/
└── Images/
    ├── Alexa/
    │   ├── alexa.png                 (image détaillée)
    │   ├── alexa_header.png          (optionnel - en-tête)
    │   ├── alexa_small_portrait.png  (petit portrait)
    │   └── alexa_small_select.png    (portrait sélectionné)
    ├── Hunter/
    │   ├── hunter.png
    │   ├── hunter_header.png
    │   ├── hunter_small_portrait.png
    │   └── hunter_small_select.png
    └── ...
```

### Nouvelle API

- **Endpoint** : `/api/resources/personnages/{personnage}/{fichier}`
- Remplace l'ancien système de fichiers statiques `/images/personnages/`
- Exemple : `/api/resources/personnages/Alexa/alexa_small_portrait.png`

### Nouveaux Services

#### `PersonnageResourceManager`
Service d'accès aux ressources embarquées avec méthodes :
- `GetImageBytes(personnageFolder, fileName)` - Récupère une image en bytes
- `GetImageStream(personnageFolder, fileName)` - Récupère un stream d'image
- `ImageExists(personnageFolder, fileName)` - Vérifie l'existence d'une image
- `GetAllPersonnageImages(personnageFolder)` - Liste toutes les images d'un personnage
- `GetAllResourceNames()` - Liste toutes les ressources (débogage)

#### `PersonnageImageUrlHelper`
Helper pour générer les URLs des images :
- `GetImageDetailUrl(nomPersonnage)` - URL de l'image détaillée
- `GetImageHeaderUrl(nomPersonnage)` - URL de l'image d'en-tête
- `GetImageSmallPortraitUrl(nomPersonnage)` - URL du petit portrait
- `GetImageSmallSelectUrl(nomPersonnage)` - URL du portrait sélectionné
- `NormalizePersonnageName(nomPersonnage)` - Normalisation en PascalCase
- `GetLegacyImageUrl(...)` - Support de compatibilité v0.12.0

#### `PersonnageResourcesController`
Contrôleur API avec endpoints :
- `GET /api/resources/personnages/{personnage}/{fileName}` - Récupère une image
- `GET /api/resources/personnages/list` - Liste toutes les ressources
- `HEAD /api/resources/personnages/{personnage}/{fileName}` - Vérifie l'existence
- `GET /api/resources/personnages/{personnage}/all` - Liste les images d'un personnage

## 🔧 Modifications Techniques

### Modèle `Personnage`
- Propriétés `ImageUrl*` mises à jour pour utiliser `PersonnageImageUrlHelper`
- URLs générées dynamiquement via l'API de ressources
- Colonnes stockées maintenues pour compatibilité DB

### `PersonnageService`
- Méthodes `Add()` et `Update()` utilisent `PersonnageImageUrlHelper`
- URLs de l'API v0.12.1 utilisées pour les colonnes stockées

### `AppConstants.Paths`
- `ImagesPersonnages` : pointe vers `/api/resources/personnages`
- `ImagesPersonnagesLegacy` : `/images/personnages` (compatibilité)

## 📦 Migration

### Script PowerShell : `Migrate-PersonnageImages.ps1`

Automatise la migration des images depuis `wwwroot/images/personnages` vers la nouvelle structure :

```powershell
# Simulation (voir ce qui serait fait)
.\scripts\Migrate-PersonnageImages.ps1 -WhatIf

# Migration réelle
.\scripts\Migrate-PersonnageImages.ps1
```

Le script :
1. Identifie automatiquement les personnages depuis les noms de fichiers
2. Crée les dossiers en PascalCase (ex: "alexa" → "Alexa")
3. Copie les 4 types d'images dans le dossier approprié
4. Ignore les fichiers déjà migrés

### Étapes de Migration Manuelles

1. **Compiler la nouvelle DLL** :
   ```bash
   dotnet build CharacterManager.Resources.Personnages
   ```

2. **Migrer les images** :
   ```powershell
   .\scripts\Migrate-PersonnageImages.ps1
   ```

3. **Tester l'API** :
   - Démarrer l'application
   - Naviguer vers `/api/resources/personnages/list`
   - Vérifier qu'une image est accessible : `/api/resources/personnages/Alexa/alexa_small_portrait.png`

4. **(Optionnel) Nettoyer l'ancien dossier** :
   Une fois la migration validée, vous pouvez supprimer `/wwwroot/images/personnages` (sauf le dossier `adult` si utilisé)

## 🔄 Compatibilité

### Compatibilité Ascendante
- L'ancien chemin `/images/personnages/{fichier}` peut rester fonctionnel si les fichiers sont conservés
- Les colonnes DB stockées continuent d'être remplies
- Migration transparente pour les utilisateurs finaux

### Compatibilité Descendante
- Les bases de données v0.12.0 fonctionnent sans modification
- Pas de migration DB requise
- Les URLs sont calculées dynamiquement

## 🐛 Corrections

- Amélioration de la gestion des noms de personnages avec caractères spéciaux
- Normalisation cohérente des noms (espaces, tirets, apostrophes)

## 📝 Notes Techniques

### Convention de Nommage des Dossiers

Les dossiers de personnages utilisent le **PascalCase** :
- `alexa` → `Alexa`
- `o-rinn` → `ORinn`
- `zoe et chloe` → `ZoeEtChloe`

### Embedded Resources .NET

Les images sont intégrées comme **Embedded Resources** :
- Compilées dans la DLL au build
- Namespace automatique : `CharacterManager.Resources.Personnages.Images.{Dossier}.{Fichier}`
- Accès via `Assembly.GetManifestResourceStream()`

### Performance

- Cache HTTP de 1 heure (3600s) via `[ResponseCache]`
- Streaming direct depuis la DLL (pas de copie mémoire inutile)
- Réduction de la taille du dossier `wwwroot`

## 🔮 Prochaines Étapes

### Version 0.12.2 (Future)
- Support du contenu adulte dans `Images/Adult/`
- Interface d'administration pour gérer les images
- Preview des images disponibles par personnage
- Upload d'images personnalisées

## 📚 Documentation

- Voir [Images/README.md](../CharacterManager.Resources.Personnages/Images/README.md) pour la structure des dossiers
- Voir [SCRIPTS.md](../SCRIPTS.md) pour l'utilisation de `Migrate-PersonnageImages.ps1`

## 🙏 Remerciements

Cette version améliore significativement l'organisation des ressources et prépare le terrain pour une gestion plus avancée des images de personnages.

---

**Version** : 0.12.1  
**Date** : 2 janvier 2026  
**Auteur** : Thorinval
