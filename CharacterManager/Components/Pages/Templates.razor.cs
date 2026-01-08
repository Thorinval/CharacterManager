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
    public PmlExportService PmlExportService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    private const string success = "success";
    private const string warning = "warning";
    private const string error = "error";
    private const string alert = "alert";

    internal List<Template> templates = new();
    internal Toast? toastRef;
    internal int? editingTemplateId = null;
    private string editingName = string.Empty;

    // Template editor state
    internal bool showTemplateEditor = false;
    private string templateNom = string.Empty;
    private string templateDescription = string.Empty;
    internal List<Personnage?> templatePersonnages = [];
    private List<int> templateSelectedIds = [];
    private int selectedTemplateId = 0;
    private string searchTerm = string.Empty;
    internal int? currentlyDraggedId;
    private List<Personnage> personnagesFiltres = [];

    internal IEnumerable<IGrouping<string, Personnage>> GroupedPersonnages
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

    internal void OpenTemplateEditor()
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

    internal void HandleSearchInput(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    internal void ClearSearch()
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

    internal void HandleDragStart(DragEventArgs e, Personnage personnage)
    {
        currentlyDraggedId = personnage.Id;
    }

    internal void HandleInvalidDrop()
    {
        currentlyDraggedId = null;
    }

    internal async Task SaveTemplate()
    {
        if (string.IsNullOrEmpty(templateNom) || templateSelectedIds.Count == 0)
            return;

        try
        {
            await PersonnageService.CreateTemplateAsync(
                templateNom,
                templateDescription,
                templateSelectedIds
            );
            toastRef?.Show($"Template '{templateNom}' créé avec succès", error);
            CancelTemplateCreation();
            templates = PersonnageService.GetAllTemplates().ToList();
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de la création du template: {ex.Message}", error);
        }
    }

    internal async Task LoadSelectedTemplate()
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
            toastRef?.Show($"Erreur lors du chargement du template: {ex.Message}", error);
        }
    }

    internal async Task ExportTemplateAsPml()
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

            var pmlBytes = await PmlExportService.ExporterTemplatesPmlAsync(new[] { template });
            var fileName = $"{AppConstants.ExportPrefixes.Template}_{templateNom}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pmlBytes));
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de l'export: {ex.Message}", error);
        }
    }

    internal void OpenInInventaire(int id)
    {
        Navigation.NavigateTo($"/inventaire?templateId={id}");
    }

    internal async Task ExportTemplate(int id)
    {
        var template = await PersonnageService.GetTemplateAsync(id);
        if (template is null)
        {
            toastRef?.Show("Template introuvable", error);
            return;
        }
        var pmlBytes = await PmlExportService.ExporterTemplatesPmlAsync(new[] { template });
        var fileName = $"{AppConstants.ExportPrefixes.Template}_{template.Nom}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";
        await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pmlBytes));
        toastRef?.Show($"Export de '{template.Nom}' effectué", success);
    }

    internal async Task DeleteTemplate(int id)
    {
        var reponseOk = await JSRuntime.InvokeAsync<bool>("confirm", "Supprimer ce template ?");
        if (!reponseOk) return;
        var deleteOk = await PersonnageService.DeleteTemplateAsync(id);

        if (deleteOk)
        {
            templates = PersonnageService.GetAllTemplates().ToList();
            StateHasChanged();
        }
        else
        {
            toastRef?.Show("Suppression échouée", error);
        }
    }

    internal async Task DuplicateTemplate(int id)
    {
        var template = await PersonnageService.GetTemplateAsync(id);
        if (template is null)
        {
            await JSRuntime.InvokeVoidAsync(alert, "Template introuvable");
            return;
        }

        var ids = template.GetPersonnageIds();
        var copyName = template.Nom + " (copie)";
        var newTemplate = await PersonnageService.CreateTemplateAsync(copyName, template.Description, ids);
        templates = PersonnageService.GetAllTemplates().ToList();
        toastRef?.Show($"Template '{newTemplate.Nom}' créé.", success);
    }

    internal void StartRename(Template t)
    {
        editingTemplateId = t.Id;
        editingName = t.Nom;
    }

    private void CancelRename()
    {
        editingTemplateId = null;
        editingName = string.Empty;
    }

    internal async Task SaveRename(Template t)
    {
        if (string.IsNullOrWhiteSpace(editingName))
        {
            toastRef?.Show("Le nom ne peut pas être vide", warning);
            return;
        }
        var ids = t.GetPersonnageIds();
        var ok = await PersonnageService.UpdateTemplateAsync(t.Id, editingName, t.Description, ids);
        if (ok)
        {
            toastRef?.Show("Template renommé", success);
            templates = PersonnageService.GetAllTemplates().ToList();
            CancelRename();
        }
        else
        {
            toastRef?.Show("Échec du renommage", error);
        }
    }
}
