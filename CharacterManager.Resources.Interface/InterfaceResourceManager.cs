namespace CharacterManager.Resources.Interface;

using System.Reflection;

/// <summary>
/// Service d'accès aux ressources d'interface incluses dans l'assembly.
/// </summary>
public static class InterfaceResourceManager
{
    private static readonly Assembly Assembly = typeof(InterfaceResourceManager).Assembly;
    private const string ResourceNamespace = "CharacterManager.Resources.Interface.Images";

    /// <summary>
    /// Récupère une image d'interface comme flux de bytes.
    /// </summary>
    /// <param name="fileName">Nom du fichier image (ex: "fond_puissance.png")</param>
    /// <returns>Contenu du fichier en bytes, ou null si non trouvé</returns>
    public static byte[]? GetImageBytes(string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Récupère une image d'interface comme flux.
    /// </summary>
    /// <param name="fileName">Nom du fichier image (ex: "fond_puissance.png")</param>
    /// <returns>Stream du fichier, ou null si non trouvé</returns>
    public static Stream? GetImageStream(string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{fileName}";
        return Assembly.GetManifestResourceStream(resourceName);
    }

    /// <summary>
    /// Liste tous les fichiers images disponibles.
    /// </summary>
    /// <returns>Collection des noms de fichiers images</returns>
    public static IEnumerable<string> GetAvailableImages()
    {
        return Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourceNamespace))
            .Select(n => n.Substring(ResourceNamespace.Length + 1)); // +1 pour le point
    }
}
