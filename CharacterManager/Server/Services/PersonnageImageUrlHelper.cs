namespace CharacterManager.Server.Services;

using CharacterManager.Server.Constants;

/// <summary>
/// Service helper pour générer les URLs des images de personnages.
/// Depuis la v0.12.1, les images sont servies depuis la DLL CharacterManager.Resources.Personnages
/// via l'API /api/resources/personnages/{personnage}/{fichier}
/// </summary>
public static class PersonnageImageUrlHelper
{
    /// <summary>
    /// Génère l'URL de l'image détaillée d'un personnage.
    /// Format: /api/resources/personnages/{PersonnageFolder}/{nom}.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage (ex: "Alexa", "Hunter")</param>
    /// <returns>URL complète de l'image détaillée</returns>
    public static string GetImageDetailUrl(string nomPersonnage)
    {
        var folder = NormalizePersonnageName(nomPersonnage);
        var fileName = $"{nomPersonnage.ToLower().Replace(" ", "_")}.png";
        return $"{AppConstants.Paths.ImagesPersonnages}/{folder}/{fileName}";
    }

    /// <summary>
    /// Génère l'URL de l'image d'en-tête d'un personnage.
    /// Format: /api/resources/personnages/{PersonnageFolder}/{nom}_header.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <returns>URL complète de l'image d'en-tête</returns>
    public static string GetImageHeaderUrl(string nomPersonnage)
    {
        var folder = NormalizePersonnageName(nomPersonnage);
        var fileName = $"{nomPersonnage.ToLower().Replace(" ", "_")}_header.png";
        return $"{AppConstants.Paths.ImagesPersonnages}/{folder}/{fileName}";
    }

    /// <summary>
    /// Génère l'URL du petit portrait d'un personnage.
    /// Format: /api/resources/personnages/{PersonnageFolder}/{nom}_small_portrait.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <returns>URL complète du petit portrait</returns>
    public static string GetImageSmallPortraitUrl(string nomPersonnage)
    {
        var folder = NormalizePersonnageName(nomPersonnage);
        var fileName = $"{nomPersonnage.ToLower().Replace(" ", "_")}_small_portrait.png";
        return $"{AppConstants.Paths.ImagesPersonnages}/{folder}/{fileName}";
    }

    /// <summary>
    /// Génère l'URL du portrait en mode sélectionné d'un personnage.
    /// Format: /api/resources/personnages/{PersonnageFolder}/{nom}_small_select.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <returns>URL complète du portrait sélectionné</returns>
    public static string GetImageSmallSelectUrl(string nomPersonnage)
    {
        var folder = NormalizePersonnageName(nomPersonnage);
        var fileName = $"{nomPersonnage.ToLower().Replace(" ", "_")}_small_select.png";
        return $"{AppConstants.Paths.ImagesPersonnages}/{folder}/{fileName}";
    }

    /// <summary>
    /// Normalise le nom du personnage pour créer le nom du dossier.
    /// Convertit en PascalCase (première lettre de chaque mot en majuscule).
    /// Ex: "alexa" -> "Alexa", "o-rinn" -> "ORinn", "zoe et chloe" -> "ZoeEtChloe"
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage à normaliser</param>
    /// <returns>Nom du dossier en PascalCase</returns>
    public static string NormalizePersonnageName(string nomPersonnage)
    {
        if (string.IsNullOrWhiteSpace(nomPersonnage))
            return string.Empty;

        // Remplacer les espaces et tirets par des séparateurs
        var parts = nomPersonnage
            .ToLower()
            .Replace("'", "")
            .Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);

        // Convertir chaque partie en PascalCase
        var pascalCaseParts = parts.Select(part =>
        {
            if (string.IsNullOrEmpty(part)) return string.Empty;
            return char.ToUpper(part[0]) + part.Substring(1);
        });

        return string.Join("", pascalCaseParts);
    }

    /// <summary>
    /// Génère l'URL legacy (v0.12.0 et antérieures) pour compatibilité descendante.
    /// À utiliser uniquement si la nouvelle API retourne 404.
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <param name="suffix">Suffixe de l'image ("", "_header", "_small_portrait", "_small_select")</param>
    /// <param name="extension">Extension du fichier (".png", ".jpg")</param>
    /// <returns>URL legacy de l'image</returns>
    public static string GetLegacyImageUrl(string nomPersonnage, string suffix = "", string extension = ".png")
    {
        var fileName = $"{nomPersonnage.ToLower().Replace(" ", "_")}{suffix}{extension}";
        return $"{AppConstants.Paths.ImagesPersonnagesLegacy}/{fileName}";
    }
}
