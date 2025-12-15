namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Components;

public partial class Inventaire
{
    [Inject]
    public PersonnageService PersonnageService { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public CsvImportService CsvImportService { get; set; } = null!;

    private List<Personnage> personnages = new();
    private List<Personnage> personnagesFiltres = new();
    private bool showModal = false;
    private bool isEditing = false;
    private Personnage currentPersonnage = new();
    
    // Tri
    private string sortColumn = "Nom";
    private bool sortAscending = true;
    
    // Sélection multiple
    private HashSet<int> selectedPersonnages = new();
    private bool showBulkEditModal = false;
    private string bulkEditProperty = "";
    private string bulkEditValue = "";
    private bool selectAllChecked
    {
        get => selectedPersonnages.Count == personnagesFiltres.Count && personnagesFiltres.Count > 0;
        set => SelectAll();
    }
    
    private IEnumerable<IGrouping<TypePersonnage, Personnage>> GroupedPersonnages =>
        personnagesFiltres.GroupBy(p => p.Type)
            .OrderBy(g => g.Key == TypePersonnage.Commandant ? 1 : g.Key == TypePersonnage.Mercenaire ? 2 : 3);
    
    // Filtre
    private string searchTerm = "";
    
    // Mode d'affichage
    private string viewMode = "grid";

    protected override void OnInitialized()
    {
        LoadPersonnages();
        // Charger un template si présent dans l'URL
        var uri = new Uri(Navigation.Uri);
        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals("templateId", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(Uri.UnescapeDataString(kv[1]), out var templateId))
                    {
                        showTemplateEditor = true;
                        templates = PersonnageService.GetAllTemplates().ToList();
                        selectedTemplateId = templateId;
                        _ = InvokeAsync(async () => await LoadSelectedTemplate());
                    }
                }
            }
        }
    }

    private string GetViewModeClass(string mode)
    {
        return viewMode == mode ? "btn-primary" : "btn-outline-secondary";
    }

    private string GetContainerClass()
    {
        return viewMode == "grid" ? "personnages-grid" : "personnages-list";
    }

    private string GetContainerClassCompact()
    {
        return "personnages-grid-compact";
    }

    private void LoadPersonnages()
    {
        personnages = PersonnageService.GetAll().ToList();
        ApplyFiltersAndSorting();
    }

    private void ApplyFiltersAndSorting()
    {
        // Appliquer le filtre
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            personnagesFiltres = new List<Personnage>(personnages);
        }
        else
        {
            personnagesFiltres = personnages.Where(p => 
                p.Nom.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || p.Rarete.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || p.Type.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || p.Role.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || p.Faction.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || p.TypeAttaque.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
        }

        // Appliquer le tri par type d'abord (Commandant, Mercenaire, Androïde), puis par colonne sélectionnée
        var typeOrder = new Dictionary<TypePersonnage, int>
        {
            { TypePersonnage.Commandant, 1 },
            { TypePersonnage.Mercenaire, 2 },
            { TypePersonnage.Androïde, 3 }
        };

        personnagesFiltres = sortColumn switch
        {
            "Nom" => sortAscending 
                ? personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Nom).ToList() 
                : personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Nom).ToList(),
            "Rarete" => sortAscending 
                ? personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Rarete).ToList() 
                : personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Rarete).ToList(),
            "Niveau" => sortAscending 
                ? personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Niveau).ToList() 
                : personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Niveau).ToList(),
            "Type" => sortAscending 
                ? personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Type).ToList() 
                : personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Type).ToList(),
            "Rang" => sortAscending 
                ? personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Rang).ToList()
                : personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Rang).ToList(),
            _ => personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Nom).ToList()
        };
    }

    private void SortBy(string column)
    {
        if (sortColumn == column)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
        ApplyFiltersAndSorting();
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        OnSearchChanged(e.Value?.ToString() ?? "");
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        // Ne filtrer que si au moins 2 caractères sont saisis
        if (searchTerm.Length >= 2 || string.IsNullOrWhiteSpace(searchTerm))
        {
            ApplyFiltersAndSorting();
        }
    }

    private void ClearSearch()
    {
        searchTerm = "";
        ApplyFiltersAndSorting();
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

    private void ToggleSelection(int id)
    {
        if (selectedPersonnages.Contains(id))
        {
            selectedPersonnages.Remove(id);
        }
        else
        {
            selectedPersonnages.Add(id);
        }
    }

    private void SelectAll()
    {
        if (selectedPersonnages.Count == personnagesFiltres.Count)
        {
            selectedPersonnages.Clear();
        }
        else
        {
            selectedPersonnages = new HashSet<int>(personnagesFiltres.Select(p => p.Id));
        }
    }

    private void ShowBulkEditModal()
    {
        if (selectedPersonnages.Any())
        {
            showBulkEditModal = true;
        }
    }

    private void ApplyBulkEdit()
    {
        if (string.IsNullOrEmpty(bulkEditProperty) || selectedPersonnages.Count == 0)
            return;

        foreach (var id in selectedPersonnages)
        {
            var personnage = personnages.FirstOrDefault(p => p.Id == id);
            if (personnage != null)
            {
                switch (bulkEditProperty)
                {
                    case "Niveau":
                        if (int.TryParse(bulkEditValue, out int niveau))
                            personnage.Niveau = niveau;
                        break;
                    case "TypeAttaque":
                        if (Enum.TryParse<CharacterManager.Server.Models.TypeAttaque>(bulkEditValue, out var typeAttaqueValue))
                            personnage.TypeAttaque = typeAttaqueValue;
                        break;
                }
                PersonnageService.Update(personnage);
            }
        }

        LoadPersonnages();
        selectedPersonnages.Clear();
        showBulkEditModal = false;
        bulkEditProperty = "";
        bulkEditValue = "";
    }

    private void ShowAddModal()
    {
        currentPersonnage = new Personnage();
        isEditing = false;
        showModal = true;
        StateHasChanged();
    }

    private void EditPersonnage(Personnage personnage)
    {
        currentPersonnage = new Personnage
        {
            Id = personnage.Id,
            Nom = personnage.Nom,
            Rarete = personnage.Rarete,
            Niveau = personnage.Niveau,
            Type = personnage.Type,
            Rang = personnage.Rang,
            Puissance = personnage.Puissance,
            PA = personnage.PA,
            PV = personnage.PV,
            Role = personnage.Role,
            Faction = personnage.Faction,
            ImageUrlDetail = personnage.ImageUrlDetail,
            ImageUrlPreview = personnage.ImageUrlPreview,
            ImageUrlSelected = personnage.ImageUrlSelected,
            Description = personnage.Description,
            Selectionne = personnage.Selectionne,
            TypeAttaque = personnage.TypeAttaque
        };
        isEditing = true;
        showModal = true;
        StateHasChanged();
    }

    private void DeletePersonnage(int id)
    {
        PersonnageService.Delete(id);
        LoadPersonnages();
    }

    private void SavePersonnage()
    {
        if (isEditing)
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

    private void CloseModal()
    {
        showModal = false;
        currentPersonnage = new Personnage();
        StateHasChanged();
    }

    private void SortByNom() => SortBy("Nom");
    private void SortByRarete() => SortBy("Rarete");
    private void SortByNiveau() => SortBy("Niveau");
    private void SortByType() => SortBy("Type");
    private void SortByRang() => SortBy("Rang");

    private async Task DeleteSelectedPersonnages()
    {
        if (selectedPersonnages.Any())
        {
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"Êtes-vous sûr de vouloir supprimer {selectedPersonnages.Count} personnage(s) sélectionné(s) ? Cette action est irréversible.");
            if (confirmed)
            {
                foreach (var id in selectedPersonnages)
                {
                    PersonnageService.Delete(id);
                }
                LoadPersonnages();
                selectedPersonnages.Clear();
            }
        }
    }

    private void ViewPersonnage(int id)
    {
        Navigation.NavigateTo($"/detail-personnage/{id}");
    }

    private async Task ExportToCsv()
    {
        try
        {
            var csvBytes = await CsvImportService.ExportToCsvAsync(personnagesFiltres);
            var fileName = $"inventaire_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            // Utiliser JavaScript pour télécharger le fichier
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(csvBytes));
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Erreur lors de l'export: {ex.Message}");
        }
    }

    // ===== Template Methods =====

    private bool showTemplateEditor = false;
    private Toast? toastRef;
    private string templateNom = string.Empty;
    private string templateDescription = string.Empty;
    private List<Personnage?> templatePersonnages = new();
    private List<int> templateSelectedIds = new();
    private List<Template> templates = new();
    private int selectedTemplateId = 0;

    private void OpenTemplateEditor()
    {
        showTemplateEditor = true;
        templateNom = string.Empty;
        templateDescription = string.Empty;
        templatePersonnages.Clear();
        templateSelectedIds.Clear();
        templates = PersonnageService.GetAllTemplates().ToList();
        selectedTemplateId = 0;
        viewMode = "grid";
    }

    private void CancelTemplateCreation()
    {
        showTemplateEditor = false;
        templateNom = string.Empty;
        templateDescription = string.Empty;
        templatePersonnages.Clear();
        templateSelectedIds.Clear();
        selectedTemplateId = 0;
    }

    private async Task HandleTemplateSelectionChanged(List<int> selectedIds)
    {
        templateSelectedIds = selectedIds;
        // Recharger les personnages sélectionnés
        templatePersonnages.Clear();
        foreach (var id in selectedIds)
        {
            var p = await GetPersonnageById(id);
            if (p != null)
                templatePersonnages.Add(p);
        }
    }

    private Task<Personnage?> GetPersonnageById(int id)
    {
        return Task.FromResult(PersonnageService.GetById(id));
    }

    private async Task SaveTemplate()
    {
        if (string.IsNullOrEmpty(templateNom) || templateSelectedIds.Count == 0)
            return;

        try
        {
            var template = await PersonnageService.CreateTemplateAsync(
                templateNom,
                templateDescription,
                templateSelectedIds
            );

            toastRef?.Show($"Template '{template.Nom}' créé avec succès!", "success");
            CancelTemplateCreation();
            templates = PersonnageService.GetAllTemplates().ToList();
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de la création du template: {ex.Message}", "error");
        }
    }

    private async Task LoadSelectedTemplate()
    {
        if (selectedTemplateId == 0)
            return;

        var template = await PersonnageService.GetTemplateAsync(selectedTemplateId);
        if (template is null)
        {
            toastRef?.Show("Template introuvable", "error");
            return;
        }

        var ids = template.GetPersonnageIds();
        templateSelectedIds = ids;
        templatePersonnages = new List<Personnage?>();
        foreach (var id in ids)
        {
            var p = PersonnageService.GetById(id);
            if (p != null)
                templatePersonnages.Add(p);
        }
        templateNom = template.Nom;
        templateDescription = template.Description;
        toastRef?.Show($"Template '{template.Nom}' chargé.", "info");
    }

    private async Task ExportTemplateAsCsv()
    {
        if (templateSelectedIds.Count == 0)
            return;

        try
        {
            var csvBytes = await CsvImportService.ExportToCsvAsync(templatePersonnages.Where(p => p != null).Select(p => p!));
            var fileName = $"template_{templateNom}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(csvBytes));
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de l'export du template: {ex.Message}", "error");
        }
    }

    private Task HandleInvalidDrop(string message)
    {
        toastRef?.Show(message, "warning");
        return Task.CompletedTask;
    }

    // Drag & Drop depuis les cartes
    private int? currentlyDraggedId;

    private void HandleDragStart(DragEventArgs e, Personnage personnage)
    {
        currentlyDraggedId = personnage.Id;
        StateHasChanged();
    }
}
