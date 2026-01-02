using CharacterManager.Resources.Personnages;

Console.WriteLine("=== Test de CharacterManager.Resources.Personnages ===\n");

// Test 1: Liste toutes les ressources
Console.WriteLine("1. Liste des ressources embarquées:");
var allResources = PersonnageResourceManager.GetAllResourceNames();
Console.WriteLine($"   Nombre total: {allResources.Length}\n");

// Afficher les 10 premières
Console.WriteLine("   10 premières ressources:");
foreach (var resource in allResources.Take(10))
{
    Console.WriteLine($"   - {resource}");
}
Console.WriteLine();

// Test 2: Vérifier l'existence d'images spécifiques
Console.WriteLine("2. Vérification d'existence d'images:");
var tests = new[]
{
    ("Alexa", "alexa_small_portrait.png"),
    ("Hunter", "hunter_small_select.png"),
    ("Kitty", "kitty.png"),
    ("Ravenna", "ravenna_small_portrait.png"),
    ("Inexistant", "test.png")
};

foreach (var (personnage, fichier) in tests)
{
    var exists = PersonnageResourceManager.ImageExists(personnage, fichier);
    var symbol = exists ? "✓" : "✗";
    Console.WriteLine($"   {symbol} {personnage}/{fichier}");
}
Console.WriteLine();

// Test 3: Récupérer une image
Console.WriteLine("3. Récupération d'une image:");
var imageBytes = PersonnageResourceManager.GetImageBytes("Alexa", "alexa_small_portrait.png");
if (imageBytes != null)
{
    Console.WriteLine($"   ✓ Image récupérée avec succès!");
    Console.WriteLine($"   Taille: {imageBytes.Length:N0} bytes");
    Console.WriteLine($"   Premier octet: 0x{imageBytes[0]:X2}");
}
else
{
    Console.WriteLine("   ✗ Échec de récupération");
}
Console.WriteLine();

// Test 4: Lister toutes les images d'un personnage
Console.WriteLine("4. Images du personnage 'Hunter':");
var hunterImages = PersonnageResourceManager.GetAllPersonnageImages("Hunter");
Console.WriteLine($"   Nombre d'images: {hunterImages.Count}");
foreach (var (fileName, bytes) in hunterImages)
{
    Console.WriteLine($"   - {fileName} ({bytes.Length:N0} bytes)");
}
Console.WriteLine();

Console.WriteLine("=== Tests terminés ===");
