# Test Constants

Ce répertoire contient les constantes dédiées aux tests unitaires du projet CharacterManager.

## Fichiers

### TestDataConstants.cs

Contient toutes les données de test réutilisables pour les tests unitaires, organisées par catégories :

- **PersonnageNames** : Noms de personnages utilisés dans les tests (REGINA, ISABELLA, ALPHA, etc.)
- **PersonnageDescriptions** : Descriptions des personnages
- **PersonnageRoles** : Rôles de personnages (Guerrière, Sentinelle, etc.)
- **TemplateNames** : Noms et descriptions des templates/équipes de test
- **LucieHousePieceNames** : Noms des pièces de la Maison de Lucie
- **LucieHouseAspects** : Types de bonus pour les pièces (Dégâts, PV, Crit)
- **NumericValues** : Valeurs numériques utilisées dans les tests (puissance, PA, PV, niveaux, rangs)
- **FileNames** : Noms de fichiers de test
- **ExpectedErrorMessages** : Messages d'erreur attendus dans les assertions
- **TestDates** : Dates ISO utilisées dans les tests

## Utilisation

Dans vos fichiers de test, importez les constantes de test :

```csharp
using CharacterManager.Tests.Constants;

// Utilisez les constantes au lieu de littéraux
var name = TestDataConstants.PersonnageNames.Regina;
var niveau = TestDataConstants.NumericValues.NiveauLevel5;
var template = TestDataConstants.TemplateNames.MonEquipe;
```

## Avantages

1. **Maintenabilité** : Modifier une valeur de test en un seul endroit
2. **Cohérence** : Mêmes valeurs utilisées dans tous les tests
3. **Lisibilité** : Code de test plus clair et autodocumenté
4. **Prévention des bugs** : Moins d'erreurs de saisie de chaînes
5. **Facilité de refactoring** : Renommer des entités de test est facile

## Convention de nommage

- Les constantes sont groupées par domaine métier
- Les noms sont explicites et en PascalCase
- Les valeurs numériques incluent une indication de leur valeur (ex: `NiveauLevel5`)
- Les descriptions sont placées dans des commentaires XML
