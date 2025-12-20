namespace CharacterManager.Components.Pages;

using System;
using System.IO;
using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;

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
}
