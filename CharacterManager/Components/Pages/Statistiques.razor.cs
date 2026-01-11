using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CharacterManager.Components.Pages;

public partial class Statistiques : IAsyncDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    private IJSObjectReference? chartModule;

    protected override void OnInitialized()
    {
        // Charger les mercenaires
        mercenaires = PersonnageService.GetMercenaires(false);

        if (mercenaires != null && mercenaires.Any())
        {
            // Calculer les statistiques
            statsTypeAttaque = CalculerStatistiquesParTypeAttaque(mercenaires);
            statsFaction = CalculerStatistiquesParFaction(mercenaires);
            statsRang = CalculerStatistiquesParRang(mercenaires);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && mercenaires != null && mercenaires.Any())
        {
            try
            {
                // Charger le module Chart.js
                chartModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "/js/charts.js");

                // Créer le graphique des types d'attaque
                var labelsTypeAttaque = statsTypeAttaque.Keys.Select(k => GetTypeAttaqueLabel(k)).ToArray();
                var dataTypeAttaque = statsTypeAttaque.Values.ToArray();
                var colorsTypeAttaque = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0" };

                await chartModule.InvokeVoidAsync("createPieChart", 
                    "chartTypeAttaque", 
                    labelsTypeAttaque, 
                    dataTypeAttaque,
                    colorsTypeAttaque);

                // Créer le graphique des factions
                var labelsFaction = statsFaction.Keys.Select(k => GetFactionLabel(k)).ToArray();
                var dataFaction = statsFaction.Values.ToArray();
                var colorsFaction = new[] { "#9966FF", "#FF9F40", "#4BC0C0" };

                await chartModule.InvokeVoidAsync("createPieChart", 
                    "chartFaction", 
                    labelsFaction, 
                    dataFaction,
                    colorsFaction);

                // Créer le graphique des rangs
                var labelsRang = statsRang.Keys.OrderByDescending(k => k).Select(k => $"Rang {k}").ToArray();
                var dataRang = statsRang.OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
                var colorsRang = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF" };

                await chartModule.InvokeVoidAsync("createPieChart", 
                    "chartRang", 
                    labelsRang, 
                    dataRang,
                    colorsRang);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création des graphiques: {ex.Message}");
            }
        }
    }

    private static Dictionary<TypeAttaque, int> CalculerStatistiquesParTypeAttaque(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .Where(m => m.TypeAttaque != TypeAttaque.Inconnu)
            .GroupBy(m => m.TypeAttaque)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<Faction, int> CalculerStatistiquesParFaction(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .Where(m => m.Faction != Faction.Inconnu)
            .GroupBy(m => m.Faction)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<int, int> CalculerStatistiquesParRang(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .Where(m => m.Rang > 0)
            .GroupBy(m => m.Rang)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    internal  string GetTypeAttaqueLabel(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => this.LocalizationService.GetKeyValue("home.attackType.melee"),
        TypeAttaque.Distance => this.LocalizationService.GetKeyValue("home.attackType.ranged"),
        TypeAttaque.Androide => this.LocalizationService.GetKeyValue("home.attackType.android"),
        TypeAttaque.Commandant => this.LocalizationService.GetKeyValue("home.attackType.commander"),
        _ => this.LocalizationService.GetKeyValue("home.attackType.unknown")
    };

    internal  string GetFactionLabel(Faction faction) => faction switch
    {
        Faction.Syndicat => this.LocalizationService.GetKeyValue("home.faction.syndicat"),
        Faction.Pacificateurs => this.LocalizationService.GetKeyValue("home.faction.pacificateurs"),
        Faction.HommesLibres => this.LocalizationService.GetKeyValue("home.faction.hommesLibres"),
        _ => this.LocalizationService.GetKeyValue("home.faction.inconnu")
    };

    public async ValueTask DisposeAsync()
    {
        if (chartModule != null)
        {
            try
            {
                await chartModule.DisposeAsync();
            }
            catch
            {
                // Ignorer les erreurs de disposal
            }
        }
    }
}
