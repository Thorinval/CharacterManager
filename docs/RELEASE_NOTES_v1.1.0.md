# 📋 Release Notes - Version 1.1.0

> **Character Manager v1.1.0**  
> **Date de release** : 25 janvier 2026  
> **Status** : ✅ Production  
> **Branche** : `1.x_futures_version`

---

## 🎯 Vue d'ensemble

La version **1.1.0** marque une étape majeure pour Character Manager avec la **refonte complète du système d'import/export PML** et des **améliorations significatives de l'interface utilisateur**. Cette version se concentre sur la **stabilité**, l'**ergonomie** et la **prévention des doublons**.

### 🎖️ Points clés
- ✅ Import PML assisté avec prévisualisation et résolution de conflits
- ✅ Nettoyage automatisé des doublons (module admin)
- ✅ Interface utilisateur complètement uniformisée
- ✅ Calcul de la puissance réelle pour les commandants
- ✅ 78/78 tests unitaires ✅

---

## ✨ Nouvelles Fonctionnalités

### 1️⃣ Import PML Assisté 🔄

#### Workflow amélioré en 3 étapes

**Étape 1 : Prévisualisation**
- Upload du fichier PML/XML
- Détection automatique des conflits
- Rapport préliminaire détaillé
- Affichage des statistiques

**Étape 2 : Résolution de conflits**
- Vue centralisée de tous les conflits détectés
- Choix entre **nouvelle valeur** ou **ancienne valeur** pour chaque conflit
- Boutons d'action groupée : "Tout valider" / "Tout refuser"
- Compteur en temps réel (conflits totaux / résolus / non résolus)
- Impossible d'appliquer l'import tant que **tous les conflits ne sont pas résolus**

**Étape 3 : Application et rapport**
- Importation des données
- Rapport final détaillé avec statistiques :
  - Nombre de conflits résolus
  - Nouvelles valeurs appliquées
  - Anciennes valeurs conservées
- Tableau de résolutions avec badges visuels

#### Catégories de logs structurés
- 📋 **Général** : Informations globales de l'import
- 🏆 **Classement** : Données de classement
- ⚔️ **Commandant** : Historiques des commandants
- 💼 **Mercenaires** : Données des mercenaires
- 🤖 **Androïdes** : Données des androïdes
- 🏠 **Lucie** : Données de la maison de Lucie
- ⚡ **Capacités** : Données des capacités

Chaque log dispose d'indicateurs visuels :
- ✅ OK - Action réussie
- ⚠️ WARNING - Attention requise
- ❌ ERROR - Erreur détectée

#### Types de conflits détectés
- Historiques de modification dupliqués
- Valeurs antérieures incohérentes
- Personnages manquants ou mal référencés
- Doublons de données

### 2️⃣ Nettoyage des Doublons 🎯

#### Module Admin Dédié
Accessible via `/admin/cleanup-duplicates`

**Fonctionnalités**
- Détection automatique des doublons de personnages
- Groupement par similarité de nom
- Correction des références d'historique
- Fusionnement des données
- Historisation des opérations de nettoyage

**Recommandations**
- ⚠️ Faire une **sauvegarde** avant nettoyage
- ✅ Exécuter après un import si des doublons sont détectés

### 3️⃣ Maison de Lucie - Édition Inline ✏️

#### Édition des pièces
- **Mode édition** : Cliquer sur une pièce pour activer le mode édition
- **Champs modifiables** :
  - Niveau de la pièce
  - Puissance Tactique
  - Puissance Stratégique
  - État "Sélectionnée"

