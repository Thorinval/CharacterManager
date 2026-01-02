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

public partial class Home : IAsyncDisposable
{
    [Inject]
    public PersonnageImageConfigService ImageConfigService { get; set; } = null!;

    [Inject]
    public PersonnageService PersonnageService { get; set; } = null!;

    [Inject]
    public AdultModeNotificationService AdultModeNotification { get; set; } = null!;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    [Inject]
    public ProfileService ProfileService { get; set; } = null!;

    [Inject]
    public IHttpContextAccessor HttpContextAccessor { get; set; } = null!;

    [Inject]
    public PmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public HistoriqueLigueService HistoriqueLigueService { get; set; } = null!;

    [Inject]
    public HistoriqueClassementService HistoriqueClassementService { get; set; } = null!;

    [Inject]
    public CapaciteService CapaciteService { get; set; } = null!;

    private string homeImageUrl = AppConstants.Paths.HomeDefaultBackground;
    private bool isAdultModeEnabled;
    private bool showPmlImportAlert = false;
    private string? importError = null;
    private int puissanceEscouade;
    private int puissanceMeilleureEscouade;
    private int puissanceLucieEscouade;
    private string highestLigueLabel = "-";
    private Dictionary<Faction, int> mercenairesParFaction = new();
    private Dictionary<TypeAttaque, int> mercenairesParTypeAttaque = new();
    private Dictionary<Faction, int> inventaireFactions = new();
    private Dictionary<TypeAttaque, int> inventaireAttackTypes = new();
    private Dictionary<Faction, int> bestSquadFactions = new();
    private Dictionary<TypeAttaque, int> bestSquadAttackTypes = new();
    private List<(string Nom, int Puissance, int Niveau)> luciePieces = new();
    private int lucieAffection;
    private int luciePiecesMaxPower;
    private (int Nutaku, int Top150, int France)? lastClassementSummary;
    private DateTime? lastImportDate;
    private DateTime? lastExportDate;
    private string? lastImportFileName;
    private (int Commandants, int Mercenaires, int Androides) inventaireCounts;
    private int capacitesCount;

