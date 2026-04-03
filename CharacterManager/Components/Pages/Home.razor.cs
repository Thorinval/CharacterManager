namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using CharacterManager.Server.Services;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

public partial class Home : IAsyncDisposable
{
    [Inject]
    public IPersonnageService PersonnageService { get; set; } = null!;

    [Inject]
    public IAdultModeNotificationService AdultModeNotification { get; set; } = null!;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    [Inject]
    public IProfileService ProfileService { get; set; } = null!;

    [Inject]
    public IHttpContextAccessor HttpContextAccessor { get; set; } = null!;

    [Inject]
    public IPmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public IPmlExportService PmlExportService { get; set; } = null!;

    [Inject]
    public IHistoriqueLigueService HistoriqueLigueService { get; set; } = null!;

    [Inject]
    public IHistoriqueClassementService HistoriqueClassementService { get; set; } = null!;

    [Inject]
    public ICapaciteService CapaciteService { get; set; } = null!;

    internal bool isAdultModeEnabled;
    internal bool showPmlImportAlert = false;
    internal string? importError = null;
    internal int puissanceEscouade;
    internal int puissanceMeilleureEscouade;
    internal int puissanceLucieEscouade;
    internal string highestLigueLabel = "-";
    internal Dictionary<Faction, int> mercenairesParFaction = new();
    internal Dictionary<TypeAttaque, int> mercenairesParTypeAttaque = new();
    internal Dictionary<Faction, int> inventaireFactions = new();
    internal Dictionary<TypeAttaque, int> inventaireAttackTypes = new();
    internal Dictionary<Faction, int> bestSquadFactions = new();
    internal Dictionary<TypeAttaque, int> bestSquadAttackTypes = new();
    internal List<(string Nom, int Puissance, int Niveau)> luciePieces = new();
    internal int lucieAffection;
    internal int luciePiecesMaxPower;
    internal (int Nutaku, int Top150, int France)? lastClassementSummary;
    internal int maxScore;
    internal DateTime? lastImportDate;
    internal DateTime? lastExportDate;
    internal string? lastImportFileName;
    internal (int Commandants, int Mercenaires, int Androides) inventaireCounts;
    internal int capacitesCount;

