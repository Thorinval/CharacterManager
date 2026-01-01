# Dépréciation de HistoriqueEscouade - Janvier 2026

## Contexte

L'ancien modèle `HistoriqueEscouade` utilisait un format JSON sérialisé (`DonneesEscouadeJson`) pour stocker les informations d'historique de l'escouade. Ce format présentait plusieurs limitations :

- Difficulté à requêter les données individuelles (personnages, classements)
- Pas de relations structurées dans la base de données
- Pas de normalisation des données
- Complexité accrue pour les exports/imports

## Remplacement par HistoriqueClassement

Le nouveau modèle `HistoriqueClassement` offre une structure relationnelle complète :

### Avantages de HistoriqueClassement

1. **Structure relationnelle** : Relations propres avec les entités historiques
   - `PersonnageHistorique` pour mercenaires, commandant, androïdes
   - `PieceHistorique` pour les pièces Lucie House
   - `Classement` pour les différents types de classements

2. **Requêtes facilitées** : Accès direct aux données via LINQ
   - Filtrage par personnage, date, ligue
   - Agrégations sur les puissances
   - Recherche par classement

3. **Export/Import structuré** : Format PML avec XML structuré complet
   - Sections dédiées par type d'entité
   - Validation des données à l'import
   - Pas de désérialisation JSON nécessaire

4. **Extensibilité** : Facile d'ajouter de nouveaux champs
   - Ajout de nouveaux types de classements
   - Nouvelles statistiques
   - Métadonnées additionnelles

## Modifications effectuées

### 1. Modèle HistoriqueEscouade

**Fichier** : `CharacterManager/Server/Models/HistoriqueEscouade.cs`

- ✅ Marqué comme `[Obsolete]` avec message explicatif
- ✅ Documentation mise à jour pour indiquer le remplacement
- ⚠️ **Conservé** pour compatibilité avec les données existantes

### 2. Service HistoriqueClassementService

**Fichier** : `CharacterManager/Server/Services/HistoriqueClassementService.cs`

- ✅ Méthode `EnregistrerEscouadeAsync()` marquée comme `[Obsolete]`
- ✅ Méthode `ImporterHistoriqueAsync()` marquée comme `[Obsolete]`
- ℹ️ Ces méthodes sont conservées uniquement pour l'import XML legacy

### 3. PmlImportService

**Fichier** : `CharacterManager/Server/Services/PmlImportService.cs`

- ❌ **Supprimé** : Import des `HistoriqueEscouade` via format PML
- ❌ **Supprimé** : Export des `HistoriqueEscouade` via format PML
- ❌ **Supprimé** : Méthode `ImportHistoriquesAsync()`
- ✅ Seuls les imports/exports de `HistoriqueClassement` sont supportés

### 4. Constantes

**Fichier** : `CharacterManager/Server/Constants/AppConstants.cs`

- ❌ **Supprimé** : Constante `HistoriqueEscouade`
- ℹ️ Commentaire ajouté pour indiquer la dépréciation

### 5. Tests

**Fichier** : `CharacterManager.Tests/PmlImportServiceTests.cs`

- ❌ **Désactivé** : Test `ImportPmlAsync_WithHistories_ShouldPersistEntries()`
- ℹ️ Commentaire expliquant que le test concerne un format obsolète

## Ce qui est conservé

### Données existantes

- ✅ Table `HistoriquesEscouade` conservée en base de données
- ✅ Les données historiques existantes restent accessibles
- ✅ Migration de données **non requise**

### Compatibilité import XML

- ✅ Import XML legacy via `HistoriqueClassementService.ImporterHistoriqueAsync()`
- ✅ Support de l'ancien format XML avec `<Enregistrement>` et données JSON
- ⚠️ Cette méthode est marquée obsolète mais fonctionnelle

### Structure du DbContext

- ✅ `DbSet<HistoriqueEscouade> HistoriquesEscouade` conservé
- ✅ Migrations existantes non modifiées

## Migration recommandée (optionnelle)

Si vous souhaitez migrer les anciennes données `HistoriqueEscouade` vers `HistoriqueClassement`, voici la procédure :

### Script de migration (exemple)

