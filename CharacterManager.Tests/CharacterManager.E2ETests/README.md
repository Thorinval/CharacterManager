# Tests E2E CharacterManager

Ce projet contient les tests end-to-end (E2E) pour l'application CharacterManager utilisant Selenium WebDriver.

## Dépendances

- Selenium.WebDriver 4.39.0
- Selenium.WebDriver.ChromeDriver 143.0.7499.4200
- WebDriverManager 2.17.6
- xUnit

## Configuration requise

- Chrome/Chromium navigateur installé
- Application CharacterManager en cours d'exécution sur `http://localhost:5269`

## Tests disponibles

### HomePageTests
- `HomePage_ShouldLoadSuccessfully` - Vérifie que la page d'accueil se charge
- `HomePage_ShouldDisplayWelcomeContent` - Vérifie que le contenu de bienvenue s'affiche
- `HomePage_ShouldHaveNavigation` - Vérifie que la navigation existe
- `HomePage_ShouldDisplayVersionInfo` - Vérifie que l'information de version s'affiche

### InventairePageTests
- `InventairePage_ShouldLoadSuccessfully` - Vérifie que la page inventaire se charge
- `InventairePage_ShouldDisplayTable` - Vérifie que le tableau s'affiche
- `InventairePage_ShouldHaveAddButton` - Vérifie que le bouton "Ajouter" existe
- `InventairePage_AddButtonShouldOpenModal` - Vérifie que le modal s'ouvre au clic sur "Ajouter"
- `InventairePage_ModalShouldHaveFormFields` - Vérifie que le modal contient des champs de formulaire
- `InventairePage_ShouldDisplayTableColumns` - Vérifie que les colonnes du tableau s'affichent
- `InventairePage_ShouldNavigateToDetailPage` - Vérifie que cliquer sur une ligne navigue vers les détails

### NavigationTests
- `Navigation_ShouldHaveHomeLink` - Vérifie que le lien d'accueil existe
- `Navigation_ShouldHaveInventaireLink` - Vérifie que le lien inventaire existe
- `Navigation_HomeLink_ShouldNavigateToHome` - Vérifie que le lien d'accueil fonctionne
- `Navigation_InventaireLink_ShouldNavigateToInventaire` - Vérifie que le lien inventaire fonctionne
- `Navigation_ShouldStayOnSamePage_WhenClicking` - Vérifie la stabilité de la navigation

## Exécution

### Démarrer l'application
```bash
cd CharacterManager
dotnet run
```

L'application devrait être accessible sur `http://localhost:5269`

### Exécuter tous les tests E2E
```bash
cd CharacterManager.E2ETests
dotnet test
```

### Exécuter un test spécifique
```bash
dotnet test --filter "InventairePage_ShouldLoadSuccessfully"
```

### Exécuter avec affichage détaillé
```bash
dotnet test -v detailed
```

## Structure

- **BaseE2ETest.cs** - Classe de base contenant la configuration du WebDriver et les méthodes utilitaires
- **HomePageTests.cs** - Tests pour la page d'accueil
- **InventairePageTests.cs** - Tests pour la page inventaire
- **NavigationTests.cs** - Tests pour la navigation

## Notes importantes

- Les tests utilisent Chrome/Chromium navigateur
- WebDriverManager gère automatiquement le téléchargement du ChromeDriver
- Chaque test est indépendant et dispose d'une instance WebDriver propre
- Les délais (Thread.Sleep) sont utilisés pour permettre au DOM de se charger complètement
- Les tests utilisent des sélecteurs CSS et XPath robustes pour localiser les éléments

## Dépannage

### Le navigateur Chrome n'est pas détecté
- Installez Google Chrome ou Chromium
- Vérifiez que Chrome est dans le PATH système

### Connexion refusée à localhost:5269
- Assurez-vous que l'application CharacterManager est en cours d'exécution
- Vérifiez que le port 5269 est disponible

### Timeout - Éléments non trouvés
- Augmentez le délai WaitTimeoutSeconds dans BaseE2ETest
- Vérifiez que les sélecteurs CSS correspondent à l'interface réelle
- Vérifiez que les pages prennent plus de temps à charger
