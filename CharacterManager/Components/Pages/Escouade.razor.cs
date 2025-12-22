namespace CharacterManager.Components.Pages;

using System;
using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

public partial class Escouade
{
    private List<Personnage> personnagesEscouade = new();
    private List<Personnage> mercenaires = new();
    private List<Personnage> commandants = new();
    private List<Personnage> androides = new();
    private List<Piece> luciePieces = new();

    private bool showModal = false;
    private Personnage currentPersonnage = new();
    private bool isEditing = false;

    private int puissanceEscouade = 0;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    protected override void OnInitialized()
    {
        LoadPersonnages();
    }

    private void LoadPersonnages()
    {
        personnagesEscouade = PersonnageService.GetEscouade().ToList();
        mercenaires = PersonnageService.GetMercenaires(true).ToList();
        commandants = PersonnageService.GetCommandants(true).ToList();
        androides = PersonnageService.GetAndroides(true).ToList();
        puissanceEscouade = PersonnageService.GetPuissanceEscouade();

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
    }

    private void CloseModal()
    {
        showModal = false;
        currentPersonnage = new Personnage();
        StateHasChanged();
    }

    private void NavigateToDetail(int id, string filter, string? returnUrl = null)
    {
        var back = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var encodedBack = Uri.EscapeDataString(back);
        Navigation.NavigateTo($"/detail-personnage/{id}?filter={filter}&returnUrl={encodedBack}");
    }


    private string GetCommandantHeaderImage()
    {
        if (commandants.Count != 0)
        {
            var commandant = commandants.First();
            return TemplateEscouade.ResolveHeaderImage(commandant.Nom);

        }
        return AppConstants.Paths.GenericCommandantHeader;
    }

    private void NavigateToCommandantDetail()
    {
        if (commandants.Count != 0)
        {
            var cmd = commandants.First();
            NavigateToDetail(cmd.Id, TemplateEscouade.GetFilterForCommandants(), "/escouade");
        }
    }

    private void SavePersonnage()
    {
        if (currentPersonnage.Id > 0)
        {
            PersonnageService.Update(currentPersonnage);
        }
        else
        {
            PersonnageService.Add(currentPersonnage);
        }
        LoadPersonnages();
        CloseModal();
    }

    private void ChangePuissanceEscouade(int delta)
    {
        currentPersonnage.Puissance = Math.Max(0, currentPersonnage.Puissance + delta);
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
            Console.WriteLine($"[Escouade] Failed to ensure aspect columns: {ex.Message}");
        }
    }
}