```csharp
public async Task MigrerHistoriquesEscouadeAsync()
{
    var historiquesEscouade = await dbContext.HistoriquesEscouade
        .AsNoTracking()
        .ToListAsync();

    foreach (var historiqueEscouade in historiquesEscouade)
    {
        try
        {
            var donnees = JsonSerializer.Deserialize<DonneesEscouadeSerialisees>(
                historiqueEscouade.DonneesEscouadeJson
            );

            if (donnees == null) continue;

            var historiqueClassement = new HistoriqueClassement
            {
                DateEnregistrement = DateOnly.FromDateTime(historiqueEscouade.DateEnregistrement),
                Ligue = donnees.Ligue,
                Score = donnees.Score,
                PuissanceTotal = historiqueEscouade.PuissanceTotal,
                // ... mapper les autres champs
            };

            // Mapper les mercenaires
            foreach (var merc in donnees.Mercenaires)
            {
                historiqueClassement.Mercenaires.Add(new PersonnageHistorique
                {
                    Nom = merc.Nom,
                    Niveau = merc.Niveau,
                    Rang = merc.Rang,
                    Puissance = merc.Puissance,
                    // ...
                });
            }

            // Mapper le commandant
            if (donnees.Commandant != null)
            {
                historiqueClassement.Commandant = new PersonnageHistorique
                {
                    Nom = donnees.Commandant.Nom,
                    // ...
                };
            }

            // Ajouter les classements
            historiqueClassement.Classements.Add(new Classement
            {
                Nom = "Nutaku",
                Type = TypeClassement.Nutaku,
                Valeur = donnees.Nutaku
            });
            // ...

            dbContext.HistoriquesClassement.Add(historiqueClassement);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur migration historique {historiqueEscouade.Id}: {ex.Message}");
        }
    }

    await dbContext.SaveChangesAsync();
}
```

### Vérification post-migration

```csharp
var countEscouade = await dbContext.HistoriquesEscouade.CountAsync();
var countClassement = await dbContext.HistoriquesClassement.CountAsync();

Console.WriteLine($"HistoriquesEscouade: {countEscouade}");
Console.WriteLine($"HistoriquesClassement: {countClassement}");
```

### Suppression des anciennes données (après vérification)

⚠️ **Attention** : Ne supprimer les données qu'après vérification complète de la migration !

```csharp
// Supprimer les anciennes données
dbContext.HistoriquesEscouade.RemoveRange(dbContext.HistoriquesEscouade);
await dbContext.SaveChangesAsync();
```

## Impact sur l'application

### ✅ Pas d'impact fonctionnel

- L'application fonctionne normalement
- Les pages d'historique utilisent déjà `HistoriqueClassement`
- Les exports PML incluent les historiques structurés

### ⚠️ Avertissements de compilation

Les méthodes et classes obsolètes généreront des avertissements de compilation avec l'attribut `[Obsolete]`. Ces avertissements sont intentionnels et servent de rappel pour ne pas utiliser ces éléments dans du nouveau code.

### 🔄 Compatibilité ascendante

- Les anciennes données restent accessibles en lecture
- Les anciens fichiers XML peuvent toujours être importés
- Aucune perte de données

## Format PML actuel (2026)

Le format PML actuel exporte/importe uniquement `HistoriqueClassement` :

```xml
<HistoriqueClassement>
  <DateEnregistrement>2026-01-01</DateEnregistrement>
  <Ligue>25</Ligue>
  <Score>12500</Score>
  <PuissanceTotal>85000</PuissanceTotal>
  <PuissanceCommandant>15000</PuissanceCommandant>
  <PuissanceMercenaires>60000</PuissanceMercenaires>
  <PuissanceLucie>10000</PuissanceLucie>
  
  <Classements>
    <ClassementItem>
      <Nom>Nutaku</Nom>
      <TypeClassement>Nutaku</TypeClassement>
      <Valeur>150</Valeur>
    </ClassementItem>
    <!-- ... autres classements ... -->
  </Classements>
  
  <Mercenaires>
    <Personnage>
      <Nom>Alice</Nom>
      <!-- ... propriétés complètes ... -->
    </Personnage>
    <!-- ... autres mercenaires ... -->
  </Mercenaires>
  
  <!-- ... Commandant, Androides, Pieces ... -->
</HistoriqueClassement>
```

## Résumé

| Élément | Statut | Action |
|---------|--------|--------|
| Modèle `HistoriqueEscouade` | ⚠️ Obsolète | Conservé pour compatibilité |
| Table `HistoriquesEscouade` | ✅ Active | Conservée avec données existantes |
| Import/Export PML HistoriqueEscouade | ❌ Supprimé | Utiliser HistoriqueClassement |
| Import XML legacy | ⚠️ Obsolète | Conservé pour compatibilité |
| Modèle `HistoriqueClassement` | ✅ Actuel | Utiliser pour nouveaux enregistrements |
| Format PML HistoriqueClassement | ✅ Actuel | Format standard pour export/import |

## Conclusion

La dépréciation de `HistoriqueEscouade` fait partie d'une évolution vers une architecture plus structurée et maintenable. Le nouveau modèle `HistoriqueClassement` offre de meilleures performances, une meilleure maintenabilité et une extensibilité accrue.

Les anciennes données et fonctionnalités sont conservées pour assurer une transition en douceur sans perte de données ni rupture de compatibilité.
