namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;
using CharacterManager.Server.Constants;
using CharacterManager.Components.Modal;
public partial class Classements
{
    [Inject]
    public IHistoriqueClassementService HistoriqueService { get; set; } = null!;

    [Inject]
    public IPmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public IPmlExportService PmlExportService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public IModalService ModalService { get; set; } = null!;

    [Inject]
    public IClientLocalizationService LocalizationService { get; set; } = null!;

    private const string Identifier = "alert";
    internal List<HistoriqueClassement>? historiques;
    private DateTime dateDebut = DateTime.Today.AddMonths(-1);
    private DateTime dateFin = DateTime.Today.AddDays(1);
    internal int nbMercenairesMax = 0;
    internal int nbAndroidsMax = 0;
    internal InputFile? inputFileRef;
    internal bool isImporting = false;

    internal PersonnageClassement Commandant => historique.Commandant ?? new PersonnageClassement { Nom = "Aucun", Type = Server.Models.TypePersonnage.Commandant };

    internal HistoriqueClassement historique = new();

    protected override async Task OnInitializedAsync()
    {
        await ChargerHistorique();
    }

    private async Task ChargerHistorique()
    {
        historiques = await HistoriqueService.GetHistoriqueAsync();
        MettreAJourTaillesColonnes();
    }

    internal async Task FiltrerHistorique()
    {
        historiques = await HistoriqueService.GetHistoriqueAsync(dateDebut, dateFin.AddDays(1));
        MettreAJourTaillesColonnes();
    }

    internal async Task ReinitialiserFiltres()
    {
        dateDebut = DateTime.Today.AddMonths(-1);
        dateFin = DateTime.Today.AddDays(1);
        await ChargerHistorique();
    }

    private void MettreAJourTaillesColonnes()
    {
        nbMercenairesMax = historiques?.Any() == true
            ? historiques.Max(h => h.Mercenaires?.Count ?? 0)
            : 0;

        nbAndroidsMax = historiques?.Any() == true
            ? historiques.Max(h => h.Androides?.Count ?? 0)
            : 0;
    }

    internal async Task SupprimerEnregistrement(int id)
    {
        await HistoriqueService.SupprimerEnregistrementAsync(id);
        await ChargerHistorique();
    }

    internal async Task ViderHistorique()
    {
        var confirmationMessage = LocalizationService.GetKeyValue("ui.confirmations.confirmClearRankings");
        if (await JSRuntime.InvokeAsync<bool>("confirm", confirmationMessage))
        {
            await HistoriqueService.ViderHistoriqueAsync();
            await ChargerHistorique();
        }
    }

    internal static string GetImageUrl(string nomPersonnage)
    {
        // Retourner l'image par défaut si le nom est vide ou null
        if (string.IsNullOrWhiteSpace(nomPersonnage))
        {
            return AppConstants.Paths.DefaultPortrait;
        }

        // Utilise le helper qui gère le dossier spécifique du personnage
        return PersonnageImageUrlHelper.GetImageSmallPortraitUrl(nomPersonnage);
    }

    // ...removed duplicate RenderStars, use TemplateEscouade.GetRankStars instead

    internal async Task ExporterHistorique()
    {
        try
        {
            var options = PmlExportOptions.FromBooleans(
                exportInventory: false,
                exportTemplates: false,
                exportBestSquad: false,
                exportHistories: true,
                exportLeagueHistory: false);

            var bytes = await PmlExportService.ExportPmlAsync(options);
            var fileName = $"{ImportExportConstants.ExportPrefixes.HistoriqueClassements}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";
            var base64 = Convert.ToBase64String(bytes);
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, base64);
        }
        catch (Exception ex)
        {
            var errorMessage = $"{LocalizationService.GetKeyValue("errors.exportError")}: {ex.Message}";
            await JSRuntime.InvokeVoidAsync(Identifier, errorMessage);
        }
    }

    internal async Task ImporterHistorique()
    {
        // Déclenche le sélecteur de fichier XML caché
        await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('historiqueFileInput')?.click();");
    }

    internal async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null)
        {
            return;
        }

        var isSupported = file.Name.EndsWith(AppConstants.FileExtensions.Pml, StringComparison.OrdinalIgnoreCase);

        if (!isSupported)
        {
            await JSRuntime.InvokeVoidAsync(Identifier, LocalizationService.GetKeyValue("errors.importFormatNotSupported"));
            return;
        }

        isImporting = true;
        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var result = await PmlImportService.ImportPmlAsync(
                stream,
                file.Name,
                importInventory: false,
                importTemplates: false,
                importBestSquad: false,
                importHistories: true,
                importLeagueHistory: false);

            var importMessage = result.SuccessCount > 0
                ? $"{result.SuccessCount} enregistrement(s) importé(s) avec succès."
                : "Aucun enregistrement importé.";

            if (!string.IsNullOrEmpty(result.Error))
            {
                importMessage += $"\nErreur: {result.Error}";
            }

            if (result.Errors.Count > 0)
            {
                var preview = string.Join("\n", result.Errors.Take(3));
                importMessage += $"\nDétails (aperçu):\n{preview}";
            }

            await JSRuntime.InvokeVoidAsync(Identifier, importMessage);
            await ChargerHistorique();
        }
        catch (Exception ex)
        {
            var errorMessage = $"{LocalizationService.GetKeyValue("errors.importError")}: {ex.Message}";
            await JSRuntime.InvokeVoidAsync(Identifier, errorMessage);
        }
        finally
        {
            isImporting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    internal Task ShowCreerClassementModalAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            { "OnSaved", EventCallback.Factory.Create(this, ChargerHistorique) }
        };

        ModalService.Open<CreerClassementModal>(parameters, ModalSize.XL);
        return Task.CompletedTask;
    }

    internal void EditEnregistrement(HistoriqueClassement historique)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Existing", historique },
            { "OnSaved", EventCallback.Factory.Create(this, ChargerHistorique) }
        };

        ModalService.Open<CreerClassementModal>(parameters, ModalSize.XL);
    }
}