#### Feedback visuel
- Bordure bleue (#667eea) autour des pièces en édition
- Boutons Sauvegarder / Annuler explicites
- Feedback utilisateur immédiat

#### Historisation automatique
- Toutes les modifications enregistrées dans l'historique
- Traçabilité complète des modifications
- Consultable via le page "Historique des modifications"

### 4️⃣ Puissance Réelle des Commandants 💪

#### Calcul automatique
```
Puissance Réelle = Puissance + (Rang × 20)
```

#### Affichage unifié
- Visible sur toutes les pages : Inventaire, Escouade, Meilleure Escouade, Classements
- Format : **Puissance (Puissance Réelle)**
- Affichage en gras pour meilleure visibilité

#### Tri amélioré
- Tri par puissance utilise la **puissance réelle** pour les commandants
- Tri correct pour mercenaires et androïdes

---

## 🎨 Amélioration de l'Interface Utilisateur

### 1️⃣ Uniformisation des Headers

**Avant**
- Headers avec fonds blancs opaques
- Styles incohérents selon les pages
- Espacement variable

**Après**
- ✅ Headers transparents avec `border-bottom` subtile
- ✅ Design cohérent inspiré de MaisonLucie
- ✅ Padding standardisé : `0 0 1.25rem 0`
- ✅ Border subtile : `rgba(0, 0, 0, 0.1)`
- ✅ Couleur texte : `#1e293b`
- ✅ Icônes bleues : `#667eea`

**Pages affectées**
- Capacités
- Inventaire
- Templates
- Historique
- Historique des Modifications
- Historique des Ligues
- Gestion des Utilisateurs
- Statistiques

### 2️⃣ Espacement et Layout

**Amélioration**
- Ajout de `padding-top: 2rem` au contenu principal
- Évite que les pages ne soient collées à la top bar
- Meilleure respiration visuelle

**Pages conservant leur style**
- Escouade
- Meilleure Escouade
- (Overrides explicites conservés)

### 3️⃣ Suppression des Overrides CSS Locaux

**Nettoyage effectué**
- Consolidation en `app.css` centralisé
- Suppression des fichiers CSS redondants
- Héritage cohérent du style global

**Fichiers mis à jour**
- `Templates.css` - Héritage du header global
- `Histoligues.css` - Conversion du fond bleu en transparent
- `Historique.css` - Suppression du gradient
- `HistoriqueModifications.css` - Suppression des overrides
- `Capacites.css` - Centralisation des styles
- `Inventaire.css` - Unified styling

---

## 🔧 Améliorations Techniques

### 1️⃣ Logging Enrichi

#### Contexte EF Core
Logs debug détaillés avant les requêtes `FirstOrDefault` :
- Affichage des critères de recherche
- Nom du personnage/utilisateur cherché
- Détails des opérations d'export

#### Services instrumentés
- `PersonnageService` ✅
- `ProfileService` ✅
- `PmlExportService` ✅

#### Configuration Serilog
```json
{
  "Properties": {
    "Application": "CharacterManager"
  },
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "CharacterManager.Server.Services": "Debug"
    }
  }
}
```

### 2️⃣ Modèles de Données Enrichis

**Import Preview Result**
```csharp
public class ImportPreviewResult
{
    public List<ImportLogEntry> ImportLogs { get; set; }
    public List<ImportConflict> Conflicts { get; set; }
    public int ConflictCount { get; set; }
}
```

**Import Conflict**
```csharp
public class ImportConflict
{
    public string PersonnageId { get; set; }
    public string Field { get; set; }
    public DateTime ModificationDate { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public bool UserChoice { get; set; } // true = nouvelle, false = ancienne
}
```

**Import Log Entry**
```csharp
public enum ImportLevel { Ok, Warning, Error }
public enum ImportCategory { General, Ranking, Commander, Mercenary, Android, Lucie, Abilities }

public class ImportLogEntry
{
    public ImportLevel Level { get; set; }
    public ImportCategory Category { get; set; }
    public string DataType { get; set; }
    public string Message { get; set; }
}
```

---

## 🧪 Qualité et Tests

### Test Coverage

| Catégorie | Tests | Status |
|-----------|-------|--------|
| **Total** | **78** | ✅ PASS |
| PersonnageService | 15 | ✅ PASS |
| HistoriqueModificationService | 23 | ✅ PASS |
| PmlImportService | 18 | ✅ PASS |
| ValidationService | 12 | ✅ PASS |
| ProfileService | 10 | ✅ PASS |

### Nouveaux Tests Ajoutés

#### Détection de Conflits
```
✅ ImportConflicts_WhenHistoryPersonageMissing
```
Valide la détection des conflits quand un personnage référencé n'existe pas

#### Recalculation des Valeurs
```
✅ OldValueRecalculated_WhenPriorModificationArrives
```
Valide la recalculation des valeurs anciennes lors de l'arrivée d'une modification antérieure

### Mise à jour des Tests Existants
- Ajout des mocks `ILogger<T>` dans tous les tests
- Maintien de la couverture existante
- Aucune régression détectée

---

## 📥 Export/Import - Format PML

### Workflow Complet

**Export (Inventaire / Templates / Historique)**
1. Clic sur "Exporter"
2. Format généré : `.pml` (XML personnalisé)
3. Téléchargement avec timestamp : `export_2026-01-25.pml`

**Import (Assistant en 3 étapes)**
1. **Sélection** du fichier `.pml`
2. **Prévisualisation** avec détection des conflits
3. **Résolution** des conflits de manière supervisée
4. **Application** avec rapport détaillé

### Format PML Supporté

```xml
<?xml version="1.0" encoding="utf-8"?>
<CharacterManager>
  <Metadata>
    <ExportDate>2026-01-25T15:30:00Z</ExportDate>
    <Version>1.1.0</Version>
    <ExportType>Full|Partial</ExportType>
  </Metadata>
  <Personnages>
    <!-- Données des personnages -->
  </Personnages>
  <Historiques>
    <!-- Données d'historique -->
  </Historiques>
</CharacterManager>
```

---

## 🐛 Corrections de Bugs

### Localisation
- ✅ Correction des accolades manquantes dans `fr.json`
- ✅ Correction des accolades manquantes dans `en.json`
- ✅ Parsing JSON maintenant correct

### Structure HTML
- ✅ Correction des balises `<div>` mal fermées dans `Capacites.razor`
- ✅ Build validation pass (RZ9980, RZ9981, RZ1026)
- ✅ Build sans avertissements

### CSS
- ✅ Suppression des overrides redondants
- ✅ Héritage cohérent du style global
- ✅ Pas de régressions visuelles

---

## 🚀 Déploiement et Installation

### Prérequis
- Windows 10+ ou Linux/MacOS avec Docker
- .NET 9.0 Runtime (installeur inclus)
- SQLite (embarqué)

### Installation Windows
1. Télécharger `CharacterManager-1.1.0-Setup.exe`
2. Exécuter l'installateur
3. Suivre les instructions
4. L'application se lance au démarrage

### Mise à Jour Depuis v1.0.x
1. ⚠️ **Backup** de `charactermanager.db`
2. Installer v1.1.0 (préserve les données)
3. ✅ Module nettoyage doublons disponible si nécessaire
4. Relancer l'application

### Docker
```bash
docker-compose up -d
# Accès : http://localhost:8080
```

---

## ⚠️ Notes Importantes

### Base de Données
- ✅ Migration automatique v1.0.x → v1.1.0
- ⚠️ Backup recommandé avant nettoyage des doublons
- 📁 Localisation : `~/.CharacterManager/charactermanager.db`

### Authentification
- Credentials par défaut conservés de v1.0.0
- **Changez le mot de passe admin immédiatement**
- Consultez `appsettings.json` pour les identifiants

### Import PML
- Accepte uniquement `.pml` (XML valide)
- `Import PML` page ne change pas le workflow
- `Importer l'inventaire` utilise le nouvel assistant 3 étapes

### Nettoyage Doublons
- ✨ **Module Admin** : `/admin/cleanup-duplicates`
- ⚠️ **À utiliser après un import** si doublons détectés
- 📊 Affiche le nombre de doublons trouvés
- ✅ Historisation complète des nettoyages

---

## 📊 Statistiques

| Métrique | Valeur |
|----------|--------|
| **Nouvelle pages** | 2 (Import assistant, Admin cleanup) |
| **Fichiers modifiés** | 45+ |
| **Tests unitaires** | 78 ✅ |
| **Couverture** | ~85% |
| **Taille du build** | ~92 MB |
| **Durée du build** | ~45 secondes |

---

## 🔄 Comparaison v1.0.0 → v1.1.0

### Améliorations Majeures

| Fonctionnalité | v1.0.0 | v1.1.0 |
|---|---|---|
| Import/Export | Simple | **Assisté 3 étapes** |
| Détection de conflits | ❌ | ✅ |
| Nettoyage doublons | ❌ | ✅ Admin panel |
| Édition Lucie | ❌ | ✅ Inline |
| Puissance réelle | ❌ | ✅ Automatique |
| UI uniforme | Partielle | ✅ Complète |
| Tests | 60/60 | ✅ 78/78 |
| Logging | Basique | **Structuré** |

---

## 🎯 Prochaines Étapes (v1.2.0+)

### En Préparation
- 🔄 Refonte du système de classement
- 🎨 Améliorations UX supplémentaires
- 🌍 Support multilingue étendu
- 📊 Nouveaux graphiques
- ⚡ Optimisations de performance

### Roadmap Complète
Voir [ROADMAP.md](ROADMAP.md) pour plus de détails.

---

## 📝 Guide de Migration

### Utilisateurs v1.0.0
1. ✅ Mise à jour via installeur (données préservées)
2. ⚠️ Testez les imports
3. 🧹 Utilisez le nettoyage doublons si besoin
4. 📝 Consultez [RELEASE_NOTES.md](RELEASE_NOTES.md) pour plus infos

### Développeurs
1. Checkout branche `main`
2. `git pull origin main`
3. `dotnet restore && dotnet build`
4. `dotnet run` dans dossier `CharacterManager/`

---

## 🤝 Support et Retours

### Signaler un Bug
- 🐛 [Ouvrir une issue](https://github.com/Thorinval/CharacterManager/issues)
- Inclure : version, étapes de reproduction, logs

### Suggestions
- 💡 [Ouvrir une discussion](https://github.com/Thorinval/CharacterManager/discussions)
- 📧 Contact via GitHub

### Ressources
- 📖 [Documentation complète](../DOCUMENTATION.md)
- 🚀 [Guide d'installation](INSTALLATION_GUIDE.md)
- 📚 [Quick Start](QUICK_START.md)

---

## ✅ Checklist de Validation

Avant la mise en production, s'assurer que :

- [x] Tous les tests passent (78/78)
- [x] Build sans erreurs
- [x] Pas de régressions détectées
- [x] Installer fonctionne correctement
- [x] Import/Export testé manuellement
- [x] Module nettoyage doublons testé
- [x] Localisation validée (FR/EN)
- [x] Documentation à jour
- [x] Release notes rédigées
- [x] Version bumped (1.0.0 → 1.1.0)

---

## 📎 Annexes

### A. Modèles de Données Modifiés
- `HistoriqueModification` - Campos ajoutés pour tracking
- `ImportConflict` - Nouveau modèle
- `ImportLogEntry` - Nouveau modèle
- `ImportPreviewResult` - Nouveau modèle

### B. Services Modifiés
- `PersonnageService` - Ajout logging + édition Lucie
- `PmlImportService` - Refonte complète
- `HistoriqueModificationService` - Logging enrichi
- `ProfileService` - Logging ajouté

### C. Pages Blazor Modifiées
- `Inventaire.razor` - Header unifié + UI amélioré
- `MaisonLucie.razor` - Édition inline + historisation
- `Import.razor` - **Nouveau** - Assistant 3 étapes
- `AdminCleanupDuplicates.razor` - **Nouveau** - Module nettoyage

### D. Fichiers de Configuration
- `appsettings.json` - Configuration Serilog mise à jour
- `CharacterManager.csproj` - Dépendances sans changements majeurs
- `CharacterManager.iss` - Version mise à jour (1.1.0)

---

<div align="center">

**Made with ❤️ by Thorinval**

[GitHub](https://github.com/Thorinval) • [Repository](https://github.com/Thorinval/CharacterManager)

**Version 1.1.0 - Production Ready ✅**

</div>