    protected override async Task OnInitializedAsync()
    {
        AdultModeNotification.Subscribe(OnAdultModeChanged);
        isAdultModeEnabled = await GetCurrentAdultModeAsync();
        puissanceEscouade = PersonnageService.GetPuissanceEscouade();
        puissanceMeilleureEscouade = PersonnageService.GetPuissanceMaxEscouade();
        puissanceLucieEscouade = PersonnageService.GetPuissanceLucieEscouade();

        var mercenairesSelectionnes = (await PersonnageService.GetMercenairesAsync(true)).ToList();
        mercenairesParFaction = CalculerMercenairesParFaction(mercenairesSelectionnes);
        mercenairesParTypeAttaque = CalculerMercenairesParTypeAttaque(mercenairesSelectionnes);

        var mercenairesInventaire = (await PersonnageService.GetMercenairesAsync(false)).ToList();
        inventaireFactions = CalculerMercenairesParFaction(mercenairesInventaire);
        inventaireAttackTypes = CalculerMercenairesParTypeAttaque(mercenairesInventaire);

        inventaireCounts = PersonnageService.GetInventoryCounts();

        var highestLigue = await HistoriqueLigueService.GetHighestLeagueAsync();
        highestLigueLabel = FormatLigueLabel(highestLigue);

        lastImportDate = await PmlImportService.GetLastImportedDateAsync();
        lastExportDate = await PmlExportService.GetLastExportDate();
        lastImportFileName = await PmlImportService.GetLastImportedFileName();

        // Composition de la meilleure escouade (top mercenaires/androides/commandant)
        var topMercenaires = (await PersonnageService.GetTopMercenairesAsync()).ToList();
        var topAndroides = (await PersonnageService.GetTopAndroidesAsync()).ToList();
        var topCommandant = await PersonnageService.GetTopCommandantAsync();
        bestSquadFactions = CalculerCompositionParFaction(topMercenaires, topAndroides, topCommandant);
        bestSquadAttackTypes = CalculerCompositionParTypeAttaque(topMercenaires, topAndroides, topCommandant);

        // Données Maison Lucie (pièces + affection)
        var lucieHouse = await DbContext.LucieHouses
            .Include(static h => h.Pieces)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        lucieAffection = lucieHouse?.Affection ?? 0;
        luciePieces = lucieHouse?.Pieces
            .OrderByDescending(static p => p.Puissance)
            .Select(static p => (p.Nom, p.Puissance, p.Niveau))
            .ToList() ?? new();
        luciePiecesMaxPower = PersonnageService.GetPuissanceMaxLucieEscouade();

        // Dernier enregistrement de classement pour afficher les valeurs Nutaku / Top150 / France
        var lastClassement = (await HistoriqueClassementService.GetHistoriqueRecentAsync(1)).FirstOrDefault();
        if (lastClassement != null)
        {
            lastClassementSummary = (
                GetClassementValeur(lastClassement, TypeClassement.Nutaku),
                GetClassementValeur(lastClassement, TypeClassement.Top150),
                GetClassementValeur(lastClassement, TypeClassement.France)
            );
        }

        maxScore = await HistoriqueClassementService.GetMaxScoreAsync();

        // Charge du nombre de capacités
        capacitesCount = CapaciteService.GetCount();

        // Vérifie si la base est vide (aucun personnage, template, historique ou profil)
        bool dbIsEmpty = !await DbContext.Personnages.AnyAsync()
            && !await DbContext.Templates.AnyAsync()
            && !await DbContext.Profiles.AnyAsync();

        if (dbIsEmpty)
        {
            string[] possibleFiles = [
                Path.Combine("wwwroot", "config.pml")
            ];

            string? configFile = possibleFiles.FirstOrDefault(File.Exists);
            if (configFile != null)
            {
                try
                {
                    using var stream = File.OpenRead(configFile);
                    var result = await PmlImportService.ImportPmlAsync(stream, Path.GetFileName(configFile));
                    if (!result.IsSuccess)
                    {
                        importError = $"{LocalizationService.GetKeyValue("errors.importError")}: {result.Error}";
                        showPmlImportAlert = true;
                    }
                }
                catch (Exception ex)
                {
                    importError = $"{LocalizationService.GetKeyValue("errors.importError")}: {ex.Message}";
                    showPmlImportAlert = true;
                }
            }
            else
            {
                importError = LocalizationService.GetKeyValue("errors.configNotFound");
                showPmlImportAlert = true;
            }
        }
    }

    private void OnAdultModeChanged(bool isAdultModeEnabled)
    {
        this.isAdultModeEnabled = isAdultModeEnabled;
        
        // Only call StateHasChanged if the component is initialized (has a render handle)
        // This allows testing the business logic without Blazor's rendering infrastructure
        try
        {
            StateHasChanged();
        }
        catch (InvalidOperationException)
        {
            // Render handle not initialized - this is expected in unit tests
        }
    }

    private async Task<bool> GetCurrentAdultModeAsync()
    {
        var user = HttpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return await GetAdultModeForAuthenticatedUserAsync(user);
        }

