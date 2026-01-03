namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;
using CharacterManager.Server.Constants;
using CharacterManager.Components.Modal;
public partial class Historique
{
    [Inject]
    public HistoriqueClassementService HistoriqueService { get; set; } = null!;

    [Inject]
    public PmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public IModalService ModalService { get; set; } = null!;

    private List<HistoriqueClassement>? historiques;
    private DateTime dateDebut = DateTime.Today.AddMonths(-1);
    private DateTime dateFin = DateTime.Today.AddDays(1);
    private int nbMercenairesMax = 0;
    private int nbAndroidsMax = 0;
    private InputFile? inputFileRef;

    private Personnage Commandant => historique.Commandant ?? new Personnage { Nom = "Aucun", Type = Server.Models.TypePersonnage.Commandant };

    private HistoriqueClassement historique = new();

    protected override async Task OnInitializedAsync()
    {
        await ChargerHistorique();
    }

    private async Task ChargerHistorique()
    {
        historiques = await HistoriqueService.GetHistoriqueAsync();
        MettreAJourTaillesColonnes();
    }

    private async Task FiltrerHistorique()
    {
        historiques = await HistoriqueService.GetHistoriqueAsync(dateDebut, dateFin.AddDays(1));
        MettreAJourTaillesColonnes();
    }

    private async Task ReinitialiserFiltres()
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

    private async Task SupprimerEnregistrement(int id)
    {
        await HistoriqueService.SupprimerEnregistrementAsync(id);
        await ChargerHistorique();
    }

    private async Task ViderHistorique()
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Êtes-vous CERTAIN de vouloir vider tout l'historique? Cette action est irréversible."))
        {
            await HistoriqueService.ViderHistoriqueAsync();
            await ChargerHistorique();
        }
    }

    private static string GetImageUrl(string nomPersonnage)
    {
        // Normalize the name: remove spaces, convert to lowercase
        var normalized = nomPersonnage.ToLower().Replace(" ", "_").Replace("'", "");
        return $"{AppConstants.Paths.ImagesPersonnages}/{normalized}{AppConstants.ImageSuffixes.SmallPortrait}{AppConstants.FileExtensions.Png}";
    }

    // ...removed duplicate RenderStars, use TemplateEscouade.GetRankStars instead

    private async Task ExporterHistorique()
    {
        try
        {
            var options = PmlExportOptions.FromBooleans(
                exportInventory: false,
                exportTemplates: false,
                exportBestSquad: false,
                exportHistories: true,
                exportLeagueHistory: false);

            var bytes = await PmlImportService.ExportPmlAsync(options);
            var fileName = $"{AppConstants.ExportPrefixes.HistoriqueClassements}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";
            var base64 = Convert.ToBase64String(bytes);
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, base64);
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Erreur lors de l'export: {ex.Message}");
        }
    }

    private async Task ImporterHistorique()
    {
        // Déclenche le sélecteur de fichier XML caché
        await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('historiqueFileInput')?.click();");
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null)
        {
            return;
        }

        var isSupported = file.Name.EndsWith(AppConstants.FileExtensions.Pml, StringComparison.OrdinalIgnoreCase);

        if (!isSupported)
        {
            await JSRuntime.InvokeVoidAsync("alert", "Veuillez sélectionner un fichier PML.");
            return;
        }

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

            await JSRuntime.InvokeVoidAsync("alert", importMessage);
            await ChargerHistorique();
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Erreur lors de l'import: {ex.Message}");
        }
    }

    private Task ShowCreerClassementModalAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            { "OnSaved", EventCallback.Factory.Create(this, ChargerHistorique) }
        };

        ModalService.Open<CreerClassementModal>(parameters, ModalSize.XL);
        return Task.CompletedTask;
    }

    private void EditEnregistrement(HistoriqueClassement historique)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Existing", historique },
            { "OnSaved", EventCallback.Factory.Create(this, ChargerHistorique) }
        };

        ModalService.Open<CreerClassementModal>(parameters, ModalSize.XL);
    }
}
