# Nouvelle Architecture d'Import/Export - Format PML

## Vue d'ensemble

À partir de cette version, le système d'import/export de CharacterManager utilise le format **PML (Personnage Markup Language)**, basé sur XML, avec l'extension `.pml` pour les fichiers de données structurées.

Le système CSV est maintenant déprécié et remplacé par une structure XML plus flexible et extensible.

## Structure PML

Un fichier PML peut contenir jusqu'à trois sections principales au même niveau:

### 1. **HistoriqueClassements**
Enregistrements historiques des escouades et classements.
```xml
<HistoriqueEscouadePML>
  <HistoriqueClassements>
    <Enregistrement ID="1">
      <informations>
        <Date>2025-12-13T00:00:00Z</Date>
        <Ligue>4</Ligue>
        <Score>12345</Score>
        <Puissance>26980</Puissance>
      </informations>
      <Classement>
        <Nutaku>3268</Nutaku>
        <Top150>5227</Top150>
        <Pays>217</Pays>
      </Classement>
      <Commandant>...</Commandant>
      <Escouade>...</Escouade>
      <Androides>...</Androides>
      <Lucie>
        <Puissance>500</Puissance>
      </Lucie>
    </Enregistrement>
  </HistoriqueClassements>
</HistoriqueEscouadePML>
```

### 2. **inventaire**
Base de données des personnages disponibles.
```xml
<inventaire>
  <Personnage>
    <Nom>REGINA</Nom>
    <Rarete>SSR</Rarete>
    <Type>Mercenaire</Type>
    <Puissance>3320</Puissance>
    <PA>140</PA>
    <PV>509</PV>
    <Niveau>14</Niveau>
    <Rang>2</Rang>
    <Role>Sentinelle</Role>
    <Faction>Syndicat</Faction>
    <Selectionne>true</Selectionne>
    <Description>Personnage de rare SSR</Description>
  </Personnage>
</inventaire>
```

### 3. **templates**
Modèles prédéfinis d'équipes.
```xml
<templates>
  <template>
    <Nom>Escouade Principale</Nom>
    <Description>Mon équipe de tournoi habituelle</Description>
    <Personnage>
      <Nom>BELLE</Nom>
      <Rarete>SSR</Rarete>
      <Puissance>3090</Puissance>
      <Niveau>8</Niveau>
    </Personnage>
  </template>
</templates>
```

## Services

### PmlImportService
Service responsable de l'import/export au format PML.

**Méthodes principales:**
- `ImportPmlAsync(Stream stream, string fileName)` - Importe un fichier PML
- `ExporterInventairePmlAsync(IEnumerable<Personnage> personnages)` - Exporte l'inventaire au format PML
- `ExporterTemplatesPmlAsync(IEnumerable<Template> templates)` - Exporte les templates au format PML

### HistoriqueEscouadeService
Mise à jour pour supporter l'export PML complet incluant inventaire et templates.

**Méthode mise à jour:**
- `ExporterHistoriqueXmlAsync()` - Exporte maintenant au format PML avec toutes les sections

## Pages UI

### /import-pml
Nouvelle page dédiée à l'import de fichiers PML pour l'inventaire et les templates.

**Fichiers:**
- `ImportPml.razor` - Interface utilisateur
- `ImportPml.razor.cs` - Logique métier

### Pages mises à jour

#### Historique.razor
- Support des fichiers `.pml` en addition aux `.xml`
- Import/Export au format PML

#### Inventaire.razor
- Injection de `PmlImportService` au lieu de `CsvImportService`
- Méthode `ExportInventairePml()` pour exporter en PML
- Méthode `ExportTemplateAsPml()` pour exporter les templates en PML

#### Templates.razor
- Injection de `PmlImportService` au lieu de `CsvImportService`
- Méthode `ExportTemplate()` mise à jour pour exporter en PML

## Migration depuis CSV

Si vous aviez des fichiers CSV, vous devez les convertir au format PML:

**Avant (CSV):**
```csv
Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction
REGINA;SSR;Mercenaire;3320;140;509;Mêlée;Sentinelle;14;2;Oui;Syndicat
```

**Après (PML/XML):**
```xml
<inventaire>
  <Personnage>
    <Nom>REGINA</Nom>
    <Rarete>SSR</Rarete>
    <Type>Mercenaire</Type>
    <Puissance>3320</Puissance>
    <PA>140</PA>
    <PV>509</PV>
    <Niveau>14</Niveau>
    <Rang>2</Rang>
    <Role>Sentinelle</Role>
    <Faction>Syndicat</Faction>
    <Selectionne>Oui</Selectionne>
    <Description>Description du personnage</Description>
  </Personnage>
</inventaire>
```

## Extensions de fichiers

- `.pml` - Format recommandé pour les fichiers d'import/export (XML structuré)
- `.xml` - Toujours supporté pour compatibilité rétro-active avec l'historique

## Exemples

Un fichier d'exemple complet est disponible: `exemple_export_pml.pml`

Cet exemple démontre:
- La structure d'historique complet
- Les enregistrements d'inventaire
- Les modèles de templates

## Détails techniques

### Valeurs énumérées

**Rarete:**
- `SSR` - Super Rare
- `SR` - Rare
- `R` - Uncommon
- `N` - Normal

**Type:**
- `Mercenaire` - Personnage mercenaire
- `Androïde` - Androïde
- `Commandant` - Personnage commandant

**Role:**
- `Sentinelle`
- `Guerrière` / `Guerriere`
- `Tireuse`
- `Magicienne`
- `Soigneuse`

**Faction:**
- `Syndicat`
- `Ordre`
- `Androïde`

## Validation

L'import PML effectue des validations:
- Vérification de l'existence des personnages
- Validation des types de données
- Vérification de la cohérence des rôles et factions
- Gestion des valeurs manquantes

Les erreurs d'import détaillées sont reportées à l'utilisateur via le résultat d'import.

## Service d'injection des dépendances

Le `PmlImportService` est enregistré dans `Program.cs`:

```csharp
builder.Services.AddScoped<PmlImportService>();
```

Il est disponible pour injection dans les composants Razor et autres services.
