namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;
using CharacterManager.Server.Constants;

public partial class Historique
{
    private bool showCreerClassement = false;

    [Inject]
    public HistoriqueEscouadeService HistoriqueService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    private List<HistoriqueEscouade>? historiques;
    private DateTime dateDebut = DateTime.Today.AddMonths(-1);
    private DateTime dateFin = DateTime.Today.AddDays(1);
    private int nbMercenairesMax = 0;
    private int nbAndroidsMax = 0;
    private InputFile? inputFileRef;

    private Personnage Commandant => historique.Commandant ?? new Personnage { Nom = "Aucun", Type = Server.Models.TypePersonnage.Commandant };

    private HistoriqueClassement historique = new();

    private void ShowCreerClassementModal()
    {
        showCreerClassement = true;
        StateHasChanged();
    }
    protected override async Task OnInitializedAsync()
    {
        await ChargerHistorique();
    }

    private async Task ChargerHistorique()
    {
        historiques = await HistoriqueService.GetHistoriqueAsync();
        CalculerMaxPersonnages();
    }

    private void CalculerMaxPersonnages()
    {
        if (historiques == null || historiques.Count == 0)
            return;

        nbMercenairesMax = 0;
        nbAndroidsMax = 0;

        foreach (var historique in historiques)
        {
            var donnees = HistoriqueService.DeserializerEscouade(historique.DonneesEscouadeJson);
            if (donnees != null)
            {
                nbMercenairesMax = Math.Max(nbMercenairesMax, donnees.Mercenaires.Count);
                nbAndroidsMax = Math.Max(nbAndroidsMax, donnees.Androides.Count);
            }
        }
    }

    private async Task FiltrerHistorique()
    {
        historiques = await HistoriqueService.GetHistoriqueAsync(dateDebut, dateFin.AddDays(1));
        CalculerMaxPersonnages();
    }

    private async Task ReinitialiserFiltres()
    {
        dateDebut = DateTime.Today.AddMonths(-1);
        dateFin = DateTime.Today.AddDays(1);
        await ChargerHistorique();
    }

    private async Task SupprimerEnregistrement(int id)
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Êtes-vous sûr de vouloir supprimer cet enregistrement?"))
        {
            await HistoriqueService.SupprimerEnregistrementAsync(id);
            await ChargerHistorique();
        }
    }

    private async Task ViderHistorique()
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Êtes-vous CERTAIN de vouloir vider tout l'historique? Cette action est irréversible."))
        {
            await HistoriqueService.ViderHistoriqueAsync();
            await ChargerHistorique();
        }
    }

    private string GetImageUrl(string nomPersonnage)
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
            var xmlBytes = await HistoriqueService.ExporterHistoriqueXmlAsync();
            var fileName = $"{AppConstants.ExportPrefixes.HistoriqueClassements}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Xml}";
            var base64 = Convert.ToBase64String(xmlBytes);
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
        try
        {
            var file = e.File;
            if (file != null && (file.Name.EndsWith(AppConstants.FileExtensions.Xml, StringComparison.OrdinalIgnoreCase)
                              || file.Name.EndsWith(AppConstants.FileExtensions.Pml, StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var count = await HistoriqueService.ImporterHistoriqueAsync(stream);
                await ChargerHistorique();
                await JSRuntime.InvokeVoidAsync("alert", $"{count} enregistrement(s) importé(s) avec succès.");
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", "Veuillez sélectionner un fichier XML ou PML.");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Erreur lors de l'import: {ex.Message}");
        }
    }

    private async Task AjouterClassement(HistoriqueClassement nouveauHistorique)
    {
        historique.Classements.AddRange(nouveauHistorique.Classements);
        showCreerClassement = false;
        await InvokeAsync(StateHasChanged);
    }
}
