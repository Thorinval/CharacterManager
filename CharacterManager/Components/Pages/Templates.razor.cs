namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Components;

public partial class Templates
{
    [Inject]
    public PersonnageService PersonnageService { get; set; } = null!;

    [Inject]
    public PmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    private List<Template> templates = new();
    private Toast? toastRef;
    private int? editingTemplateId = null;
    private string editingName = string.Empty;

    // Template editor state
    private bool showTemplateEditor = false;
    private string templateNom = string.Empty;
    private string templateDescription = string.Empty;
    private List<Personnage?> templatePersonnages = [];
    private List<int> templateSelectedIds = [];
    private int selectedTemplateId = 0;
    private string searchTerm = string.Empty;
    private int? currentlyDraggedId;
    private List<Personnage> personnagesFiltres = [];

    private IEnumerable<IGrouping<string, Personnage>> GroupedPersonnages
    {
        get
        {
            return personnagesFiltres
                .GroupBy(p => p.Type.ToString())
                .OrderBy(g => g.Key);
        }
    }

    protected override void OnInitialized()
    {
        templates = PersonnageService.GetAllTemplates().ToList();
        LoadPersonnages();
    }

    private void LoadPersonnages()
    {
        personnagesFiltres = PersonnageService.GetAll()
            .Where(p => p.Selectionne)
            .OrderByDescending(p => p.Puissance)
            .ToList();
    }

    private void OpenTemplateEditor()
    {
        showTemplateEditor = true;
        templateNom = string.Empty;
        templateDescription = string.Empty;
        templatePersonnages.Clear();
        templateSelectedIds.Clear();
        templates = [.. PersonnageService.GetAllTemplates()];
        selectedTemplateId = 0;
        LoadPersonnages();
    }

    private void CancelTemplateCreation()
    {
        showTemplateEditor = false;
        templateNom = string.Empty;
        templateDescription = string.Empty;
        templatePersonnages.Clear();
        templateSelectedIds.Clear();
        selectedTemplateId = 0;
        templates = PersonnageService.GetAllTemplates().ToList();
    }

    private async Task HandleTemplateSelectionChanged(List<int> selectedIds)
    {
        templateSelectedIds = selectedIds;
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

    private void HandleSearchInput(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    private void ClearSearch()
    {
        searchTerm = string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var all = PersonnageService.GetAll().Where(p => p.Selectionne).OrderByDescending(p => p.Puissance).ToList();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            personnagesFiltres = all;
        }
        else
        {
            var lower = searchTerm.ToLower();
            personnagesFiltres = all.Where(p => p.Nom.ToLower().Contains(lower)).ToList();
        }
    }

    private void HandleDragStart(DragEventArgs e, Personnage personnage)
    {
        currentlyDraggedId = personnage.Id;
    }

    private void HandleInvalidDrop()
    {
        currentlyDraggedId = null;
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
            toastRef?.Show($"Template '{templateNom}' créé avec succès", "success");
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

        try
        {
            var template = await PersonnageService.GetTemplateAsync(selectedTemplateId);
            if (template != null)
            {
                templateNom = template.Nom;
                templateDescription = template.Description ?? string.Empty;
                var ids = template.GetPersonnageIds();
                await HandleTemplateSelectionChanged(ids);
            }
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors du chargement du template: {ex.Message}", "error");
        }
    }

    private async Task ExportTemplateAsPml()
    {
        if (string.IsNullOrEmpty(templateNom) || templateSelectedIds.Count == 0)
            return;

        try
        {
            var template = new Template
            {
                Nom = templateNom,
                Description = templateDescription,
                DateCreation = DateTime.UtcNow,
                DateModification = DateTime.UtcNow,
                PuissanceTotal = templatePersonnages.Sum(p => p?.Puissance ?? 0)
            };
            template.SetPersonnageIds(templateSelectedIds);

            var pmlBytes = await PmlImportService.ExporterTemplatesPmlAsync(new[] { template });
            var fileName = $"{AppConstants.ExportPrefixes.Template}_{templateNom}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pmlBytes));
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de l'export: {ex.Message}", "error");
        }
    }

    private void OpenInInventaire(int id)
    {
        Navigation.NavigateTo($"/inventaire?templateId={id}");
    }

    private async Task ExportTemplate(int id)
    {
        var template = await PersonnageService.GetTemplateAsync(id);
        if (template is null)
        {
            toastRef?.Show("Template introuvable", "error");
            return;
        }
        var pmlBytes = await PmlImportService.ExporterTemplatesPmlAsync(new[] { template });
        var fileName = $"{AppConstants.ExportPrefixes.Template}_{template.Nom}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";
        await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pmlBytes));
        toastRef?.Show($"Export de '{template.Nom}' effectué", "success");
    }

    private async Task DeleteTemplate(int id)
    {
        var ok = await JSRuntime.InvokeAsync<bool>("confirm", "Supprimer ce template ?");
        if (!ok) return;
        var success = await PersonnageService.DeleteTemplateAsync(id);
        if (success)
        {
            templates = PersonnageService.GetAllTemplates().ToList();
            StateHasChanged();
        }
        else
        {
            toastRef?.Show("Suppression échouée", "error");
        }
    }

    private async Task DuplicateTemplate(int id)
    {
        var template = await PersonnageService.GetTemplateAsync(id);
        if (template is null)
        {
            await JSRuntime.InvokeVoidAsync("alert", "Template introuvable");
            return;
        }

        var ids = template.GetPersonnageIds();
        var copyName = template.Nom + " (copie)";
        var newTemplate = await PersonnageService.CreateTemplateAsync(copyName, template.Description, ids);
        templates = PersonnageService.GetAllTemplates().ToList();
        toastRef?.Show($"Template '{newTemplate.Nom}' créé.", "success");
    }

    private void StartRename(Template t)
    {
        editingTemplateId = t.Id;
        editingName = t.Nom;
    }

    private void CancelRename()
    {
        editingTemplateId = null;
        editingName = string.Empty;
    }

    private async Task SaveRename(Template t)
    {
        if (string.IsNullOrWhiteSpace(editingName))
        {
            toastRef?.Show("Le nom ne peut pas être vide", "warning");
            return;
        }
        var ids = t.GetPersonnageIds();
        var ok = await PersonnageService.UpdateTemplateAsync(t.Id, editingName, t.Description, ids);
        if (ok)
        {
            toastRef?.Show("Template renommé", "success");
            templates = PersonnageService.GetAllTemplates().ToList();
            CancelRename();
        }
        else
        {
            toastRef?.Show("Échec du renommage", "error");
        }
    }
}
