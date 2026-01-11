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
    internal List<Personnage> personnagesEscouade = new();
    internal List<Personnage> mercenaires = new();
    internal List<Personnage> commandants = new();
    internal List<Personnage> androides = new();
    internal List<Piece> luciePieces = new();

    internal bool showModal = false;
    internal Personnage currentPersonnage = new();
    internal bool isEditing = false;

    internal int puissanceEscouade = 0;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    [Inject]
    public IModalService ModalService { get; set; } = null!;

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
            TemplateEscouade.EnsureLuciePieceAspectColumns(DbContext);
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

    private void NavigateToDetail(int id)
    {
        ModalService.Open<Modal.DetailPersonnageModal>(
            new Dictionary<string, object> { { "PersonnageId", id } },
            ModalSize.XL
        );
    }


    internal string GetCommandantHeaderImage()
    {
        if (commandants.Count != 0)
        {
            var commandant = commandants.First(c => c.Selectionne);
            return TemplateEscouade.ResolveHeaderImage(commandant.Nom);

        }
        return AppConstants.Paths.GenericCommandantHeader;
    }

    internal void NavigateToCommandantDetail()
    {
        if (commandants.Count != 0)
        {
            var cmd = commandants.First(c => c.Selectionne);
            NavigateToDetail(cmd.Id);
        }
    }

    internal async Task SavePersonnage()
    {
        if (currentPersonnage.Id > 0)
        {
            await PersonnageService.UpdateAsync(currentPersonnage);
        }
        else
        {
            await PersonnageService.AddAsync(currentPersonnage);
        }
        LoadPersonnages();
        CloseModal();
    }

    internal void ChangePuissanceEscouade(int delta)
    {
        currentPersonnage.Puissance = Math.Max(0, currentPersonnage.Puissance + delta);
    }

    internal static int GetPiecePower(Piece piece) => piece.Puissance;
}
