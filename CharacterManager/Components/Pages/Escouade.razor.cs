namespace CharacterManager.Components.Pages;

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

    private string GetFilterForEscouade() => "escouade";
    private string GetFilterForCommandants() => "commandants";
    private string GetFilterForMercenaires() => "mercenaires";
    private string GetFilterForAndroides() => "androides";

    private string GetCommandantHeaderImage()
    {
        if (commandants.Any())
        {
            var commandant = commandants.First();
            if (!string.IsNullOrEmpty(commandant.ImageUrlHeader))
            {
                return commandant.ImageUrlHeader;
            }
            if (!string.IsNullOrEmpty(commandant.Nom))
            {
                var nomFichier = commandant.Nom.ToLower().Replace(" ", "_");
                return $"{AppConstants.Paths.ImagesPersonnages}/{nomFichier}{AppConstants.ImageSuffixes.Header}{AppConstants.FileExtensions.Png}";
            }
        }
        return AppConstants.Paths.HunterHeader;
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
}
