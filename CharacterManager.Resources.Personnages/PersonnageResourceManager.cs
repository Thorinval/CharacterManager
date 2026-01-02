namespace CharacterManager.Resources.Personnages;

using System.Reflection;

/// <summary>
/// Service d'accès aux ressources d'images de personnages incluses dans l'assembly.
/// Les images sont organisées par personnage dans des sous-dossiers.
/// Chaque personnage peut avoir jusqu'à 4 images :
/// - {nom}.png : image détaillée
/// - {nom}_header.png : image d'en-tête (optionnel)
/// - {nom}_small_portrait.png : petit portrait
/// - {nom}_small_select.png : portrait en mode sélectionné
/// </summary>
public static class PersonnageResourceManager
{
    private static readonly Assembly Assembly = typeof(PersonnageResourceManager).Assembly;
    private const string ResourceNamespace = "CharacterManager.Resources.Personnages.Images";

    /// <summary>
    /// Récupère une image de personnage comme flux de bytes.
    /// </summary>
    /// <param name="personnageFolder">Nom du dossier du personnage (ex: "Alexa", "Hunter")</param>
    /// <param name="fileName">Nom du fichier image (ex: "alexa.png", "alexa_small_portrait.png")</param>
    /// <returns>Contenu du fichier en bytes, ou null si non trouvé</returns>
    public static byte[]? GetImageBytes(string personnageFolder, string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{personnageFolder}.{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Récupère une image de personnage comme flux.
    /// </summary>
    /// <param name="personnageFolder">Nom du dossier du personnage</param>
    /// <param name="fileName">Nom du fichier image</param>
    /// <returns>Stream de l'image, ou null si non trouvée</returns>
    public static Stream? GetImageStream(string personnageFolder, string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{personnageFolder}.{fileName}";
        return Assembly.GetManifestResourceStream(resourceName);
    }

    /// <summary>
    /// Liste toutes les ressources d'images disponibles dans l'assembly.
    /// Utile pour le débogage et la découverte des ressources.
    /// </summary>
    /// <returns>Liste des noms de ressources</returns>
    public static string[] GetAllResourceNames()
    {
        return Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourceNamespace))
            .ToArray();
    }

    /// <summary>
    /// Vérifie si une image de personnage existe dans les ressources.
    /// </summary>
    /// <param name="personnageFolder">Nom du dossier du personnage</param>
    /// <param name="fileName">Nom du fichier image</param>
    /// <returns>True si la ressource existe, sinon false</returns>
    public static bool ImageExists(string personnageFolder, string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{personnageFolder}.{fileName}";
        return Assembly.GetManifestResourceNames().Contains(resourceName);
    }

    /// <summary>
    /// Récupère toutes les images d'un personnage spécifique.
    /// </summary>
    /// <param name="personnageFolder">Nom du dossier du personnage</param>
    /// <returns>Dictionnaire avec le nom de fichier comme clé et les bytes comme valeur</returns>
    public static Dictionary<string, byte[]> GetAllPersonnageImages(string personnageFolder)
    {
        var result = new Dictionary<string, byte[]>();
        var prefix = $"{ResourceNamespace}.{personnageFolder}.";
        
        var resourceNames = Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix));

        foreach (var resourceName in resourceNames)
        {
            var fileName = resourceName.Substring(prefix.Length);
            using var stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                result[fileName] = memoryStream.ToArray();
            }
        }

        return result;
    }
}