        return await GetAdultModeFromSettingsAsync();
    }

    private async Task<bool> GetAdultModeForAuthenticatedUserAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        var username = user.Identity?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        try
        {
            var profile = await ProfileService.GetByUsernameAsync(username);
            if (profile != null)
            {
                return profile.AdultMode;
            }

            return await GetAdultModeFromSettingsAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> GetAdultModeFromSettingsAsync()
    {
        try
        {
            var settings = await DbContext.AppSettings.FirstOrDefaultAsync();
            return settings?.IsAdultModeEnabled ?? false;
        }
        catch
        {
            return false;
        }
    }

    internal string FormatLigueLabel(int? ligue)
    {
        if (!ligue.HasValue)
        {
            return this.LocalizationService.GetKeyValue("home.highestLeagueNone");
        }

        if (ligue.Value == 50)
        {
            return this.LocalizationService.GetKeyValue("home.eliteTop50");
        }

        return $"{this.LocalizationService.GetKeyValue("leagueHistory.table.league")} {ligue.Value}";
    }

    internal static Dictionary<Faction, int> CalculerMercenairesParFaction(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .GroupBy(m => m.Faction)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    internal static Dictionary<TypeAttaque, int> CalculerMercenairesParTypeAttaque(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .GroupBy(m => m.TypeAttaque)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    internal string GetFactionLabel(Faction faction) => faction switch
    {
        Faction.Syndicat => this.LocalizationService.GetKeyValue("home.faction.syndicat"),
        Faction.Pacificateurs => this.LocalizationService.GetKeyValue("home.faction.pacificateurs"),
        Faction.HommesLibres => this.LocalizationService.GetKeyValue("home.faction.hommesLibres"),
        _ => this.LocalizationService.GetKeyValue("home.faction.inconnu")
    };

    internal string GetTypeAttaqueLabel(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => this.LocalizationService.GetKeyValue("home.attackType.melee"),
        TypeAttaque.Distance => this.LocalizationService.GetKeyValue("home.attackType.distance"),
        TypeAttaque.Androide => this.LocalizationService.GetKeyValue("home.attackType.android"),
        TypeAttaque.Commandant => this.LocalizationService.GetKeyValue("home.attackType.commander"),
        _ => this.LocalizationService.GetKeyValue("home.attackType.unknown")
    };

    internal static string GetFactionShapeClass(Faction faction) => faction switch
    {
        Faction.Syndicat => "shape-triangle",
        Faction.Pacificateurs => "shape-square",
        Faction.HommesLibres => "shape-circle",
        _ => string.Empty
    };

    internal static string GetFactionColorClass(Faction faction) => faction switch
    {
        Faction.Syndicat => "faction-syndicat",
        Faction.Pacificateurs => "faction-pacificateurs",
        Faction.HommesLibres => "faction-hommeslibres",
        _ => string.Empty
    };

    internal static string GetTypeAttaqueIcon(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => "bi-hand-thumbs-up-fill",
        TypeAttaque.Distance => "bi-bullseye",
        TypeAttaque.Androide => "bi-cpu",
        TypeAttaque.Commandant => "bi-star-fill",
        _ => "bi-question-circle"
    };

    internal static Dictionary<Faction, int> CalculerCompositionParFaction(
        IEnumerable<Personnage> mercenaires,
        IEnumerable<Personnage> androides,
        Personnage? commandant)
    {
        var all = new List<Personnage>();
        if (commandant != null)
        {
            all.Add(commandant);
        }
        all.AddRange(mercenaires);
        all.AddRange(androides);

        return all
            .GroupBy(static p => p.Faction)
            .ToDictionary(static g => g.Key, static g => g.Count());
    }

    internal static Dictionary<TypeAttaque, int> CalculerCompositionParTypeAttaque(
        IEnumerable<Personnage> mercenaires,
        IEnumerable<Personnage> androides,
        Personnage? commandant)
    {
        var all = new List<Personnage>();
        if (commandant != null)
        {
            all.Add(commandant);
        }
        all.AddRange(mercenaires);
        all.AddRange(androides);

        return all
            .GroupBy(static p => p.TypeAttaque)
            .ToDictionary(static g => g.Key, static g => g.Count());
    }

    internal static int GetClassementValeur(HistoriqueClassement historique, TypeClassement type)
    {
        return historique.Classements.FirstOrDefault(c => c.Type == type)?.Valeur ?? 0;
    }

    internal static string FormatClassementValeur(int valeur)
    {
        return valeur > 0 ? valeur.ToString() : "-";
    }

    internal string FormatDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        var localDate = value.Value.ToLocalTime();
        var language = this.LocalizationService.GetCurrentLanguage()?.ToLowerInvariant() ?? "fr";
        var isFrench = language.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
        var culture = isFrench ? new CultureInfo("fr-FR") : CultureInfo.InvariantCulture;
        var format = isFrench ? "dd/MM/yyyy HH:mm" : "yyyy-MM-dd HH:mm";

        return localDate.ToString(format, culture);
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        AdultModeNotification.Unsubscribe(OnAdultModeChanged);
        return ValueTask.CompletedTask;
    }
}


