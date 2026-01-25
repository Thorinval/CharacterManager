namespace CharacterManager.Components.Pages;

using System.IO;
using System.Text;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Services;
using CharacterManager.Server.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

public static class TemplateEscouade
{
    public const string FilterEscouade = "escouade";
    public const string FilterCommandants = "commandants";
    public const string FilterMercenaires = "mercenaires";
    public const string FilterAndroides = "androides";

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
    internal static bool FileExists(string relativePath)
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
        var starsBuilder = new StringBuilder();
        for (int i = 1; i <= 7; i++)
        {
            if (i <= rank)
            {
                starsBuilder.Append("<span style='color: #FFD700;'>★</span>");
            }
            else
            {
                starsBuilder.Append("<span style='color: #CCCCCC;'>☆</span>");
            }
        }
        return new MarkupString(starsBuilder.ToString());
    }

     public static void EnsureLuciePieceAspectColumns(ApplicationDbContext DbContext)
    {
        try
        {
            const string hydratedTactiques = "{\"Nom\":\"Aspects tactiques\",\"Puissance\":0,\"Bonus\":[]}";
            const string hydratedStrategiques = "{\"Nom\":\"Aspects stratégiques\",\"Puissance\":0,\"Bonus\":[]}";

            using var conn = (SqliteConnection)DbContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            var hasTactiques = false;
            var hasStrategiques = false;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(Pieces);";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var name = reader.GetString(1);
                    if (string.Equals(name, "AspectsTactiques", StringComparison.OrdinalIgnoreCase)) hasTactiques = true;
                    if (string.Equals(name, "AspectsStrategiques", StringComparison.OrdinalIgnoreCase)) hasStrategiques = true;
                }
            }

            if (!hasTactiques)
            {
                DbContext.Database.ExecuteSqlRaw("ALTER TABLE Pieces ADD COLUMN AspectsTactiques TEXT NOT NULL DEFAULT '';");
            }
            if (!hasStrategiques)
            {
                DbContext.Database.ExecuteSqlRaw("ALTER TABLE Pieces ADD COLUMN AspectsStrategiques TEXT NOT NULL DEFAULT '';");
            }

            DbContext.Database.ExecuteSql($"UPDATE Pieces SET AspectsTactiques = {hydratedTactiques} WHERE AspectsTactiques IS NULL OR AspectsTactiques = '';");
            DbContext.Database.ExecuteSql($"UPDATE Pieces SET AspectsStrategiques = {hydratedStrategiques} WHERE AspectsStrategiques IS NULL OR AspectsStrategiques = '';");
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"[Escouade] Failed to ensure aspect columns: {ex.Message}");
        }
    }   
}


