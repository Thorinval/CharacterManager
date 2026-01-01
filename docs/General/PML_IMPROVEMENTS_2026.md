# Améliorations du format PML - Janvier 2026

## Résumé des modifications

Ce document décrit les améliorations apportées au système d'import/export PML pour une gestion complète des données de l'application CharacterManager.

**Note importante** : `HistoriqueEscouade` (ancien format JSON sérialisé) est désormais obsolète et remplacé par `HistoriqueClassement` (format relationnel structuré). Les anciennes références ont été nettoyées du code, mais le modèle est conservé pour compatibilité avec les données existantes.

## 1. Ajout de l'affection de Lucie House

### Problème

L'affection de Lucie House n'était pas exportée ni importée dans le format PML.

### Solution

- **Export** : Ajout du champ `<Affection>` dans la section `<LucieHouse>` lors de l'export
- **Import** : Lecture et import du champ `<Affection>` depuis la section `<LucieHouse>`

### Structure XML

```xml
<LucieHouse>
  <Affection>50</Affection>
  <Piece>
    <Nom>Cuisine</Nom>
    <Niveau>2</Niveau>
    ...
  </Piece>
  ...
</LucieHouse>
```

## 2. Ajout des historiques de ligue

### Problème

Les historiques de passage dans les ligues n'étaient pas gérés dans l'import/export PML.

### Solution

Ajout d'une nouvelle section `<HistoriqueLigue>` pour gérer les passages de ligue.

### Structure XML

```xml
<HistoriqueLigue>
  <DatePassage>2026-01-01</DatePassage>
  <Ligue>25</Ligue>
  <Notes>Passage en ligue Elite</Notes>
</HistoriqueLigue>
```

### Caractéristiques

- Export des 100 derniers historiques de ligue
- Import avec validation :
  - Date requise (format DateOnly)
  - Ligue requise (entre 1 et 50)
  - Notes optionnelles

## 3. Amélioration des historiques de classement

### Problème

Les historiques de classement n'étaient pas exportés/importés dans le format PML. Seuls les historiques d'escouade (JSON) étaient disponibles.

### Solution

Ajout d'une nouvelle section structurée `<HistoriqueClassement>` avec toutes les données détaillées.

### Structure XML

```xml
<HistoriqueClassement>
  <DateEnregistrement>2026-01-01</DateEnregistrement>
  <Ligue>25</Ligue>
  <Score>12500</Score>
  <PuissanceTotal>85000</PuissanceTotal>
  <PuissanceCommandant>15000</PuissanceCommandant>
  <PuissanceMercenaires>60000</PuissanceMercenaires>
  <PuissanceLucie>10000</PuissanceLucie>
  
  <!-- Classements détaillés -->
  <Classements>
    <ClassementItem>
      <Nom>Nutaku</Nom>
      <TypeClassement>Nutaku</TypeClassement>
      <Valeur>150</Valeur>
    </ClassementItem>
    <ClassementItem>
      <Nom>Top 150</Nom>
      <TypeClassement>Top150</TypeClassement>
      <Valeur>85</Valeur>
    </ClassementItem>
    <ClassementItem>
      <Nom>France</Nom>
      <TypeClassement>France</TypeClassement>
      <Valeur>25</Valeur>
    </ClassementItem>
  </Classements>
  
  <!-- Mercenaires historiques -->
  <Mercenaires>
    <Personnage>
      <Nom>Alice</Nom>
      <Rarete>SSR</Rarete>
      <Type>Mercenaire</Type>
      <Puissance>8500</Puissance>
      <PA>150</PA>
      <PV>2000</PV>
      <Niveau>80</Niveau>
      <Rang>6</Rang>
      <Role>Sentinelle</Role>
      <Faction>Syndicat</Faction>
      <TypeAttaque>Mêlée</TypeAttaque>
      <Selectionne>True</Selectionne>
      <Description></Description>
    </Personnage>
    <!-- ... autres mercenaires ... -->
  </Mercenaires>
  
  <!-- Commandant historique -->
  <Commandant>
    <Nom>Catherine</Nom>
    <Rarete>SSR</Rarete>
    <Type>Commandant</Type>
    <Puissance>15000</Puissance>
    <!-- ... autres propriétés ... -->
  </Commandant>
  
  <!-- Androïdes historiques -->
  <Androides>
    <Personnage>
      <Nom>X-01</Nom>
      <Rarete>SR</Rarete>
      <Type>Androïde</Type>
      <Puissance>5000</Puissance>
      <!-- ... autres propriétés ... -->
    </Personnage>
    <!-- ... autres androïdes ... -->
  </Androides>
  
  <!-- Pièces Lucie House historiques -->
  <Pieces>
    <Piece>
      <Nom>Cuisine</Nom>
      <Niveau>2</Niveau>
      <PuissanceTactique>50</PuissanceTactique>
      <PuissanceStrategique>30</PuissanceStrategique>
      <Selectionne>True</Selectionne>
    </Piece>
    <!-- ... autres pièces ... -->
  </Pieces>
</HistoriqueClassement>
```

