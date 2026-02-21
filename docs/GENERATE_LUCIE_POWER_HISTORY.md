# Génération de l'historique de puissance Lucie

Ce document explique comment générer rétroactivement l'historique de puissance de Lucie à partir des données existantes (classements et modifications de pièces).

## Contexte

Depuis la mise à jour récente, l'application enregistre automatiquement l'évolution de la puissance de Lucie dans l'historique des modifications. Cela permet aux graphiques de statistiques d'afficher correctement l'évolution sans décrochages.

Pour les données existantes (avant cette mise à jour), il est nécessaire de générer rétroactivement ces enregistrements d'historique.

## Méthode 1 : Via le script PowerShell (Recommandé)

### Prérequis
- PowerShell
- .NET 9.0 SDK
- `dotnet-script` (sera installé automatiquement si nécessaire)

### Utilisation

```powershell
cd D:\Devs\CharacterManager\scripts
.\Generate-LuciePowerHistory.ps1
```

Le script va :
1. Vérifier que `dotnet-script` est installé (et l'installer si nécessaire)
2. Parcourir tous les classements existants et créer des enregistrements d'historique pour la puissance Lucie
3. Parcourir toutes les modifications de pièces et créer des enregistrements d'historique pour les jours où il y a eu des modifications
4. Afficher un résumé du nombre d'enregistrements créés

### Sortie attendue

```
=== Génération de l'historique de puissance Lucie ===

Vérification de dotnet-script...
✓ dotnet-script disponible

Exécution du script de génération...
Dossier de travail: D:\Devs\CharacterManager\CharacterManager

Base de données: D:\Devs\CharacterManager\CharacterManager\charactermanager.db

Début de la génération...

[LucieHistory] Début de la génération de l'historique de puissance Lucie
[LucieHistory] Trouvé 45 classements
[LucieHistory] ✓ 2025-12-01: Puissance 2825
[LucieHistory] ✓ 2025-12-15: Puissance 3215
...
[LucieHistory] 28 classements traités
[LucieHistory] Trouvé 67 modifications de pièces
[LucieHistory] Sur 12 jours distincts
[LucieHistory] ✓ 2025-11-20: Sélection=2500, Max=2800
...
[LucieHistory] 8 jours traités
[LucieHistory] Enregistrements PuissanceLucieSelectionnee: 36
[LucieHistory] Enregistrements PuissanceLucieMax: 36
[LucieHistory] Génération terminée avec succès

=== Résumé ===
✓ Classements traités: 28
✓ Jours de modifications traités: 8

✓ Génération terminée avec succès!

=== ✓ Génération terminée ===
```

## Méthode 2 : Via le code C#

Vous pouvez également appeler la méthode directement depuis le code :

```csharp
// Injection des services nécessaires
var dbInitService = serviceProvider.GetRequiredService<IDatabaseInitializationService>();
var historiqueService = serviceProvider.GetRequiredService<IHistoriqueModificationService>();
var personnageService = serviceProvider.GetRequiredService<IPersonnageService>();

// Génération de l'historique
var (classementsTraites, joursTraites) = await dbInitService.GenerateLuciePowerHistoryAsync(
    historiqueService, 
    personnageService);

Console.WriteLine($"Classements traités: {classementsTraites}");
Console.WriteLine($"Jours traités: {joursTraites}");
```

## Fonctionnement détaillé

### 1. Traitement des classements

Pour chaque classement dans `HistoriquesClassement` :
- Si un enregistrement d'historique de puissance Lucie n'existe pas déjà pour cette date
- Et si la puissance Lucie du classement est > 0
- Crée deux enregistrements :
  - `PuissanceLucieSelectionnee` (EntiteId=-1) avec la valeur du classement
  - `PuissanceLucieMax` (EntiteId=-2) avec la valeur du classement (on suppose que la puissance max était au moins égale à la sélection)

### 2. Traitement des modifications de pièces

Pour chaque jour où il y a eu des modifications de pièces :
- Vérifie si un enregistrement existe déjà
- Si non, crée deux enregistrements avec les valeurs actuelles de puissance :
  - `PuissanceLucieSelectionnee` avec `GetPuissanceLucieEscouade()`
  - `PuissanceLucieMax` avec `GetPuissanceMaxLucieEscouade()`

**Note** : Pour les jours de modifications de pièces, on utilise les valeurs actuelles comme référence. C'est une approximation car on ne peut pas reconstituer les valeurs historiques exactes.

### 3. Évitement des doublons

La méthode vérifie toujours si un enregistrement existe déjà avant d'en créer un nouveau. Vous pouvez donc exécuter le script plusieurs fois sans créer de doublons.

## Fichiers créés

- [scripts/GenerateLuciePowerHistory.csx](../scripts/GenerateLuciePowerHistory.csx) : Script C# principal
- [scripts/Generate-LuciePowerHistory.ps1](../scripts/Generate-LuciePowerHistory.ps1) : Wrapper PowerShell
- [DatabaseInitializationService.cs](../CharacterManager/Server/Services/DatabaseInitializationService.cs) : Méthode `GenerateLuciePowerHistoryAsync`

## Dépannage

### "dotnet-script" n'est pas reconnu

Si le script ne trouve pas `dotnet-script` après installation :
```powershell
dotnet tool install -g dotnet-script
```

Puis redémarrez votre terminal.

### Base de données verrouillée

Si la base de données est verrouillée, fermez l'application CharacterManager avant d'exécuter le script.

### Permissions insuffisantes

Exécutez PowerShell en tant qu'administrateur si vous rencontrez des problèmes de permissions.

## Après la génération

Après avoir généré l'historique :
1. Redémarrez l'application CharacterManager
2. Allez dans la page Statistiques
3. Les graphiques d'évolution de puissance devraient maintenant afficher correctement l'évolution de la puissance de Lucie sans décrochages

## Impact sur les statistiques

Les graphiques affectés :
- **Évolution de la puissance de l'équipe sélectionnée** : Inclut maintenant la puissance de Lucie sélectionnée
- **Évolution de la puissance de la meilleure équipe** : Inclut maintenant la puissance max de Lucie

Cela permet d'avoir une vue complète de l'évolution de la puissance totale, incluant les pièces de la Maison de Lucie.
