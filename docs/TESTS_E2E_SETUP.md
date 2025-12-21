# Tests E2E Setup

## Guide d'utilisation des tests E2E avec Selenium

Les tests E2E ont été implémentés avec succès pour CharacterManager!

### Structure créée

📁 CharacterManager.E2ETests/
├── BaseE2ETest.cs          (Classe de base pour tous les tests)
├── HomePageTests.cs        (Tests de la page d'accueil)
├── InventairePageTests.cs  (Tests de la page inventaire)
├── NavigationTests.cs      (Tests de navigation)
└── README.md              (Documentation détaillée)

### Démarrage rapide

#### 1. Démarrer l'application

```bash
cd d:\Devs\CharacterManager\CharacterManager
dotnet run
```

L'application sera accessible sur http://localhost:5269

#### 2. Exécuter tous les tests E2E

```bash
cd d:\Devs\CharacterManager\CharacterManager.Tests\CharacterManager.E2ETests
dotnet test
```

#### 3. Exécuter des tests spécifiques

```bash
# Tester uniquement la page d'accueil
dotnet test --filter "HomePageTests"

# Tester uniquement la page inventaire
dotnet test --filter "InventairePageTests"

# Tester uniquement la navigation
dotnet test --filter "NavigationTests"

# Exécuter un test spécifique
dotnet test --filter "HomePage_ShouldLoadSuccessfully"
```

### Tests disponibles

**HomePageTests** (4 tests)

- ✓ Charge de la page
- ✓ Affichage du contenu de bienvenue
- ✓ Existence de la navigation
- ✓ Affichage des infos de version

**InventairePageTests** (7 tests)

- ✓ Charge de la page inventaire
- ✓ Affichage du tableau
- ✓ Bouton "Ajouter" présent
- ✓ Ouverture du modal au clic
- ✓ Champs de formulaire dans le modal
- ✓ Affichage des colonnes du tableau
- ✓ Navigation vers les détails

**NavigationTests** (5 tests)

- ✓ Lien d'accueil existe
- ✓ Lien inventaire existe
- ✓ Navigation vers l'accueil
- ✓ Navigation vers l'inventaire
- ✓ Stabilité de la navigation

### Points clés de l'implémentation

1. **Classe de base (BaseE2ETest.cs)**
   - Gère l'initialisation et la fermeture du WebDriver
   - Fournit des méthodes utilitaires pour attendre les éléments
   - Utilise WebDriverManager pour gérer automatiquement ChromeDriver
   - Configure les options du navigateur (sans notifications, etc.)

2. **Attentes explicites**
   - Utilise WebDriverWait au lieu de délais fixes quand possible
   - Attend que les éléments soient cliquables avant d'interagir

3. **Gestion des erreurs**
   - Gère les cas où les éléments peuvent avoir différents sélecteurs
   - Dispose correctement des ressources

4. **Indépendance des tests**
   - Chaque test crée sa propre instance de navigateur
   - Aucune dépendance entre les tests

### Configuration requise

- ✓ Chrome/Chromium installé
- ✓ .NET 9.0
- ✓ Application CharacterManager en cours d'exécution

### Dépannage

**Erreur: "Can't connect to localhost:5269"**
→ Assurez-vous que l'application CharacterManager est en cours d'exécution

**Erreur: "Chrome not found"**
→ Installez Google Chrome ou Chromium

**Tests très lents**
→ Augmentez WaitTimeoutSeconds dans BaseE2ETest.cs si les pages prennent du temps à charger

### Personnalisation

Pour ajouter des tests pour de nouvelles pages:

```csharp
public class NouvellePage Tests : BaseE2ETest
{
    [Fact]
    public void NouvellePage_ShouldLoad()
    {
        // Act
        NavigateTo($"{BaseUrl}/nouvelle-page");
        Thread.Sleep(1000);

        // Assert
        var element = WaitForElement(By.ClassName("element-attendu"));
        Assert.NotNull(element);
    }
}
```
