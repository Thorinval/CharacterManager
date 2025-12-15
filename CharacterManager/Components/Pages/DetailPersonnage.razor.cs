namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using System.Linq;

public partial class DetailPersonnage
{
    [Parameter]
    public int Id { get; set; }

    [SupplyParameterFromQuery(Name = "filter")]
    public string? Filter { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    private Personnage? currentPerso;
    private List<Personnage> tousLesPersonnages = new();
    private int indexActuel = 0;
    private string animationClass = "slide-in-left";
    private bool HasNavigation => tousLesPersonnages.Count > 1;

    protected override async Task OnParametersSetAsync()
    {
        // Charger la liste filtrée à chaque navigation
        tousLesPersonnages = GetListeFiltree();
        animationClass = ""; // Pas d'animation au premier chargement

        // Charger le personnage à chaque changement de paramètre (Id)
        currentPerso = tousLesPersonnages.FirstOrDefault(p => p.Id == Id) ?? PersonnageService.GetById(Id);

        // Trouver l'index du personnage actuel
        if (currentPerso != null)
        {
            indexActuel = tousLesPersonnages.FindIndex(p => p.Id == currentPerso.Id);
            if (indexActuel == -1)
            {
                if (tousLesPersonnages.Count > 0)
                {
                    indexActuel = 0;
                    currentPerso = tousLesPersonnages[0];
                    Id = currentPerso.Id;
                }
                else
                {
                    indexActuel = 0;
                }
            }
        }

        await base.OnParametersSetAsync();
    }

    private async Task NavigateToPrevious()
    {
        if (tousLesPersonnages.Count == 0) return;
        
        // Déclencher l'animation de sortie vers la droite
        animationClass = "slide-right";
        StateHasChanged();
        
        // Attendre que l'animation soit terminée
        await Task.Delay(500);
        
        // Aller au personnage précédent (wraparound circulaire)
        indexActuel = (indexActuel - 1 + tousLesPersonnages.Count) % tousLesPersonnages.Count;

        // Mettre à jour l'ID et recharger les données filtrées
        Id = tousLesPersonnages[indexActuel].Id;
        await OnParametersSetAsync();
        animationClass = "slide-in-left";
        StateHasChanged();
    }

    private async Task NavigateToNext()
    {
        if (tousLesPersonnages.Count == 0) return;
        
        // Déclencher l'animation de sortie vers la gauche
        animationClass = "slide-left";
        StateHasChanged();
        
        // Attendre que l'animation soit terminée
        await Task.Delay(500);
        
        // Aller au personnage suivant (wraparound circulaire)
        indexActuel = (indexActuel + 1) % tousLesPersonnages.Count;

        // Mettre à jour l'ID et recharger les données filtrées
        Id = tousLesPersonnages[indexActuel].Id;
        await OnParametersSetAsync();
        animationClass = "slide-in-right";
        StateHasChanged();
    }

    private void GoBack()
    {
        var target = string.IsNullOrWhiteSpace(ReturnUrl) ? "/inventaire" : Uri.UnescapeDataString(ReturnUrl);
        Navigation.NavigateTo(target);
    }

    private List<Personnage> GetListeFiltree()
    {
        return Filter?.ToLower() switch
        {
            "escouade" => PersonnageService.GetEscouade().ToList(),
            "mercenaires" => PersonnageService.GetMercenaires(true).ToList(),
            "commandants" => PersonnageService.GetCommandants(true).ToList(),
            "androides" => PersonnageService.GetAndroides(true).ToList(),
            _ => PersonnageService.GetAll().ToList(),
        };
    }
}
