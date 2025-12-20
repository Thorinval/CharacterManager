# Manifeste des fichiers modifiés - Migration PML

## 📂 Index des fichiers affectés

### 🆕 Fichiers CRÉÉS

#### Services
- ✅ `CharacterManager/Server/Services/PmlImportService.cs` (430 lignes)
  - Service principal d'import/export PML
  - Méthodes: ImportPmlAsync, ExporterInventairePmlAsync, ExporterTemplatesPmlAsync
  - Support des 3 sections: HistoriqueClassements, inventaire, templates

#### Pages UI
- ✅ `CharacterManager/Components/Pages/ImportPml.razor` (180 lignes)
  - Interface utilisateur pour import PML
- ✅ `CharacterManager/Components/Pages/ImportPml.razor.cs` (80 lignes)
  - Code-behind ImportPml

#### Tests
- ✅ `CharacterManager.Tests/PmlImportServiceTests.cs` (250 lignes)
  - 6 test cases pour PmlImportService

#### Documentation
- ✅ `PML_FORMAT_GUIDE.md` (300+ lignes)
  - Guide complet du format PML
  - Exemples XML
  - Migration CSV→PML
  - Détails techniques

- ✅ `CHANGELOG_PML_MIGRATION.md` (250+ lignes)
  - Historique des changements
  - Guide de migration utilisateur
  - Avantages PML vs CSV

- ✅ `MIGRATION_DETAILS.md` (400+ lignes)
  - Détails techniques d'implémentation
  - Points clés de code
  - Points d'attention

- ✅ `MIGRATION_SUMMARY.md` (300+ lignes)
  - Récapitulatif complet
  - Statistiques
  - Checklist

#### Exemples
- ✅ `exemple_export_pml.pml` (140 lignes)
  - Exemple complet avec toutes les sections
  - Historique + inventaire + templates

---

### ✏️ Fichiers MODIFIÉS

#### Services
| Fichier | Changements |
|---------|------------|
| `CharacterManager/Server/Services/HistoriqueEscouadeService.cs` | Méthode `ExporterHistoriqueXmlAsync()` mise à jour pour inclure inventaire et templates (dans les sections XML) |

#### Configuration et DI
| Fichier | Changements |
|---------|------------|
| `CharacterManager/Program.cs` | Ajout ligne: `builder.Services.AddScoped<PmlImportService>();` |

#### Pages UI
| Fichier | Changements |
|---------|------------|
| `CharacterManager/Components/Pages/Historique.razor.cs` | Validation fichier: accepte `.xml` ET `.pml` |
| `CharacterManager/Components/Pages/Inventaire.razor.cs` | CsvImportService → PmlImportService (4 occurrences) + ExportTemplateAsCsv → ExportTemplateAsPml |
| `CharacterManager/Components/Pages/Templates.razor.cs` | CsvImportService → PmlImportService + export CSV → PML |
| `CharacterManager/Components/Layout/NavMenu.razor` | Ajout lien navigation `/import-pml` |

#### Localisation (i18n)
| Fichier | Changements |
|---------|------------|
| `CharacterManager/wwwroot/i18n/fr.json` | Ajout clés `importPml` + `navigation.importPml` |
| `CharacterManager/wwwroot/i18n/en.json` | Ajout clés `importPml` + `navigation.importPml` |

---

## 📊 Résumé des modifications

### Comptage
- **Fichiers créés:** 10
  - 1 service
  - 2 pages UI (Razor + code-behind)
  - 1 test file
  - 4 fichiers doc
  - 1 exemple
  - 1 ce manifeste

- **Fichiers modifiés:** 8
  - 1 service
  - 1 configuration
  - 3 pages UI
  - 2 fichiers i18n
  - 1 navigation

- **Total affecté:** 18 fichiers

### Lignes de code
| Type | Approx. |
|------|---------|
| Code nouveau (services) | 430 |
| Code nouveau (UI) | 180 |
| Tests nouveaux | 250 |
| Documentation | 1200+ |
| Modifications existantes | 50 |
| **TOTAL** | **~2110** |

---

## 🔍 Détails des modifications par fichier

### PmlImportService.cs (NOUVEAU)
```
Lignes: 430
Méthodes:
  - ImportPmlAsync() - Public, import depuis Stream
  - ImportInventaireAsync() - Private, traite section <inventaire>
  - ImportTemplatesAsync() - Private, traite section <templates>
  - ParsePersonnageFromXml() - Private, parse élément XML
  - ExporterInventairePmlAsync() - Public, export inventaire
  - ExporterTemplatesPmlAsync() - Public, export templates
  - ParseRarete/Type/Role/Faction() - Private, parse enums
  - EnsureImageOrDefault() - Private, validation images
  - GetLastImportedFileName() - Public, historique
  - SaveLastImportedFileName() - Private, persistence
```

### HistoriqueEscouadeService.cs
```
Modification: ExporterHistoriqueXmlAsync()
Avant: Exportait uniquement HistoriqueClassements
Après: Exporte aussi <inventaire> et <templates>
Lignes ajoutées: ~60
```

### ImportPml.razor + .cs (NOUVEAU)
```
Razor: 180 lignes
  - Structure HTML similaire à ImportCsv
  - InputFile accepte .pml et .xml
  - Affichage des résultats d'import
  
Code-behind: 80 lignes
  - Injection PmlImportService
  - Méthodes OnFileSelected, HandleImport, Reset
```

