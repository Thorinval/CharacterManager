namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
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

    protected override void OnInitialized()
    {
        templates = PersonnageService.GetAllTemplates().ToList();
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
