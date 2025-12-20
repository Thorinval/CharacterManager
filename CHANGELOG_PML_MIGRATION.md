# Changement d'architecture d'import/export - Format PML

## Résumé des changements

Le système d'import/export de CharacterManager a été complètement refondu pour abandonner le format CSV en faveur d'un format XML structuré appelé **PML (Personnage Markup Language)**.

**Date de changement:** Décembre 2025
**Version affectée:** À partir de cette version

## Nouveautés

### 1. **Nouveau format PML (.pml)**
- Extension recommandée: `.pml` (les fichiers `.xml` sont toujours supportés)
- Format: XML structuré basé sur le standard
- Structure: Peut contenir jusqu'à 3 sections principales
  - `<HistoriqueClassements>` : Historique des escouades
  - `<inventaire>` : Base de données des personnages
  - `<templates>` : Modèles d'équipes prédéfinis

### 2. **Nouveau service: PmlImportService**
Remplace complètement `CsvImportService` pour l'import/export d'inventaire et de templates.

**Localisation:** `CharacterManager/Server/Services/PmlImportService.cs`

**Méthodes principales:**
- `ImportPmlAsync(Stream stream, string fileName)` 
  - Importe les données depuis un fichier PML
  - Supporte les 3 sections (inventaire, templates, historique)
  
- `ExporterInventairePmlAsync(IEnumerable<Personnage> personnages)`
  - Exporte l'inventaire au format PML
  
- `ExporterTemplatesPmlAsync(IEnumerable<Template> templates)`
  - Exporte les templates au format PML

### 3. **Nouvelle page UI: /import-pml**
Page dédiée à l'import de fichiers PML pour l'inventaire et les templates.

**Fichiers:**
- `ImportPml.razor` - Interface utilisateur
- `ImportPml.razor.cs` - Logique métier

**Navigation:** Menu principal → "Import PML"

### 4. **Export unifié des données**
L'export depuis `HistoriqueEscouadeService.ExporterHistoriqueXmlAsync()` génère désormais un fichier PML complet contenant:
- Tous les enregistrements d'historique
- La base de données complète d'inventaire
- Tous les modèles de templates

Cela permet une sauvegarde et une restauration complètes du système.

## Pages modifiées

### Historique.razor
- ✅ Support des fichiers `.pml` en addition aux `.xml`
- ✅ La validation d'extension accepte les deux formats
- ✅ Import/Export au format PML

### Inventaire.razor
- ✅ Injection de `PmlImportService` au lieu de `CsvImportService`
- ✅ Méthode `ExportInventaire()` → exporte en PML
- ✅ Méthode `ExportTemplateAsPml()` (anciennement `ExportTemplateAsCsv()`)

### Templates.razor
- ✅ Injection de `PmlImportService` au lieu de `CsvImportService`
- ✅ Méthode `ExportTemplate()` mise à jour pour exporter en PML

### NavMenu.razor
- ✅ Ajout du lien "Import PML" dans la navigation

### Program.cs
- ✅ Enregistrement du service: `builder.Services.AddScoped<PmlImportService>();`

## Services modifiés

### PmlImportService (nouveau)
Service principal pour la gestion des fichiers PML.

**Caractéristiques:**
- Import de multiples formats de données
- Validation complète des données
- Gestion des doublons (mise à jour vs création)
- Support des templates avec gestion des IDs de personnages
- Traçabilité du dernier fichier importé

### HistoriqueEscouadeService
- ✅ `ExporterHistoriqueXmlAsync()` mise à jour
  - Génère maintenant un fichier PML complet
  - Inclut les sections inventaire et templates
  - Contient toujours l'historique complet

## Internationalization (i18n)

### Nouvelles clés de traduction

**Français (fr.json):**
```json
"importPml": {
  "title": "Import des fichiers PML",
  "subtitle": "Importer des personnages, templates ou historique depuis un fichier PML",
  ...
}

"navigation": {
  "importPml": "Import PML"
}
```

