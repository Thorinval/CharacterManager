# Système de Localisation (i18n)

## Vue d'ensemble

L'application dispose d'un système de localisation complet permettant de supporter plusieurs langues. Actuellement, deux langues sont disponibles :

- **Français (fr)** - Langue par défaut
- **Anglais (en)**

## Structure

### Fichiers de ressources

Les ressources de traduction se trouvent dans `wwwroot/i18n/`:

- `fr.json` - Traductions en français
- `en.json` - Traductions en anglais

Chaque fichier JSON est structuré par sections (sections.key):

```json
{
  "common": {
    "yes": "Oui",
    "no": "Non"
  },
  "navigation": {
    "home": "Accueil"
  }
}

```

### Services

#### `LocalizationService`

Service backend singleton pour gérer les ressources de traduction.

#### `ClientLocalizationService`

Service scoped pour les composants Blazor permettant d'accéder aux traductions.

Méthodes disponibles:

- `InitializeAsync(languageCode)` - Initialise le service avec une langue
- `T(key)` - Récupère une traduction par sa clé (notation pointée)
- `SetLanguageAsync(languageCode)` - Change de langue
- `GetCurrentLanguage()` - Retourne la langue courante
- `GetResources()` - Retourne les ressources complètes

## Utilisation

### 1. Ajouter une traduction

Modifier les fichiers JSON dans `wwwroot/i18n/`:

**fr.json:**

```json
{
  "mySection": {
    "myKey": "Texte en français"
  }
}
```

**en.json:**

```json
{
  "mySection": {
    "myKey": "Text in English"
  }
}
```

### 2. Utiliser dans un composant Razor

Injecter `ClientLocalizationService` et utiliser la méthode `T()`:

```razor
@using CharacterManager.Server.Services
@inject ClientLocalizationService LocalizationService

<h1>@LocalizationService.T("mySection.myKey")</h1>

@code {
    protected override async Task OnInitializedAsync()
    {
        // Le service est automatiquement initialisé via LocalizationProvider
        // à partir de la langue stockée dans AppSettings
    }
}
```

### 3. Utiliser le composant réutilisable

Pour plus de simplicité, utiliser le composant `LocalizedText`:

```razor
<LocalizedText Key="mySection.myKey" />
```

## Stockage de la langue

La langue préférée est stockée dans la table `AppSettings.Language`:

- Valeur par défaut: "fr"
- Modifiable via la page Settings

## Migration de la base de données

La colonne `Language` a été ajoutée au modèle `AppSettings`. La valeur par défaut est "fr".

Si vous avez une base de données existante, la valeur sera automatiquement définie à "fr" pour tous les enregistrements existants.

## Ajout d'une nouvelle langue

Pour ajouter une nouvelle langue (ex: Espagnol):

1. **Créer le fichier de ressources** `wwwroot/i18n/es.json`
2. **Ajouter la langue à `LocalizationService.GetAvailableLanguages()`**:

   ```csharp
   public List<LanguageOption> GetAvailableLanguages()
   {
       return new List<LanguageOption>
       {
           new LanguageOption { Code = "fr", Name = "Français" },
           new LanguageOption { Code = "en", Name = "English" },
           new LanguageOption { Code = "es", Name = "Español" }  // Nouveau
       };
   }
   ```

## Bonnes pratiques

1. **Utiliser une notation pointée cohérente**: `section.subsection.key`
2. **Traduire complètement**: Assurer que chaque clé existe dans toutes les langues
3. **Éviter les chaînes codées en dur**: Utiliser le service de localisation pour tout texte visible
4. **Séparer par domaine**: Grouper les clés par page/fonctionnalité

## Exemple complet

**fr.json:**

```json
{
  "inventory": {
    "title": "Inventaire",
    "add": "Ajouter personnage",
    "delete": "Supprimer"
  }
}
```

**en.json:**

```json
{
  "inventory": {
    "title": "Inventory",
    "add": "Add Character",
    "delete": "Delete"
  }
}
```

**Composant Razor:**

```razor
@using CharacterManager.Server.Services
@inject ClientLocalizationService Loc

<div class="header">
    <h1>@Loc.T("inventory.title")</h1>
    <button>@Loc.T("inventory.add")</button>
</div>

@code {
    protected override async Task OnInitializedAsync()
    {
        // Service déjà initialisé
    }
}
```

## Débogage

Pour vérifier les traductions chargées:

```csharp
var resources = LocalizationService.GetResources();
```

Pour recharger les ressources:

```csharp
LocalizationService.ClearCache();
await LocalizationService.InitializeAsync("fr");
```
