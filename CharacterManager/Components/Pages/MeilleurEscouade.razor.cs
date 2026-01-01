namespace CharacterManager.Components.Pages;

using System;
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
    private List<Piece> luciePieces = [];

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    [Inject]
    public IModalService ModalService { get; set; } = null!;

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
        topMercenaires = [.. PersonnageService.GetTopMercenaires(8)];
        topCommandant = PersonnageService.GetTopCommandant();
        topAndroides = [.. PersonnageService.GetTopAndroides(3)];
        puissanceMax = PersonnageService.GetPuissanceMaxEscouade();
        luciePieces= [.. PersonnageService.GetTopLucieRooms(2)];
        
        StateHasChanged();
    }
    
    private void NavigateToDetail(int id, string filter, string? returnUrl = null)
    {
        Console.WriteLine($"[MeilleurEscouade] NavigateToDetail appelé avec ID={id}, filter={filter}");
        var perso = topMercenaires.Concat(topAndroides).FirstOrDefault(p => p.Id == id) ?? topCommandant;
        Console.WriteLine($"[MeilleurEscouade] Personnage trouvé: {perso?.Nom} (ID={perso?.Id})");
        
        ModalService.Open<CharacterManager.Components.Modal.DetailPersonnageModal>(
            new Dictionary<string, object> { { "PersonnageId", id } },
            ModalSize.XL
        );
    }

    private string GetCommandantHeaderImage()
    {
        if (topCommandant != null)
        {
            return TemplateEscouade.ResolveHeaderImage(topCommandant.Nom);
        }
        return AppConstants.Paths.GenericCommandantHeader;
    }

    private void NavigateToCommandantDetail()
    {
        if (topCommandant != null)
        {
            NavigateToDetail(topCommandant.Id, TemplateEscouade.GetFilterForCommandants(), "/meilleur-escouade");
        }
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
            Console.WriteLine($"[MeilleurEscouade] Failed to ensure aspect columns: {ex.Message}");
        }
    }
}
