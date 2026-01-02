# Plan de Développement - Version 0.12.1

## Objectif Principal

Centraliser la gestion des images de personnages dans une DLL dédiée avec une organisation par dossier de personnage.

## Contexte

### Situation Actuelle (v0.12.0)
- Images de personnages dans `wwwroot/images/personnages/`
- Fichiers en vrac (130+ fichiers PNG/JPG)
- Pas d'organisation claire
- Mélange de 4 types d'images par personnage

### Problèmes Identifiés
1. **Organisation** : Difficile de retrouver toutes les images d'un personnage
2. **Maintenance** : Ajout/modification d'images fastidieux
3. **Évolutivité** : Pas de structure pour gérer du contenu additionnel
4. **Déploiement** : Gros dossier wwwroot copié à chaque build

## Solution Proposée

### Architecture en DLL de Ressources

```
CharacterManager.Resources.Personnages (nouvelle DLL)
├── CharacterManager.Resources.Personnages.csproj
├── PersonnageResourceManager.cs (service d'accès)
└── Images/
    ├── {Personnage1}/
    │   ├── {nom}.png
    │   ├── {nom}_header.png
    │   ├── {nom}_small_portrait.png
    │   └── {nom}_small_select.png
    └── {Personnage2}/
        └── ...
```

### Avantages

✅ **Organisation claire** : Un dossier = un personnage  
✅ **Embedded Resources** : Intégré dans la DLL (pas de copie externe)  
✅ **Type-safe** : Accès via C# (pas de string magic)  
✅ **API REST** : Exposition via endpoint dédié  
✅ **Performance** : Cache HTTP, streaming direct  
✅ **Évolutivité** : Facile d'ajouter d'autres types de ressources  

## Tâches de Développement

### Phase 1 : Infrastructure de Base ✅

- [x] Créer le projet `CharacterManager.Resources.Personnages.csproj`
- [x] Configurer les Embedded Resources (*.png, *.jpg)
- [x] Créer `PersonnageResourceManager.cs` avec méthodes de base
- [x] Ajouter le projet à la solution

### Phase 2 : API et Services ✅