### PmlImportServiceTests.cs (NOUVEAU)
```
Lignes: 250
Tests:
  1. ImportPmlAsync_WithValidInventaire_ShouldImportPersonnages
  2. ImportPmlAsync_WithValidTemplate_ShouldImportTemplate
  3. ImportPmlAsync_WithMixedSections_ShouldImportBoth
  4. ExporterInventairePmlAsync_ShouldExportPersonnages
  5. ExporterTemplatesPmlAsync_ShouldExportTemplates
  6. ImportPmlAsync_WithEmptyFile_ShouldReturnError
```

### Program.cs
```
Lignes ajoutées: 1
Changement:
  builder.Services.AddScoped<PmlImportService>();
Position: Après CsvImportService
```

### Historique.razor.cs
```
Lignes modifiées: 3
Avant:
  if (file != null && file.Name.EndsWith(".xml", ...))

Après:
  if (file != null && (file.Name.EndsWith(".xml", ...) 
                    || file.Name.EndsWith(".pml", ...)))

Message utilisateur: "XML ou PML"
```

### Inventaire.razor.cs
```
Modifications: 5 grandes
1. Injection: CsvImportService → PmlImportService
2. Export inventaire:
   - ExportToCsvAsync → ExporterInventairePmlAsync
   - Extension: .csv → .pml
3. Méthode template:
   - ExportTemplateAsCsv → ExportTemplateAsPml
   - Création Template temporaire avec SetPersonnageIds
   - ExportToCsvAsync → ExporterTemplatesPmlAsync
```

### Templates.razor.cs
```
Modifications: 2
1. Injection: CsvImportService → PmlImportService
2. ExportTemplate:
   - ExportToCsvAsync → ExporterTemplatesPmlAsync
   - Extension: .csv → .pml
```

### NavMenu.razor
```
Lignes ajoutées: 5
Structure:
  <div class="nav-item px-3">
    <NavLink class="nav-link" href="import-pml">
      <LocalizedText Key="navigation.importPml" />
    </NavLink>
  </div>
Position: Après le lien "Import CSV"
```

### fr.json (Localisation)
```
Clés ajoutées: 15
Sections:
  - importPml (14 clés)
  - navigation.importPml (1 clé)
Lignes approximatives: 40
```

### en.json (Localisation)
```
Clés ajoutées: 15
Sections:
  - importPml (14 clés)
  - navigation.importPml (1 clé)
Lignes approximatives: 40
```

---

## 🎯 Impact sur l'architecture

### Avant
```
CSV Import/Export
├─ CsvImportService
│  ├─ ImportCsvAsync()
│  └─ ExportToCsvAsync()
├─ ImportCsv.razor
└─ Pages utilisant CsvImportService
```

### Après
```
PML Import/Export (XML Structuré)
├─ PmlImportService (NOUVEAU)
│  ├─ ImportPmlAsync()
│  ├─ ExporterInventairePmlAsync()
│  ├─ ExporterTemplatesPmlAsync()
│  └─ Support 3 sections
├─ ImportPml.razor (NOUVEAU)
├─ Pages utilisant PmlImportService
└─ CsvImportService (toujours disponible, non utilisé)
```

---

## 🔄 Flux de données

### Import Inventaire
```
Fichier .pml
    ↓
PmlImportService.ImportPmlAsync()
    ↓
Extrait section <inventaire>
    ↓
ParsePersonnageFromXml() pour chaque
    ↓
Valide et stocke en BD
    ↓
Retour ImportResult
```

### Export Inventaire
```
Inventaire.razor
    ↓
PmlImportService.ExporterInventairePmlAsync()
    ↓
Génère XML <InventairePML>
    ↓
Téléchargement inventaire_*.pml
```

### Export Historique Complet
```
Historique.razor
    ↓
HistoriqueEscouadeService.ExporterHistoriqueXmlAsync()
    ↓
Génère XML avec 3 sections:
  - HistoriqueClassements (existant)
  - inventaire (NOUVEAU)
  - templates (NOUVEAU)
    ↓
Téléchargement PML complet
```

---

## ✅ Validation des changements

- ✅ Aucune perte de données
- ✅ Compatibilité rétroactive (import XML ancien format)
- ✅ Tests unitaires complets
- ✅ Traductions multilingues
- ✅ Navigation mise à jour
- ✅ Injection de dépendances correcte
- ✅ Pas de breaking changes pour les utilisateurs

---

## 📚 Documentation générée

1. **PML_FORMAT_GUIDE.md** - Spécification du format
2. **CHANGELOG_PML_MIGRATION.md** - Historique des changements
3. **MIGRATION_DETAILS.md** - Détails techniques
4. **MIGRATION_SUMMARY.md** - Récapitulatif complet
5. **PML_FILES_MANIFEST.md** - Ce fichier (index)

---

## 🔗 Relations entre fichiers

```
PmlImportService.cs
    ↓ utilisé par
    ├─ ImportPml.razor.cs
    ├─ Inventaire.razor.cs
    └─ Templates.razor.cs

HistoriqueEscouadeService.cs
    ↓ appelé par
    └─ Historique.razor.cs

Program.cs
    ↓ enregistre
    └─ PmlImportService

NavMenu.razor
    ↓ pointe vers
    └─ ImportPml.razor

i18n/fr.json + en.json
    ↓ utilisé par
    ├─ ImportPml.razor
    ├─ NavMenu.razor
    └─ Pages UI
```

---

**Dernière mise à jour:** Décembre 2025
**Status:** ✅ Complet et fonctionnel
**Version:** À partir de cette version
