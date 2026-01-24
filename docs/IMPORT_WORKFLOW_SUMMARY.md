# Workflow d'Import PML - Résumé des Fonctionnalités

## Vue d'ensemble

Le système d'import PML a été amélioré avec un workflow complet de prévisualisation, détection de conflits et rapport final détaillé.

## Flux d'Import Amélioré

### 1. Prévisualisation (Pré-rapport)

**Bouton**: "Prévisualiser l'import"

**Fonctionnalités**:
- Analyse du fichier PML sans écriture en base de données
- Validation de toutes les données (commandant, mercenaires, androides, pièces Lucie)
- Détection des classements identiques ou moins bons déjà présents
- Génération d'un rapport détaillé avec logs structurés par catégorie
- Détection automatique des conflits sur les historiques de modification

**Logs structurés par catégorie**:
- Général (parsing, erreurs structurelles)
- Classement (doublons, validation de composition)
- Commandant (puissance, données valides)
- Mercenaires (composition de l'escouade)
- Androides (composition de l'équipe)
- Lucie (maison, pièces)
- Capacités (import des capacités)

**Niveaux de logs**:
- ✅ **Ok** (badge vert) : Validation réussie
- ⚠️ **Warning** (badge orange) : Données ignorées ou déjà présentes
- ❌ **Error** (badge rouge) : Erreur de validation ou parsing

### 2. Résolution des Conflits

**Affichage**: Bouton "Résoudre les conflits (X)" visible si des conflits sont détectés

**Compteurs en temps réel**:
- Nombre total de conflits
- Nombre de conflits résolus
- Nombre de conflits non résolus

**Types de conflits**:
- Modifications existantes dans les historiques de personnages
- Conflits détectés par personnage, champ modifié et date

**Interface de résolution**:
- Tableau détaillé de chaque conflit avec :
  - Nom du personnage
  - Champ modifié
  - Date du classement
  - Ancienne valeur (existante en base)
  - Nouvelle valeur (à importer)
- Choix par conflit : 
  - "Accepter la nouvelle valeur" (écrase l'existant)
  - "Conserver l'existant" (ignore l'import)
- Actions groupées :
  - "Tout valider" (accepte toutes les nouvelles valeurs)
  - "Tout refuser" (conserve toutes les anciennes valeurs)

**Validation**:
- Bouton "OK" désactivé tant que tous les conflits ne sont pas résolus
- Impossible d'appliquer l'import sans résoudre tous les conflits

### 3. Acceptation/Annulation

**Boutons de contrôle**:
- ✅ **"Accepter l'intégration"** : Applique l'import (désactivé si conflits non résolus)
- ❌ **"Annuler"** : Réinitialise le workflow, retourne au formulaire d'import

### 4. Rapport Final (Post-Import)

Après application de l'import, affichage d'un **rapport définitif** comprenant :

#### 4.1 Résumé de l'import
- Statut de réussite/échec
- Nombre d'entrées importées avec succès

#### 4.2 Résolutions de conflits appliquées

**Tableau détaillé** (si des conflits ont été résolus) :
- 📊 Statistiques : 
  - Nombre total de conflits résolus
  - Nombre de nouvelles valeurs appliquées
  - Nombre d'anciennes valeurs conservées
- **Colonnes du tableau** :
  - Personnage
  - Champ modifié
  - Date du classement
  - Ancienne valeur (barrée si écrasée, en vert si conservée)
  - Nouvelle valeur (en vert si appliquée, barrée si refusée)
  - Résolution appliquée :
    - ✅ Badge vert "Nouvelle appliquée" (si écrasement)
    - 🔄 Badge gris "Ancienne conservée" (si conservée)

**Mise en valeur visuelle** :
- Dégradé de fond pour distinguer la section
- Icône "task_alt" pour indiquer la résolution
- Valeurs appliquées en gras et vert
- Valeurs ignorées barrées et en gris

#### 4.3 Rapport d'import détaillé

**Structure hiérarchique** :
- Groupement par **catégorie** (Classement, Commandant, Mercenaires, etc.)
- Groupement par **type de données** (Structure, Puissance, Composition, etc.)
- Liste des **messages de log** avec badges de niveau (Ok/Warning/Error)

**Informations incluses** :
- Dates de classement importées
- Validations de composition (8 mercenaires, 3 androides)
- Erreurs de parsing ou validation
- Données ignorées (doublons, classements moins bons)

## Modèles de Données

### ImportPreviewResult
```csharp
- bool IsSuccess
- string? Error
- List<ImportLogEntry> Logs
- List<ImportConflict> Conflicts
- int ValidCount
- bool HasConflicts
```

### ImportConflict
```csharp
- string PersonnageName
- string ChampModifie
- DateOnly DateClassement
- object? AncienneValeur
- object? NouvelleValeur
- string ConflictKey
```

### ConflictResolutionApplied
```csharp
- string PersonnageName
- string ChampModifie
- DateOnly DateClassement
- object? AncienneValeur
- object? NouvelleValeur
- bool Overwritten  // true = nouvelle appliquée, false = ancienne conservée
```

### ImportLogEntry
```csharp
- ImportLogLevel Level (Ok, Warning, Error)
- ImportLogCategory Category (General, Classement, Commandant, Mercenaires, Androides, Lucie, Capacites)
- string DataType
- string Message
```

## Flux Technique

### Étape 1 : Prévisualisation
1. Upload du fichier PML
2. Appel `PreviewPmlClassementsAsync(Stream)`
3. Parsing XML sans écriture DB
4. Validation de chaque classement
5. Détection des conflits via `DetectHistoriqueConflicts()`
6. Retour d'un `ImportPreviewResult` avec logs et conflits

### Étape 2 : Résolution
1. Affichage des conflits dans l'UI
2. Utilisateur choisit pour chaque conflit (overwrite ou keep)
3. Stockage des décisions dans `Dictionary<string, bool>`
4. Validation : tous les conflits doivent avoir une décision

### Étape 3 : Application
1. Si conflits : appel `ImportPmlWithConflictResolution(Stream, fileName, resolutions, originalConflicts)`
2. Si pas de conflits : appel `ImportPmlAsync(Stream, fileName, ...options)`
3. Construction du rapport avec `ConflictsApplied` basé sur `originalConflicts`
4. Écriture en base de données
5. Retour d'un `ImportResult` avec logs et résolutions appliquées

### Étape 4 : Rapport Final
1. Affichage du statut de réussite
2. Si `ConflictsApplied` non vide : affichage du tableau des résolutions
3. Affichage du rapport détaillé des logs
4. Bouton "Réinitialiser" pour recommencer

## Services Impliqués

- **PmlImportService** :
  - `PreviewPmlClassementsAsync()` : Prévisualisation sans DB
  - `ImportPmlWithConflictResolution()` : Import avec résolutions
  - `DetectHistoriqueConflicts()` : Détection des conflits
  
- **ImportExportPmlModal** :
  - Gestion du workflow UI
  - Affichage des rapports
  - Résolution des conflits

## Bénéfices Utilisateur

✅ **Transparence** : L'utilisateur voit exactement ce qui va être importé avant validation

✅ **Contrôle** : Résolution manuelle ou en masse des conflits avec visualisation claire

✅ **Traçabilité** : Rapport final détaillé des actions effectuées et résolutions appliquées

✅ **Sécurité** : Validation stricte et impossibilité d'importer avec des conflits non résolus

✅ **Clarté** : Logs structurés par catégorie avec codes couleur (vert/orange/rouge)

## Date de Création

Janvier 2026