- [x] Créer `PersonnageResourcesController.cs`
  - [x] Endpoint `GET /{personnage}/{fichier}`
  - [x] Endpoint `GET /list` (debug)
  - [x] Endpoint `HEAD /{personnage}/{fichier}` (existence)
  - [x] Endpoint `GET /{personnage}/all` (liste images d'un personnage)
- [x] Créer `PersonnageImageUrlHelper.cs`
  - [x] Méthodes de génération d'URLs
  - [x] Normalisation des noms (PascalCase)
  - [x] Support de compatibilité legacy

### Phase 3 : Intégration ✅

- [x] Mettre à jour `Personnage.cs`
  - [x] Modifier les propriétés `ImageUrl*` pour utiliser le helper
- [x] Mettre à jour `PersonnageService.cs`
  - [x] Méthodes `Add()` et `Update()`
- [x] Mettre à jour `AppConstants.Paths`
  - [x] Nouvelles constantes pour l'API
  - [x] Constantes legacy pour compatibilité
- [x] Ajouter la référence au projet principal

### Phase 4 : Migration ✅

- [x] Créer `Migrate-PersonnageImages.ps1`
  - [x] Identifier les personnages depuis les noms de fichiers
  - [x] Créer les dossiers en PascalCase
  - [x] Copier les fichiers
  - [x] Mode simulation (-WhatIf)
  - [x] Rapport de migration
- [x] Créer `Images/README.md` (documentation de la structure)

### Phase 5 : Versioning et Documentation ✅

- [x] Incrémenter la version vers 0.12.1
  - [x] `CharacterManager.csproj`
  - [x] `appsettings.json`
  - [x] `CharacterManager.Resources.Interface.csproj`
  - [x] `CharacterManager.Resources.Personnages.csproj`
- [x] Créer `RELEASE_NOTES_v0.12.1.md`
- [x] Créer `VERSION_0.12.1_PLAN.md` (ce document)

## Structure des Fichiers Créés/Modifiés

### Nouveaux Fichiers

```
CharacterManager.Resources.Personnages/
├── CharacterManager.Resources.Personnages.csproj
├── PersonnageResourceManager.cs
└── Images/
    ├── README.md
    ├── Alexa/ (exemple)
    ├── Hunter/ (exemple)
    ├── Kitty/ (exemple)
    └── Ravenna/ (exemple)

CharacterManager/
└── Server/
    ├── Controllers/
    │   └── PersonnageResourcesController.cs
    └── Services/
        └── PersonnageImageUrlHelper.cs

scripts/
└── Migrate-PersonnageImages.ps1

docs/
├── RELEASE_NOTES_v0.12.1.md
└── VERSION_0.12.1_PLAN.md
```

### Fichiers Modifiés

```
CharacterManager.sln
CharacterManager/CharacterManager.csproj
CharacterManager/appsettings.json
CharacterManager.Resources.Interface/CharacterManager.Resources.Interface.csproj
CharacterManager/Server/Models/Personnage.cs
CharacterManager/Server/Services/PersonnageService.cs
CharacterManager/Server/Constants/AppConstants.cs
.gitignore
```

## Conventions

### Nommage des Dossiers

**Format** : PascalCase (première lettre de chaque mot en majuscule)

Exemples :
- `alexa` → `Alexa`
- `o-rinn` → `ORinn`
- `zoe et chloe` → `ZoeEtChloe`
- `hunter` → `Hunter`

### Types d'Images

Par personnage (jusqu'à 4) :

1. **Détail** : `{nom}.png` ou `.jpg`
2. **Header** : `{nom}_header.png` (optionnel)
3. **Small Portrait** : `{nom}_small_portrait.png`
4. **Small Select** : `{nom}_small_select.png`

Tous les fichiers en lowercase avec underscores.

### Namespace des Ressources

```
CharacterManager.Resources.Personnages.Images.{Dossier}.{Fichier}
```

Exemple :
```
CharacterManager.Resources.Personnages.Images.Alexa.alexa_small_portrait.png
```

## API Endpoints

### Récupérer une Image

```http
GET /api/resources/personnages/{personnage}/{fichier}
```

**Exemple** :
```
GET /api/resources/personnages/Alexa/alexa_small_portrait.png
```

**Réponse** :
- 200 OK + image (PNG ou JPEG)
- 404 Not Found si l'image n'existe pas

**Cache** : 1 heure (3600s)

### Lister Toutes les Ressources (Debug)

```http
GET /api/resources/personnages/list
```

**Réponse** :
```json
{
  "Count": 425,
  "Resources": [
    "CharacterManager.Resources.Personnages.Images.Alexa.alexa.png",
    "CharacterManager.Resources.Personnages.Images.Alexa.alexa_header.png",
    ...
  ]
}
```

### Vérifier l'Existence

```http
HEAD /api/resources/personnages/{personnage}/{fichier}
```

**Réponse** :
- 200 OK si existe
- 404 Not Found sinon

### Lister les Images d'un Personnage

```http
GET /api/resources/personnages/{personnage}/all
```

**Exemple** :
```
GET /api/resources/personnages/Alexa/all
```

**Réponse** :
```json
{
  "Personnage": "Alexa",
  "ImageCount": 4,
  "Images": [
    "alexa.png",
    "alexa_header.png",
    "alexa_small_portrait.png",
    "alexa_small_select.png"
  ]
}
```

## Migration depuis v0.12.0

### Étapes Manuelles

1. **Build du projet de ressources** :
   ```powershell
   dotnet build CharacterManager.Resources.Personnages
   ```

2. **Migration des images** :
   ```powershell
   # Simulation
   .\scripts\Migrate-PersonnageImages.ps1 -WhatIf
   
   # Migration réelle
   .\scripts\Migrate-PersonnageImages.ps1
   ```

3. **Tester** :
   - Build complet : `dotnet build`
   - Démarrer l'app
   - Vérifier `/api/resources/personnages/list`

4. **(Optionnel) Nettoyer** :
   - Une fois validé, supprimer `wwwroot/images/personnages` (sauf `adult/` si nécessaire)

### Script de Migration

**Caractéristiques** :

- ✅ Détection automatique des personnages
- ✅ Normalisation des noms en PascalCase
- ✅ Support de tous les types d'images
- ✅ Mode simulation (-WhatIf)
- ✅ Rapport détaillé (copiés/ignorés)
- ✅ Gestion des doublons

**Utilisation** :

```powershell
# Par défaut (depuis la racine du repo)
.\scripts\Migrate-PersonnageImages.ps1

# Avec chemins personnalisés
.\scripts\Migrate-PersonnageImages.ps1 `
  -SourceDir ".\CharacterManager\wwwroot\images\personnages" `
  -TargetDir ".\CharacterManager.Resources.Personnages\Images"

# Simulation seulement
.\scripts\Migrate-PersonnageImages.ps1 -WhatIf
```

## Tests

### Tests Unitaires (Futurs)

À ajouter dans `CharacterManager.Tests` :

- `PersonnageResourceManagerTests.cs`
  - Test `GetImageBytes()` avec images existantes/inexistantes
  - Test `GetImageStream()` 
  - Test `ImageExists()`
  - Test `GetAllPersonnageImages()`

- `PersonnageImageUrlHelperTests.cs`
  - Test normalisation des noms
  - Test génération d'URLs
  - Test cas limites (null, vide, caractères spéciaux)

### Tests d'Intégration

- Vérifier que toutes les images sont accessibles via l'API
- Tester le cache HTTP
- Tester les fallbacks (404 → default_portrait.png)

### Tests Manuels

1. Naviguer vers la page Inventaire
2. Vérifier que toutes les images de personnages s'affichent
3. Vérifier le mode sélection (images `_small_select.png`)
4. Tester sur les pages :
   - Escouade
   - Meilleur Escouade
   - Detail Personnage
   - Historique

## Performance

### Avant (v0.12.0)

- 130+ fichiers dans wwwroot
- Copie à chaque build
- Pas de cache HTTP explicite
- Accès direct au file system

### Après (v0.12.1)

- Images dans la DLL (Embedded Resources)
- Pas de copie (compilé une fois)
- Cache HTTP 1 heure
- Streaming depuis la mémoire
- Réduction de la taille de wwwroot (~50%)

## Sécurité

### Validation des Entrées

- Noms de personnages/fichiers validés par l'API
- Pas d'accès direct au file system
- Limitation aux ressources embarquées

### Contenu Adulte

Structure prévue pour v0.12.2+ :

```
Images/
└── Adult/
    └── {Personnage}/
```

Le contrôleur pourra filtrer selon les préférences utilisateur.

## Rétrocompatibilité

### Base de Données

✅ Aucune migration nécessaire
- Colonnes `ImageUrl*Stored` toujours remplies
- Format d'URL compatible

### Code Client

✅ Transparent
- Les propriétés `ImageUrl*` du modèle `Personnage` continuent de fonctionner
- Génération automatique des URLs

### Ancien Chemin

✅ Support optionnel
- L'ancien chemin `/images/personnages/` peut coexister
- Helper `GetLegacyImageUrl()` disponible pour fallback

## Déploiement

### Fichiers à Inclure

```
bin/Release/net9.0/
├── CharacterManager.dll
├── CharacterManager.Resources.Interface.dll
├── CharacterManager.Resources.Personnages.dll  ← nouvelle
└── wwwroot/
    ├── css/
    ├── i18n/
    └── images/
        └── personnages/  ← peut être supprimé après migration
```

### Checklist

- [ ] Build en mode Release
- [ ] Vérifier que les 3 DLLs sont présentes
- [ ] Tester l'API `/api/resources/personnages/list`
- [ ] Vérifier qu'une image s'affiche dans l'interface
- [ ] Tester en production

## Évolutions Futures

### Version 0.12.2

- Interface d'administration des images
- Upload d'images personnalisées
- Gestion du contenu adulte via `Images/Adult/`
- Preview des images disponibles

### Version 0.13.0

- Support de vidéos (MP4, WebM)
- Support d'audio (voix de personnages)
- Gestion des variantes de costumes
- Métadonnées des images (auteur, licence, etc.)

## Conclusion

La v0.12.1 pose les bases d'une gestion moderne et évolutive des ressources de personnages. L'architecture en DLL avec Embedded Resources offre :

- ✅ Organisation claire
- ✅ Performance améliorée
- ✅ Facilité de maintenance
- ✅ Extensibilité pour le futur

Cette version est **100% compatible** avec v0.12.0 et ne nécessite **aucune migration de base de données**.

---

**Statut** : ✅ COMPLÉTÉ  
**Version** : 0.12.1  
**Date** : 2 janvier 2026  
**Auteur** : Thorinval
