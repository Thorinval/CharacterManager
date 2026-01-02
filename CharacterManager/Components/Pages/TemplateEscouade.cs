namespace CharacterManager.Components.Pages;

using System.IO;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;

public class TemplateEscouade
{
    public static string GetFilterForEscouade() => "escouade";
    public static string GetFilterForCommandants() => "commandants";
    public static string GetFilterForMercenaires() => "mercenaires";
    public static string GetFilterForAndroides() => "androides";    

    /// <summary>
    /// Résout le chemin d'image header en fonction du nom.
    /// Depuis v0.12.1, utilise le nouveau helper qui pointe vers les ressources embarquées.
    /// </summary>
    public static string ResolveHeaderImage(string? nomCommandant)
    {
        if (string.IsNullOrWhiteSpace(nomCommandant))
        {
            return AppConstants.Paths.GenericCommandantHeader;
        }

        return PersonnageImageUrlHelper.GetImageHeaderUrl(nomCommandant);
    }

    /// <summary>
    /// Vérifie si un fichier existe dans le répertoire wwwroot.
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    private static bool FileExists(string relativePath)
    {
        var physicalPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(physicalPath);
    }

    /// <summary>
    /// Génère une représentation en étoiles du rang.
    /// </summary>
    /// <param name="rank"></param>
    /// <returns></returns>
    public static MarkupString GetRankStars(int rank)
    {
        var stars = "";
        for (int i = 1; i <= 7; i++)
        {
            if (i <= rank)
            {
                stars += "<span style='color: #FFD700;'>★</span>";
            }
            else
            {
                stars += "<span style='color: #CCCCCC;'>☆</span>";
            }
        }
        return new MarkupString(stars);
    }
}
