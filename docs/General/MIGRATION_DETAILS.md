# Résumé des modifications - Migration CSV → PML

## 🎯 Objectif
Remplacer complètement le système d'import CSV par un système XML structuré (PML) supportant les sections inventaire et templates.

## 📋 Modifications principales

### 1️⃣ Nouveau Service: PmlImportService
**Fichier:** `CharacterManager/Server/Services/PmlImportService.cs`

**Responsabilités:**
- Import/Export au format PML
- Gestion des 3 sections: HistoriqueClassements, inventaire, templates
- Validation des données
- Gestion des doublons

**Points clés d'implémentation:**
- Utilise `System.Xml.Linq` pour parser les XML
- Supporte les templates avec stockage JSON des IDs (T.GetPersonnageIds())
- Traçabilité du dernier fichier importé via AppSettings
- Parsing des enums (Rarete, Role, Faction) depuis les valeurs XML

### 2️⃣ Service mis à jour: HistoriqueEscouadeService
**Fichier:** `CharacterManager/Server/Services/HistoriqueEscouadeService.cs`

**Changements:**
```csharp
// Avant: Exportait uniquement l'historique en XML
public async Task<byte[]> ExporterHistoriqueXmlAsync()

// Après: Exporte un PML complet avec inventaire ET templates
public async Task<byte[]> ExporterHistoriqueXmlAsync()
```

**Nouvelles sections dans l'export:**
- `<inventaire>` : Tous les personnages de la BD
- `<templates>` : Tous les templates avec leurs personnages

### 3️⃣ Pages UI modifiées

#### ImportPml.razor (NOUVELLE)
**Pages:** `Components/Pages/ImportPml.razor` et `ImportPml.razor.cs`
- Interface d'import pour fichiers PML
- Accepte les extensions `.pml` et `.xml`
- Affiche les résultats détaillés

#### Historique.razor.cs
```csharp
// Avant: Acceptait uniquement .xml
if (file.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))

// Après: Accepte .xml ET .pml
if (file != null && (file.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) 
                  || file.Name.EndsWith(".pml", StringComparison.OrdinalIgnoreCase)))
```

#### Inventaire.razor.cs
```csharp
// Avant
public CsvImportService CsvImportService { get; set; }

// Après
public PmlImportService PmlImportService { get; set; }

// Avant
var csvBytes = await CsvImportService.ExportToCsvAsync(personnagesAExporter);
var fileName = $"inventaire_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

// Après
var pmlBytes = await PmlImportService.ExporterInventairePmlAsync(personnagesAExporter);
var fileName = $"inventaire_{DateTime.Now:yyyyMMdd_HHmmss}.pml";

// Avant
private async Task ExportTemplateAsCsv()

// Après
private async Task ExportTemplateAsPml()
{
    var template = new Template { Nom = templateNom, Description = templateDescription };
    template.SetPersonnageIds(templateSelectedIds);
    var pmlBytes = await PmlImportService.ExporterTemplatesPmlAsync(new[] { template });
    // ...
}
```

#### Templates.razor.cs
```csharp
// Avant
public CsvImportService CsvImportService { get; set; }

// Après
public PmlImportService PmlImportService { get; set; }

// Avant
var csvBytes = await CsvImportService.ExportToCsvAsync(personnages);
var fileName = $"template_{template.Nom}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

// Après
var pmlBytes = await PmlImportService.ExporterTemplatesPmlAsync(new[] { template });
var fileName = $"template_{template.Nom}_{DateTime.Now:yyyyMMdd_HHmmss}.pml";
```

### 4️⃣ Programme et Injection de dépendances
**Fichier:** `Program.cs`

```csharp
// Ajout
builder.Services.AddScoped<PmlImportService>();
```

### 5️⃣ Navigation
**Fichier:** `Components/Layout/NavMenu.razor`

```html
<!-- Nouveau lien -->
<div class="nav-item px-3">
    <NavLink class="nav-link" href="import-pml">
        <LocalizedText Key="navigation.importPml" />
    </NavLink>
</div>
```

### 6️⃣ Internationalisation (i18n)
**Fichiers:** `wwwroot/i18n/fr.json`, `wwwroot/i18n/en.json`

**Nouvelles traductions:**
```json
{
  "navigation": {
    "importPml": "Import PML" // fr.json
    "importPml": "Import PML" // en.json
  },
  "importPml": {
    "title": "Import des fichiers PML",
    "subtitle": "Importer des personnages, templates ou historique...",
    "infoInventaire": "Les personnages de la section 'inventaire' seront importés...",
    "infoTemplate": "Les templates de la section 'templates' seront importés..."
    // ... autres clés
  }
}
```

