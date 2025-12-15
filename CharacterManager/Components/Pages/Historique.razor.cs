namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;

public partial class Historique
{
    [Inject]
    public HistoriqueEscouadeService HistoriqueService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    private List<HistoriqueEscouade>? historiques;
    private DateTime dateDebut = DateTime.Today.AddMonths(-1);
    private DateTime dateFin = DateTime.Today.AddDays(1);
    private int nbMercenairesMax = 0;
    private int nbAndroidsMax = 0;

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

    private async Task RinitialiserFiltres()
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
}
