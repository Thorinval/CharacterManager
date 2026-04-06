namespace CharacterManager.Server.Services;

using CharacterManager.Resources.Personnages;
using CharacterManager.Server.Constants;
using System.Globalization;
using System.Text;

/// <summary>
/// Service helper pour générer les URLs des images de personnages.
/// Depuis la v0.12.1, les images sont servies depuis la DLL CharacterManager.Resources.Personnages
/// via l'API versionnée /api/v1/resources/personnages/{personnage}/{fichier}
/// </summary>
public static class PersonnageImageUrlHelper
{
    /// <summary>
    /// Génère l'URL de l'image détaillée d'un personnage.
    /// Format: /api/v1/resources/personnages/{PersonnageFolder}/{nom}.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage (ex: "Alexa", "Hunter")</param>
    /// <returns>URL complète de l'image détaillée</returns>
    public static string GetImageDetailUrl(string nomPersonnage)
        => GetBestAvailableImageUrl(nomPersonnage, "", "_small_portrait", "_small_select", "_header");

    /// <summary>
    /// Génère l'URL de l'image d'en-tête d'un personnage.
    /// Format: /api/v1/resources/personnages/{PersonnageFolder}/{nom}_header.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <returns>URL complète de l'image d'en-tête</returns>
    public static string GetImageHeaderUrl(string nomPersonnage)
        => GetBestAvailableImageUrl(nomPersonnage, "_header", "", "_small_portrait", "_small_select");

    /// <summary>
    /// Génère l'URL du petit portrait d'un personnage.
    /// Format: /api/v1/resources/personnages/{PersonnageFolder}/{nom}_small_portrait.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <returns>URL complète du petit portrait</returns>
    public static string GetImageSmallPortraitUrl(string nomPersonnage)
        => GetBestAvailableImageUrl(nomPersonnage, "_small_portrait", "_small_select", "", "_header");

    /// <summary>
    /// Génère l'URL du portrait en mode sélectionné d'un personnage.
    /// Format: /api/v1/resources/personnages/{PersonnageFolder}/{nom}_small_select.png
    /// </summary>
    /// <param name="nomPersonnage">Nom du personnage</param>
    /// <returns>URL complète du portrait sélectionné</returns>
    public static string GetImageSmallSelectUrl(string nomPersonnage)
        => GetBestAvailableImageUrl(nomPersonnage, "_small_select", "_small_portrait", "", "_header");

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

    private static string GetBestAvailableImageUrl(string nomPersonnage, params string[] candidateSuffixes)
    {
        if (string.IsNullOrWhiteSpace(nomPersonnage))
        {
            return GetPlaceholderImageDataUrl("?");
        }

        var folder = NormalizePersonnageName(nomPersonnage);
        var normalizedFileBaseName = nomPersonnage.ToLower().Replace(" ", "_");

        foreach (var suffix in candidateSuffixes)
        {
            foreach (var extension in new[] { ".png", ".jpg" })
            {
                var fileName = $"{normalizedFileBaseName}{suffix}{extension}";
                if (PersonnageResourceManager.ImageExists(folder, fileName))
                {
                    return $"{AppConstants.Paths.ImagesPersonnages}/{folder}/{fileName}";
                }
            }
        }

        return GetPlaceholderImageDataUrl(nomPersonnage);
    }

    private static string GetPlaceholderImageDataUrl(string nomPersonnage)
    {
        var label = string.IsNullOrWhiteSpace(nomPersonnage)
            ? "?"
            : string.Concat(
                nomPersonnage
                    .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(static part => !string.IsNullOrWhiteSpace(part))
                    .Take(2)
                    .Select(static part => char.ToUpperInvariant(part[0])));

        if (string.IsNullOrWhiteSpace(label))
        {
            label = nomPersonnage[..1].ToUpper(CultureInfo.InvariantCulture);
        }

        var safeName = System.Security.SecurityElement.Escape(nomPersonnage) ?? "Personnage";
        var safeLabel = System.Security.SecurityElement.Escape(label) ?? "?";
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 240 320'><defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'><stop offset='0%' stop-color='#4b5d7a'/><stop offset='100%' stop-color='#1f2937'/></linearGradient></defs><rect width='240' height='320' rx='18' fill='url(#g)'/><circle cx='120' cy='110' r='52' fill='#94a3b8' opacity='0.35'/><text x='120' y='126' text-anchor='middle' font-family='Arial, Helvetica, sans-serif' font-size='40' font-weight='700' fill='white'>{safeLabel}</text><text x='120' y='265' text-anchor='middle' font-family='Arial, Helvetica, sans-serif' font-size='20' fill='white'>{safeName}</text></svg>";

        return $"data:image/svg+xml;charset=utf-8,{Uri.EscapeDataString(svg)}";
    }
}




