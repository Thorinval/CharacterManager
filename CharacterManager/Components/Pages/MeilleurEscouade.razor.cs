namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;

public partial class MeilleurEscouade
{
    private List<Personnage> topMercenaires = new();
    private Personnage? topCommandant;
    private List<Personnage> topAndroides = new();
    private int puissanceMax = 0;

    protected override void OnInitialized()
    {
        LoadTopPersonnages();
    }

    private void LoadTopPersonnages()
    {
        topMercenaires = PersonnageService.GetTopMercenaires(8).ToList();
        topCommandant = PersonnageService.GetTopCommandant();
        topAndroides = PersonnageService.GetTopAndroides(3).ToList();
        puissanceMax = PersonnageService.GetPuissanceMaxEscouade();
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
        var back = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var encodedBack = Uri.EscapeDataString(back);
        Navigation.NavigateTo($"/detail-personnage/{id}?filter={filter}&returnUrl={encodedBack}");
    }

    private string GetFilterForCommandants() => "commandants";
    private string GetFilterForMercenaires() => "mercenaires";
    private string GetFilterForAndroides() => "androides";

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
                return $"/images/personnages/{nomFichier}_header.png";
            }
        }
        return "/images/interface/hunter_header.png";
    }

    private void NavigateToCommandantDetail()
    {
        if (topCommandant != null)
        {
            NavigateToDetail(topCommandant.Id, GetFilterForCommandants(), "/meilleur-escouade");
        }
    }
}
