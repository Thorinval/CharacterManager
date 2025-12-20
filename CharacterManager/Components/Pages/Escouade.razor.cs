namespace CharacterManager.Components.Pages;

using System;
using System.IO;
using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;

public partial class Escouade
{
    private List<Personnage> personnagesEscouade = new();
    private List<Personnage> mercenaires = new();   
    private List<Personnage> commandants = new();   
    private List<Personnage> androides = new();

    private bool showModal = false;
    private Personnage currentPersonnage = new();
    private bool isEditing = false;

    private int puissanceMax = 0;

    private int puissanceEscouade = 0;

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

    private static string GetFilterForEscouade() => "escouade";
    private static string GetFilterForCommandants() => "commandants";
    private static string GetFilterForMercenaires() => "mercenaires";
    private static string GetFilterForAndroides() => "androides";

    private string GetCommandantHeaderImage()
    {
        if (commandants.Count == 0)
        {
            return AppConstants.Paths.GenericCommandantHeader;
        }

        var commandant = commandants.First();
        return ResolveHeaderImage(commandant.ImageUrlHeader, commandant.Nom);
    }

    private void NavigateToCommandantDetail()
    {
        if (commandants.Any())
        {
            var cmd = commandants.First();
            NavigateToDetail(cmd.Id, GetFilterForCommandants(), "/escouade");
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

    /// <summary>
    /// Résout le chemin d'image header en fonction du nom.
    /// </summary>
    private string ResolveHeaderImage(string? imageUrlHeader, string? nom)
    {
        if (!string.IsNullOrWhiteSpace(imageUrlHeader))
        {
            return imageUrlHeader;
        }

        if (string.IsNullOrWhiteSpace(nom))
        {
            return AppConstants.Paths.GenericCommandantHeader;
        }

        var nomFichier = nom.ToLower().Replace(" ", "_");
        var standardCandidate = $"{AppConstants.Paths.ImagesPersonnages}/{nomFichier}{AppConstants.ImageSuffixes.Header}{AppConstants.FileExtensions.Png}";

        if (FileExists(standardCandidate))
        {
            return standardCandidate;
        }

        return AppConstants.Paths.GenericCommandantHeader;
    }

    private static bool FileExists(string relativePath)
    {
        var physicalPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(physicalPath);
    }
}
