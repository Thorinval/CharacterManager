namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    private Personnage? currentPerso;
    private Personnage editedPerso = new();
    private List<Personnage> tousLesPersonnages = new();
    private int indexActuel = 0;
    private bool isEditing = false;
    private bool HasNavigation => tousLesPersonnages.Count > 1;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Charger la liste filtrée à chaque navigation
        tousLesPersonnages = await GetListeFiltreeAsync();

        // Charger le personnage à chaque changement de paramètre (Id)
        currentPerso = tousLesPersonnages.FirstOrDefault(p => p.Id == Id) ?? await PersonnageService.GetByIdAsync(Id);

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
        
        StateHasChanged();
        await Task.Delay(100);
        
        // Aller au personnage précédent (wraparound circulaire)
        indexActuel = (indexActuel - 1 + tousLesPersonnages.Count) % tousLesPersonnages.Count;

        // Mettre à jour l'ID et recharger les données filtrées
        Id = tousLesPersonnages[indexActuel].Id;
        await OnParametersSetAsync();
        StateHasChanged();
    }

    private async Task NavigateToNext()
    {
        if (tousLesPersonnages.Count == 0) return;
        
        StateHasChanged();
        await Task.Delay(100);
        
        // Aller au personnage suivant (wraparound circulaire)
        indexActuel = (indexActuel + 1) % tousLesPersonnages.Count;

        // Mettre à jour l'ID et recharger les données filtrées
        Id = tousLesPersonnages[indexActuel].Id;
        await OnParametersSetAsync();
        StateHasChanged();
    }

    private void GoBack()
    {
        var target = string.IsNullOrWhiteSpace(ReturnUrl) ? "/inventaire" : Uri.UnescapeDataString(ReturnUrl);
        Navigation.NavigateTo(target);
    }

    private void EnterEditMode()
    {
        if (currentPerso == null) return;
        
        isEditing = true;
        // Créer une copie pour l'édition
        editedPerso = new Personnage
        {
            Id = currentPerso.Id,
            Nom = currentPerso.Nom,
            Rarete = currentPerso.Rarete,
            Type = currentPerso.Type,
            Niveau = currentPerso.Niveau,
            Rang = currentPerso.Rang,
            Puissance = currentPerso.Puissance,
            PA = currentPerso.PA,
            PV = currentPerso.PV,
            Role = currentPerso.Role,
            Faction = currentPerso.Faction,
            TypeAttaque = currentPerso.TypeAttaque,
            Selectionne = currentPerso.Selectionne,
            Description = currentPerso.Description,
            ImageUrlHeader = currentPerso.ImageUrlHeader
        };
    }

    private async Task SaveChanges()
    {
        if (editedPerso == null) return;

        try
        {
            PersonnageService.Update(editedPerso);
            currentPerso = editedPerso;
            isEditing = false;
            await JSRuntime.InvokeVoidAsync("alert", "Personnage mis à jour avec succès.");
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Erreur lors de la sauvegarde: {ex.Message}");
        }
    }

    private void CancelEdit()
    {
        isEditing = false;
        editedPerso = new();
    }

    private string GetRarityClass(Rarete rarete)
    {
        return rarete switch
        {
            Rarete.SSR => "danger",
            Rarete.SR => "warning",
            Rarete.R => "primary",
            _ => "secondary"
        };
    }

    private MarkupString RenderStars(int rang)
    {
        if (rang < 0 || rang > 7) rang = 0;
        
        var stars = "";
        for (int i = 1; i <= 7; i++)
        {
            if (i <= rang)
            {
                stars += "<span class=\"star filled\">★</span>";
            }
            else
            {
                stars += "<span class=\"star empty\">☆</span>";
            }
        }
        
        return new MarkupString(stars);
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

    private async Task<List<Personnage>> GetListeFiltreeAsync()
    {
        return Filter?.ToLower() switch
        {
            "escouade" => (await PersonnageService.GetEscouadeAsync()).ToList(),
            "mercenaires" => (await PersonnageService.GetMercenairesAsync(true)).ToList(),
            "commandants" => (await PersonnageService.GetCommandantsAsync(true)).ToList(),
            "androides" => (await PersonnageService.GetAndroïdesAsync(true)).ToList(),
            _ => (await PersonnageService.GetAllAsync()).ToList(),
        };
    }

}