### Caractéristiques
- Export des 50 derniers historiques de classement avec toutes leurs données
- Import avec reconstruction complète de l'historique :
  - Personnages historiques (mercenaires, commandant, androïdes)
  - Pièces Lucie House historiques
  - Classements détaillés (Nutaku, Top 150, France)
  - Puissances détaillées par catégorie

## 4. Constantes ajoutées

### Dans `AppConstants.XmlElements`

```csharp
// Affection de Lucie House
public const string Affection = "Affection";

// Éléments d'historique de ligue
public const string HistoriqueLigue = "HistoriqueLigue";
public const string DatePassage = "DatePassage";
public const string Ligue = "Ligue";
public const string Notes = "Notes";

// Éléments d'historique de classement
public const string HistoriqueClassement = "HistoriqueClassement";
public const string Score = "Score";
public const string PuissanceCommandant = "PuissanceCommandant";
public const string PuissanceMercenaires = "PuissanceMercenaires";
public const string PuissanceLucie = "PuissanceLucie";
public const string Classements = "Classements";
public const string ClassementItem = "ClassementItem";
public const string TypeClassement = "TypeClassement";
public const string Valeur = "Valeur";
public const string Mercenaires = "Mercenaires";
public const string Androides = "Androides";
public const string Pieces = "Pieces";
```

## 5. Méthodes ajoutées dans PmlImportService

### Méthodes d'import
- `ImportHistoriquesLigueAsync()` : Import des historiques de ligue
- `ImportHistoriquesClassementAsync()` : Import des historiques de classement structurés

### Améliorations des méthodes existantes
- `ImportLucieHouseAsync()` : Ajout de l'import de l'affection
- `ExporterInventairePmlAsync()` : Ajout de l'export de l'affection
- `ExportPmlAsync()` : 
  - Ajout de l'export de l'affection dans LucieHouse
  - Ajout de l'export des historiques de ligue
  - Ajout de l'export des historiques de classement structurés

## 6. Compatibilité

### Rétrocompatibilité
- Les anciens fichiers PML sans les nouvelles sections restent importables
- Les champs optionnels (Notes, Affection) sont gérés avec des valeurs par défaut si absents

### Migration
Aucune migration de base de données n'est nécessaire. Les structures existantes sont utilisées :
- Table `LucieHouses` : utilise le champ `Affection` existant
- Table `HistoriquesLigue` : utilise la structure existante
- Table `HistoriquesClassement` : utilise les relations existantes avec les entités historiques

## 7. Utilisation

### Export
L'export PML inclut automatiquement toutes les nouvelles données lorsque les options correspondantes sont activées :
- **Inventaire** : inclut l'affection de Lucie House
- **Historiques** : inclut les historiques d'escouade, de ligue ET de classement

### Import
L'import PML détecte et importe automatiquement toutes les sections présentes dans le fichier :
- Section `<LucieHouse>` avec `<Affection>`
- Section `<HistoriqueLigue>`
- Section `<HistoriqueClassement>`

## 8. Limites d'export

Pour éviter des fichiers trop volumineux :
- **Historiques d'escouade** : 50 derniers
- **Historiques de ligue** : 100 derniers
- **Historiques de classement** : 50 derniers

## 9. Validation

### Historiques de ligue
- Date requise et au format valide (DateOnly)
- Ligue entre 1 et 50
- Notes optionnelles (texte libre)

### Historiques de classement
- Date requise et au format valide (DateOnly)
- Tous les champs numériques validés avant import
- Les entités historiques (personnages, pièces) sont recréées si les données sont valides

## 10. Tests recommandés

Pour valider les modifications :

1. **Test d'export**
   - Créer des données de test (affection Lucie, historiques de ligue et classement)
   - Exporter au format PML
   - Vérifier la présence de toutes les nouvelles sections dans le XML

2. **Test d'import**
   - Créer un fichier PML avec les nouvelles sections
   - Importer le fichier
   - Vérifier que toutes les données sont correctement importées en base

3. **Test de compatibilité**
   - Importer un ancien fichier PML sans les nouvelles sections
   - Vérifier qu'il est toujours fonctionnel

## Conclusion

Ces améliorations permettent maintenant une sauvegarde et restauration complète de l'état de l'application, incluant :
- L'affection de Lucie House
- L'historique complet des passages de ligue
- L'historique détaillé des classements avec toutes les compositions d'escouade

Le format PML est maintenant le format d'export/import principal et complet de CharacterManager.