**Anglais (en.json):**
```json
"importPml": {
  "title": "Import PML files",
  "subtitle": "Import characters, templates or history from a PML file",
  ...
}

"navigation": {
  "importPml": "Import PML"
}
```

## Tests

### Nouveaux tests: PmlImportServiceTests
**Localisation:** `CharacterManager.Tests/PmlImportServiceTests.cs`

**Tests inclus:**
- `ImportPmlAsync_WithValidInventaire_ShouldImportPersonnages()`
- `ImportPmlAsync_WithValidTemplate_ShouldImportTemplate()`
- `ImportPmlAsync_WithMixedSections_ShouldImportBoth()`
- `ExporterInventairePmlAsync_ShouldExportPersonnages()`
- `ExporterTemplatesPmlAsync_ShouldExportTemplates()`
- `ImportPmlAsync_WithEmptyFile_ShouldReturnError()`

## Migration depuis CSV

### Pour les utilisateurs

Si vous aviez des fichiers CSV structurés, vous pouvez toujours les importer en:

1. Convertissant manuellement le format CSV en XML
2. Ou en utilisant des outils de conversion tiers
3. Ou en exportant depuis l'inventaire et en utilisant le format PML

**Structure CSV → PML:**

**CSV:**
```csv
Personnage;Rareté;Type;Puissance;PA;PV;...
REGINA;SSR;Mercenaire;3320;140;509;...
```

**PML/XML:**
```xml
<inventaire>
  <Personnage>
    <Nom>REGINA</Nom>
    <Rarete>SSR</Rarete>
    <Type>Mercenaire</Type>
    <Puissance>3320</Puissance>
    <PA>140</PA>
    <PV>509</PV>
    ...
  </Personnage>
</inventaire>
```

### Pour les développeurs

- ❌ `CsvImportService` est maintenant déprécié (mais toujours disponible)
- ✅ Utilisez `PmlImportService` pour tous les nouveaux imports/exports
- ✅ Consultez `PML_FORMAT_GUIDE.md` pour les détails techniques

## Avantages de PML vs CSV

| Aspect | CSV | PML (XML) |
|--------|-----|-----------|
| **Extensibilité** | Faible | Excellente |
| **Structures complexes** | Limitées | Complètes |
| **Validation** | Manuelle | Schéma XML |
| **Templating** | Difficile | Natif |
| **Historique complet** | Non | Oui |
| **Import sélectif** | Non | Oui (sections) |
| **Imbrication de données** | Non | Oui |
| **Lisibilité** | Moyenne | Excellente |

## Fichiers de référence

- `exemple_export_pml.pml` - Exemple complet de fichier PML
- `PML_FORMAT_GUIDE.md` - Guide complet du format PML
- `CsvImportService.cs` - Ancien service (toujours disponible, déprécié)

## Dépendances

Aucune dépendance externe supplémentaire. Le format PML utilise:
- `System.Xml.Linq` (standard .NET)
- `System.Text.Json` (standard .NET)

## Notes de compatibilité

✅ **Rétro-compatibilité**: Les fichiers XML existants sont toujours importables par `HistoriqueEscouadeService.ImporterHistoriqueAsync()`

⚠️ **Export uniquement**: `CsvImportService` est maintenu pour la compatibilité mais ne sera plus utilisé dans la navigation standard

## Étapes futures (non implémentées)

- [ ] Migration complète de la documentation vers PML
- [ ] Outil de conversion CSV → PML
- [ ] Support du schéma XSD pour validation
- [ ] API REST pour import/export PML
- [ ] Compression des fichiers PML en ZIP

## Support

Pour toute question sur le nouveau format PML:
1. Consultez `PML_FORMAT_GUIDE.md`
2. Vérifiez les tests dans `PmlImportServiceTests.cs`
3. Examinez l'exemple dans `exemple_export_pml.pml`