    protected override async Task OnInitializedAsync()
    {
        AdultModeNotification.Subscribe(OnAdultModeChanged);
        isAdultModeEnabled = await GetCurrentAdultModeAsync();
        await UpdateHomeImageAsync(isAdultModeEnabled);
        puissanceEscouade = PersonnageService.GetPuissanceEscouade();
        puissanceMeilleureEscouade = PersonnageService.GetPuissanceMaxEscouade();
        puissanceLucieEscouade = PersonnageService.GetPuissanceLucieEscouade();

        var mercenairesSelectionnes = PersonnageService.GetMercenaires(true).ToList();
        mercenairesParFaction = CalculerMercenairesParFaction(mercenairesSelectionnes);
        mercenairesParTypeAttaque = CalculerMercenairesParTypeAttaque(mercenairesSelectionnes);

        var mercenairesInventaire = PersonnageService.GetMercenaires(false).ToList();
        inventaireFactions = CalculerMercenairesParFaction(mercenairesInventaire);
        inventaireAttackTypes = CalculerMercenairesParTypeAttaque(mercenairesInventaire);

        inventaireCounts = PersonnageService.GetInventoryCounts();

        var highestLigue = await HistoriqueLigueService.GetHighestLeagueAsync();
        highestLigueLabel = FormatLigueLabel(highestLigue);

        lastImportDate = await PmlImportService.GetLastImportedDateAsync();
        lastExportDate = await PmlImportService.GetLastExportDate();
        lastImportFileName = await PmlImportService.GetLastImportedFileName();

        // Composition de la meilleure escouade (top mercenaires/androides/commandant)
        var topMercenaires = PersonnageService.GetTopMercenaires().ToList();
        var topAndroides = PersonnageService.GetTopAndroides().ToList();
        var topCommandant = PersonnageService.GetTopCommandant();
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
                        importError = $"Erreur lors de l'import automatique du fichier de configuration : {result.Error}";
                        showPmlImportAlert = true;
                    }
                }
                catch (Exception ex)
                {
                    importError = $"Erreur lors de l'import automatique du fichier de configuration : {ex.Message}";
                    showPmlImportAlert = true;
                }
            }
            else
            {
                importError = "Aucun fichier de configuration PML/XML trouvé pour initialiser la base de données.";
                showPmlImportAlert = true;
            }
        }
    }

    private void OnAdultModeChanged(bool isAdultModeEnabled)
    {
        InvokeAsync(async () =>
        {
            this.isAdultModeEnabled = isAdultModeEnabled;
            await UpdateHomeImageAsync(isAdultModeEnabled);
            StateHasChanged();
        });
    }

    private async Task<bool> GetCurrentAdultModeAsync()
    {
        var isAdultMode = false;

        var user = HttpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var username = user.Identity?.Name ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    var profile = await ProfileService.GetByUsernameAsync(username);
                    if (profile != null)
                    {
                        isAdultMode = profile.AdultMode;
                    }
                    else
                    {
                        var settings = await DbContext.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
                        if (settings != null)
                        {
                            isAdultMode = settings.IsAdultModeEnabled;
                        }
                    }
                }
                catch
                {
                    // ignore and keep default
                }
            }
        }
        else
        {
            try
            {
                var settings = await DbContext.AppSettings.FirstOrDefaultAsync();
                if (settings != null)
                {
                    isAdultMode = settings.IsAdultModeEnabled;
                }
            }
            catch
            {
                // ignore and keep default
            }
        }

        return isAdultMode;
    }

    private string FormatLigueLabel(int? ligue)
    {
        if (!ligue.HasValue)
        {
            return LocalizationService.T("home.highestLeagueNone");
        }

        if (ligue.Value == 50)
        {
            return LocalizationService.T("home.eliteTop50");
        }

        return $"{LocalizationService.T("history.table.league")} {ligue.Value}";
    }

    private static Dictionary<Faction, int> CalculerMercenairesParFaction(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .GroupBy(m => m.Faction)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<TypeAttaque, int> CalculerMercenairesParTypeAttaque(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .GroupBy(m => m.TypeAttaque)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private string GetFactionLabel(Faction faction) => faction switch
    {
        Faction.Syndicat => LocalizationService.T("home.faction.syndicat"),
        Faction.Pacificateurs => LocalizationService.T("home.faction.pacificateurs"),
        Faction.HommesLibres => LocalizationService.T("home.faction.hommesLibres"),
        _ => LocalizationService.T("home.faction.inconnu")
    };

    private string GetTypeAttaqueLabel(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => LocalizationService.T("home.attackType.melee"),
        TypeAttaque.Distance => LocalizationService.T("home.attackType.distance"),
        TypeAttaque.Androide => LocalizationService.T("home.attackType.android"),
        TypeAttaque.Commandant => LocalizationService.T("home.attackType.commander"),
        _ => LocalizationService.T("home.attackType.unknown")
    };

    private static string GetFactionShapeClass(Faction faction) => faction switch
    {
        Faction.Syndicat => "shape-triangle",
        Faction.Pacificateurs => "shape-square",
        Faction.HommesLibres => "shape-circle",
        _ => string.Empty
    };

    private static string GetFactionColorClass(Faction faction) => faction switch
    {
        Faction.Syndicat => "faction-syndicat",
        Faction.Pacificateurs => "faction-pacificateurs",
        Faction.HommesLibres => "faction-hommeslibres",
        _ => string.Empty
    };

    private static string GetTypeAttaqueIcon(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => "bi-hand-thumbs-up-fill",
        TypeAttaque.Distance => "bi-bullseye",
        TypeAttaque.Androide => "bi-cpu",
        TypeAttaque.Commandant => "bi-star-fill",
        _ => "bi-question-circle"
    };

    private static Dictionary<Faction, int> CalculerCompositionParFaction(
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

    private static Dictionary<TypeAttaque, int> CalculerCompositionParTypeAttaque(
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

    private static int GetClassementValeur(HistoriqueClassement historique, TypeClassement type)
    {
        return historique.Classements.FirstOrDefault(c => c.Type == type)?.Valeur ?? 0;
    }

    private static string FormatClassementValeur(int valeur)
    {
        return valeur > 0 ? valeur.ToString() : "-";
    }

    private string FormatDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        var localDate = value.Value.ToLocalTime();
        var language = LocalizationService.GetCurrentLanguage()?.ToLowerInvariant() ?? "fr";
        var isFrench = language.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
        var culture = isFrench ? new CultureInfo("fr-FR") : CultureInfo.InvariantCulture;
        var format = isFrench ? "dd/MM/yyyy HH:mm" : "yyyy-MM-dd HH:mm";

        return localDate.ToString(format, culture);
    }

    private Task UpdateHomeImageAsync(bool isAdultMode)
    {
        const string adultImage = "/images/personnages/adult/accueil.png";
        var fallbackImage = AppConstants.Paths.HomeDefaultBackground;

        if (isAdultMode)
        {
            var candidate = ImageConfigService.GetDisplayPath(adultImage, isAdultMode);
            homeImageUrl = string.IsNullOrEmpty(candidate) ? fallbackImage : candidate;
        }
        else
        {
            homeImageUrl = fallbackImage;
        }

        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        AdultModeNotification.Unsubscribe(OnAdultModeChanged);
        return ValueTask.CompletedTask;
    }
}
