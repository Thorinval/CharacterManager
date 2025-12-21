namespace CharacterManager.Components.Pages;

using System;
using System.IO;
using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

public partial class MeilleurEscouade
{
    private List<Personnage> topMercenaires = new();
    private Personnage? topCommandant;
    private List<Personnage> topAndroides = new();
    private int puissanceMax = 0;
    private List<Piece> luciePieces = new();

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    protected override void OnInitialized()
    {
        LoadTopPersonnages();
    }

    protected override void OnParametersSet()
    {
        LoadTopPersonnages();
    }

    private void LoadTopPersonnages()
    {
        topMercenaires = PersonnageService.GetTopMercenaires(8).ToList();
        topCommandant = PersonnageService.GetTopCommandant();
        topAndroides = PersonnageService.GetTopAndroides(3).ToList();
        puissanceMax = PersonnageService.GetPuissanceMaxEscouade();
        try
        {
            var lucie = DbContext.LucieHouses
                .Include(l => l.Pieces)
                .FirstOrDefault();
            luciePieces = lucie?.Pieces.Where(p => p.Selectionnee).ToList() ?? new();
        }
        catch (SqliteException ex) when (ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase))
        {
            EnsureLuciePieceAspectColumns();
            var lucie = DbContext.LucieHouses
                .Include(l => l.Pieces)
                .FirstOrDefault();
            luciePieces = lucie?.Pieces.Where(p => p.Selectionnee).ToList() ?? new();
        }
        StateHasChanged();
    }
    
    private MarkupString GetRankStars(int rank)
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

    private void NavigateToDetail(int id, string filter, string? returnUrl = null)
    {
        Console.WriteLine($"[MeilleurEscouade] NavigateToDetail appelé avec ID={id}, filter={filter}");
        var perso = topMercenaires.Concat(topAndroides).FirstOrDefault(p => p.Id == id) ?? topCommandant;
        Console.WriteLine($"[MeilleurEscouade] Personnage trouvé: {perso?.Nom} (ID={perso?.Id})");
        
        var back = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var encodedBack = Uri.EscapeDataString(back);
        Navigation.NavigateTo($"/detail-personnage/{id}?filter={filter}&returnUrl={encodedBack}");
    }

    private static string GetFilterForCommandants() => "commandants";
    private static string GetFilterForMercenaires() => "mercenaires";
    private static string GetFilterForAndroides() => "androides";

    private string GetCommandantHeaderImage()
    {
        if (topCommandant != null)
        {
            if (!string.IsNullOrEmpty(topCommandant.ImageUrlHeader))
            {
                return topCommandant.ImageUrlHeader;
            }
            if (!string.IsNullOrEmpty(topCommandant.Nom))
            {
                var nomFichier = topCommandant.Nom.ToLower().Replace(" ", "_");
                var standardCandidate = $"{AppConstants.Paths.ImagesPersonnages}/{nomFichier}{AppConstants.ImageSuffixes.Header}{AppConstants.FileExtensions.Png}";

                if (FileExists(standardCandidate))
                {
                    return standardCandidate;
                }
            }
        }
        return AppConstants.Paths.GenericCommandantHeader; // Chemin d'image générique pour les commandants
    }

    private void NavigateToCommandantDetail()
    {
        if (topCommandant != null)
        {
            NavigateToDetail(topCommandant.Id, GetFilterForCommandants(), "/meilleur-escouade");
        }
    }

    private static bool FileExists(string relativePath)
    {
        var physicalPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(physicalPath);
    }

    private static int GetPiecePower(Piece piece) => piece.Puissance;

    private void EnsureLuciePieceAspectColumns()
    {
        try
        {
            const string hydratedTactiques = "{\"Nom\":\"Aspects tactiques\",\"Puissance\":0,\"Bonus\":[]}";
            const string hydratedStrategiques = "{\"Nom\":\"Aspects stratégiques\",\"Puissance\":0,\"Bonus\":[]}";

            using var conn = (SqliteConnection)DbContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(Pieces);";
            using var reader = cmd.ExecuteReader();
            var hasTactiques = false;
            var hasStrategiques = false;
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, "AspectsTactiques", StringComparison.OrdinalIgnoreCase)) hasTactiques = true;
                if (string.Equals(name, "AspectsStrategiques", StringComparison.OrdinalIgnoreCase)) hasStrategiques = true;
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
            Console.WriteLine($"[MeilleurEscouade] Failed to ensure aspect columns: {ex.Message}");
        }
    }
}