### 7️⃣ Tests
**Fichier:** `CharacterManager.Tests/PmlImportServiceTests.cs` (NOUVEAU)

**Couverture:**
- Import d'inventaire
- Import de templates
- Export d'inventaire
- Export de templates
- Gestion des fichiers vides
- Gestion des cas mixtes (inventaire + templates)

## 🏗️ Structure PML

### Format complet
```xml
<?xml version="1.0" encoding="utf-8"?>
<HistoriqueEscouadePML version="1.0" exportDate="2025-12-20T15:30:00Z">
  <!-- Section 1: Historique des escouades -->
  <HistoriqueClassements>
    <Enregistrement ID="1">
      <informations>
        <Date>2025-12-13T00:00:00Z</Date>
        <Puissance>26980</Puissance>
        ...
      </informations>
      ...
    </Enregistrement>
  </HistoriqueClassements>

  <!-- Section 2: Base de données des personnages -->
  <inventaire>
    <Personnage>
      <Nom>REGINA</Nom>
      <Rarete>SSR</Rarete>
      <Type>Mercenaire</Type>
      ...
    </Personnage>
  </inventaire>

  <!-- Section 3: Templates d'équipes -->
  <templates>
    <template>
      <Nom>Mon Équipe</Nom>
      <Description>...</Description>
      <Personnage>...</Personnage>
    </template>
  </templates>
</HistoriqueEscouadePML>
```

## 🔄 Points importants d'implémentation

### Template et GetPersonnageIds()
Le modèle `Template` stocke les personnages en JSON, pas en collection:
```csharp
// ✅ Correct
var ids = template.GetPersonnageIds(); // Retourne List<int>
template.SetPersonnageIds(nouveauxIds); // Stocke en JSON

// ❌ Incorrect (causait les erreurs de compilation)
foreach (var p in template.Personnages) // Propriété inexistante!
```

### Parsing des Énums
Les valeurs doivent correspondre aux énums réels du système:
```csharp
// Énums disponibles
public enum Rarete { R, SR, SSR, Inconnu }
public enum Role { Sentinelle, Combattante, Androide, Commandant, Inconnu }
public enum Faction { Syndicat, Pacificateurs, HommesLibres, Inconnu }

// Pas de N en Rarete, pas de Guerrière/Tireuse/etc. en Role
// Pas d'Ordre/Androïde en Faction
```

### ImportResult réutilisable
La classe `ImportResult` est définie dans `CsvImportService` et réutilisée par `PmlImportService`:
```csharp
// Dans CsvImportService.cs
public class ImportResult { ... }

// Importé par PmlImportService (pas de duplication)
```

## 📦 Fichiers de référence

| Fichier | Description |
|---------|-------------|
| `exemple_export_pml.pml` | Exemple complet de fichier PML |
| `PML_FORMAT_GUIDE.md` | Guide complet du format PML |
| `CHANGELOG_PML_MIGRATION.md` | Changelog détaillé |
| `PmlImportService.cs` | Service principal PML |
| `PmlImportServiceTests.cs` | Tests unitaires |

## ✅ Checklist de validation

- ✅ Service PmlImportService créé et testé
- ✅ HistoriqueEscouadeService mis à jour
- ✅ Historique.razor.cs supporte .pml
- ✅ Inventaire.razor.cs utilise PmlImportService
- ✅ Templates.razor.cs utilise PmlImportService
- ✅ ImportPml.razor page créée
- ✅ Navigation mise à jour
- ✅ Traductions FR et EN ajoutées
- ✅ Tests unitaires PML créés
- ✅ Aucune erreur de compilation
- ✅ Documentation complète (guides + changelog)

## 🚀 Utilisation immédiate

**Pour l'utilisateur:**
1. Accéder à `/import-pml`
2. Sélectionner un fichier `.pml` ou `.xml`
3. Les données sont importées selon les sections présentes

**Pour l'export:**
1. Inventaire: Clic sur "Exporter" → génère `inventaire_*.pml`
2. Template: Clic sur "Exporter" → génère `template_*.pml`
3. Historique: Clic sur "Exporter" → génère PML complet avec inventaire + templates

## 📝 Notes de migration

- CsvImportService reste disponible pour compatibilité mais n'est plus utilisé
- Les fichiers XML existants continuent de fonctionner (import historique)
- Aucun change de structure BD - PML est un format de sérialisation

## 🔮 Évolutions futures possibles

- Validation XSD
- Compression ZIP
- API REST pour import/export
- Synchronisation multi-formats
- Versioning du format PML
