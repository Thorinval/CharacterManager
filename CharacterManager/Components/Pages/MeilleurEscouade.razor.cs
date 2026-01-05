namespace CharacterManager.Components.Pages;

using System;
using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Data;

public partial class MeilleurEscouade
{
    private List<Personnage> topMercenaires = new();
    private Personnage? topCommandant;
    private List<Personnage> topAndroides = new();
    internal int puissanceMax = 0;
    internal List<Piece> luciePieces = new();

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
        luciePieces = [.. PersonnageService.GetTopLucieRooms(2)];

        StateHasChanged();
    }

    private void NavigateToDetail(int id, string filter)
    {
        Console.WriteLine($"[MeilleurEscouade] NavigateToDetail appelé avec ID={id}, filter={filter}");
        var perso = topMercenaires.Concat(topAndroides).FirstOrDefault(p => p.Id == id) ?? topCommandant;
        Console.WriteLine($"[MeilleurEscouade] Personnage trouvé: {perso?.Nom} (ID={perso?.Id})");

        ModalService.Open<CharacterManager.Components.Modal.DetailPersonnageModal>(
            new Dictionary<string, object> { { "PersonnageId", id } },
            ModalSize.XL
        );
    }

    internal string GetCommandantHeaderImage()
    {
        if (topCommandant != null)
        {
            return TemplateEscouade.ResolveHeaderImage(topCommandant.Nom);
        }
        return AppConstants.Paths.GenericCommandantHeader;
    }

    internal void NavigateToCommandantDetail()
    {
        if (topCommandant != null)
        {
            NavigateToDetail(topCommandant.Id, TemplateEscouade.FilterCommandants);
        }
    }

    internal static int GetPiecePower(Piece piece) => piece.Puissance;
}
