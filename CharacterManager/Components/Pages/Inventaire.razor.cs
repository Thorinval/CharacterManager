namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Components;
using CharacterManager.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Data.Common;

public partial class Inventaire : IAsyncDisposable
{
    [Inject]
    public PersonnageService PersonnageService { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public PmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public IModalService ModalService { get; set; } = null!;

    [Inject]
    public IWebHostEnvironment WebHostEnvironment { get; set; } = null!;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    private List<Personnage> personnages = [];
    private List<Personnage> personnagesFiltres = [];
    private LucieHouse? lucieHouse;
    private List<Piece> LuciePieces => lucieHouse?.Pieces ?? [];
    internal List<Piece> luciePiecesFiltres = [];
    internal bool showModal = false;
    private bool isEditing = false;
    private Personnage currentPersonnage = new();

    // Tri
    private string sortColumn = AppConstants.XmlElements.Puissance;
    private bool sortAscending = false;

    // Sélection multiple
    private HashSet<int> selectedPersonnages = [];
    internal bool showBulkEditModal = false;
    private string bulkEditProperty = "";
    private string bulkEditValue = "";

    // Filtre Commandants
    internal bool ShowOnlyCommandants = false;
    internal bool ShowOnlyMercenaires = false;
    internal bool ShowOnlyAndroides = false;
    internal bool ShowOnlyLucyRooms = false;

    internal void ToggleShowOnlyCommandants(ChangeEventArgs e)
    {
        ShowOnlyCommandants = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    internal void ToggleShowOnlyMercenaires(ChangeEventArgs e)
    {
        ShowOnlyMercenaires = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    internal void ToggleShowOnlyAndroides(ChangeEventArgs e)
    {
        ShowOnlyAndroides = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    internal void ToggleShowOnlyLucyRooms(ChangeEventArgs e)
    {
        ShowOnlyLucyRooms = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    internal bool SelectAllChecked
    {
        get => selectedPersonnages.Count == personnagesFiltres.Count && personnagesFiltres.Count > 0;
        set
        {
            if (value)
            {
                SelectAll();
            }
            else
            {
                selectedPersonnages.Clear();
            }
        }
    }

    internal IEnumerable<IGrouping<TypePersonnage, Personnage>> GroupedPersonnages =>
        personnagesFiltres.GroupBy(p => p.Type)
            .OrderBy(g => GetTypeOrder(g.Key));

    internal static int GetTypeOrder(TypePersonnage type) =>
        type switch
        {
            TypePersonnage.Commandant => 1,
            TypePersonnage.Mercenaire => 2,
            _ => 3
        };

    // Filtre
    private string searchTerm = "";

    // Mode d'affichage
    internal string viewMode = AppConstants.Defaults.ViewModeGrid;

    // JavaScript interop constants
    private const string JsAlert = "alert";

    protected override async Task OnInitializedAsync()
    {
        await LoadPersonnagesAsync();
        await LoadLucieHouseAsync();

        ApplyFiltersAndSorting();
        // Charger un template si présent dans l'URL
        var uri = new Uri(Navigation.Uri);
        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            // Template loading removed - now handled in Templates page
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    internal string GetViewModeClass(string mode)
    {
        return viewMode == mode ? "btn-primary" : "btn-outline-secondary";
    }

    internal async Task ChangeNiveauPiece(int pieceId, int delta)
    {
        var piece = lucieHouse?.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece != null)
        {
            int newValue = Math.Max(0, piece.Niveau + delta);
            await Task.Run(() => UpdatePieceField(pieceId, AppConstants.XmlElements.Niveau, newValue.ToString()));
        }
    }

    internal async Task ChangePuissance(int personnageId, int delta)
    {
        var personnage = personnagesFiltres.FirstOrDefault(p => p.Id == personnageId);
        if (personnage != null)
        {
            int newValue = Math.Max(0, personnage.Puissance + delta);
            await Task.Run(() => UpdatePersonnageField(personnageId, AppConstants.XmlElements.Puissance, newValue.ToString()));
        }
    }


    internal async Task ChangePuissancePiece(int pieceId, TypeBonus typeBonus, int delta)
    {
        var piece = lucieHouse?.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece != null)
        {
            int newValue = 0;
            string typepuissance = typeBonus == TypeBonus.Tactique ? AppConstants.XmlElements.PuissanceTactique : AppConstants.XmlElements.PuissanceStrategique;

            switch (typeBonus)
            {
                case TypeBonus.Tactique:
                    newValue = Math.Max(0, piece.AspectsTactiques.Puissance + delta);
                    break;
                case TypeBonus.Strategique:
                    newValue = Math.Max(0, piece.AspectsStrategiques.Puissance + delta);
                    break;
            }
            await UpdatePieceField(pieceId, typepuissance, newValue.ToString());
        }
    }

    internal string GetContainerClass()
    {
        return viewMode == AppConstants.Defaults.ViewModeGrid ? "personnages-grid" : "personnages-list";
    }

    internal const string ContainerClassCompact = "personnages-grid-compact";

    internal static string GetRarityClass(Rarete rarete)
    {
        return rarete switch
        {
            Rarete.SSR => "rarity-ssr",
            Rarete.SR => "rarity-sr",
            Rarete.R => "rarity-r",
            _ => ""
        };
    }

    private void UpdatePersonnageField(int personnageId, string field, string value)
    {
        var personnage = personnages.FirstOrDefault(p => p.Id == personnageId);
        if (personnage == null) return;

        try
        {
            switch (field)
            {
                case AppConstants.XmlElements.Niveau:
                    if (int.TryParse(value, out int niveau) && niveau >= 1 && niveau <= 200)
                    {
                        personnage.Niveau = niveau;
                    }
                    break;
                case AppConstants.XmlElements.Rang:
                    if (int.TryParse(value, out int rang) && rang >= 0 && rang <= 7)
                    {
                        personnage.Rang = rang;
                    }
                    break;
                case AppConstants.XmlElements.Puissance:
                    if (int.TryParse(value, out int puissance) && puissance >= 0)
                    {
                        personnage.Puissance = puissance;
                    }
                    break;
                case AppConstants.XmlElements.Selectionne:
                    if (bool.TryParse(value, out var selectionne))
                    {
                        personnage.Selectionne = selectionne;
                    }
                    break;
            }

            PersonnageService.Update(personnage);
            _ = InvokeAsync(async () =>
            {
                await LoadPersonnagesAsync();
                toastRef?.Show($"{field} mis à jour avec succès", "success");
            });
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de la mise à jour: {ex.Message}", "error");
        }
    }

    internal void OnSelectionneChanged(int personnageId, bool value)
    {
        UpdatePersonnageField(personnageId, AppConstants.XmlElements.Selectionne, value.ToString());
    }

    internal void UpdateRankFromStar(int personnageId, int clickedStar, int currentRank)
    {
        // Toggle down if clicking the currently selected star (allows rank 0)
        var newRank = clickedStar == currentRank ? Math.Max(0, clickedStar - 1) : clickedStar;
        UpdatePersonnageField(personnageId, "Rang", newRank.ToString());
    }

    private async Task LoadPersonnagesAsync()
    {
        personnages = [.. (await PersonnageService.GetAllAsync())];
        ApplyFiltersAndSorting();
    }

    public enum InventoryFilter
    {
        Tous,
        Commandants,
        Mercenaires,
        Androides,
        LucyRooms
    }

    private InventoryFilter SelectedFilter = InventoryFilter.Tous;

    internal record FilterOption(InventoryFilter Value, string LocalizationKey);

    internal readonly List<FilterOption> FilterOptions =
    [
        new(InventoryFilter.Tous, "inventory.showAll"),
        new(InventoryFilter.Commandants, "inventory.showOnlyCommandants"),
        new(InventoryFilter.Mercenaires, "inventory.showOnlyMercenaires"),
        new(InventoryFilter.Androides, "inventory.showOnlyAndroides"),
        new(InventoryFilter.LucyRooms, "inventory.showOnlyLucyRooms")
    ];

    internal void OnFilterClicked(InventoryFilter clicked)
    {
        if (SelectedFilter == clicked)
            return;

        SelectedFilter = clicked;
        ApplyFiltersAndSorting();
    }

    // 🔢 Compteur dynamique pour les badges
    internal int GetCount(InventoryFilter filter)
    {
        return filter switch
        {
            InventoryFilter.Tous => personnages.Count + LuciePieces.Count,
            InventoryFilter.Commandants => personnages.Count(p => p.Type == TypePersonnage.Commandant),
            InventoryFilter.Mercenaires => personnages.Count(p => p.Type == TypePersonnage.Mercenaire),
            InventoryFilter.Androides => personnages.Count(p => p.Type == TypePersonnage.Androide),
            InventoryFilter.LucyRooms => LuciePieces.Count,
            _ => 0
        };
    }

    internal string GetBadgeColor(InventoryFilter filter)
    {
        IEnumerable<Personnage> list = filter switch
        {
            InventoryFilter.Tous => personnages,
            InventoryFilter.Commandants => personnages.Where(p => p.Type == TypePersonnage.Commandant),
            InventoryFilter.Mercenaires => personnages.Where(p => p.Type == TypePersonnage.Mercenaire),
            InventoryFilter.Androides => personnages.Where(p => p.Type == TypePersonnage.Androide),
            InventoryFilter.LucyRooms => [], // LucyRooms has no Personnage
            _ => []
        };

        if (!list.Any())
            return "bg-secondary"; // vide → gris

        // Trouver la rareté dominante
        var dominant = list
            .GroupBy(p => p.Rarete)
            .OrderByDescending(g => g.Count())
            .First().Key;

        return dominant switch
        {
            Rarete.SSR => "bg-warning text-dark", // or
            Rarete.SR => "bg-purple text-white", // violet (custom)
            Rarete.R => "bg-primary",           // bleu
            Rarete.Inconnu => "bg-secondary",         // gris
            _ => "bg-secondary"
        };
    }
    private void ApplyFiltersAndSorting()
    {
        IEnumerable<Personnage> filtered = ApplySearchFilter(personnages);
        IEnumerable<Piece> filteredPieces = ApplySearchFilter(LuciePieces ?? []);

        filtered = ApplyTypeFilter(filtered);
        filteredPieces = ApplyTypeFilterToPieces(filteredPieces);

        personnagesFiltres = [.. filtered];
        luciePiecesFiltres = [.. filteredPieces];

        personnagesFiltres = ApplySorting(personnagesFiltres);
    }

    private IEnumerable<Personnage> ApplySearchFilter(IEnumerable<Personnage> source)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return source;

        return source.Where(p =>
            p.Nom.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || p.Rarete.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || p.Type.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || p.Role.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || p.Faction.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || p.TypeAttaque.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || p.Selectionne.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        );
    }

    private IEnumerable<Piece> ApplySearchFilter(IEnumerable<Piece> source)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return source;

        return source.Where(p =>
            p.Nom.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        );
    }

    private IEnumerable<Personnage> ApplyTypeFilter(IEnumerable<Personnage> source)
    {
        return SelectedFilter switch
        {
            InventoryFilter.Tous => source,
            InventoryFilter.Commandants => source.Where(p => p.Type == TypePersonnage.Commandant),
            InventoryFilter.Mercenaires => source.Where(p => p.Type == TypePersonnage.Mercenaire),
            InventoryFilter.Androides => source.Where(p => p.Type == TypePersonnage.Androide),
            InventoryFilter.LucyRooms => source.Where(p => p.Type == TypePersonnage.Inconnu),
            _ => source
        };
    }

    private IEnumerable<Piece> ApplyTypeFilterToPieces(IEnumerable<Piece> source)
    {
        return SelectedFilter switch
        {
            InventoryFilter.Tous => source,
            InventoryFilter.LucyRooms => source,
            _ => source.Where(p => false)
        };
    }

    private List<Personnage> ApplySorting(List<Personnage> source)
    {
        var typeOrder = new Dictionary<TypePersonnage, int>
        {
            { TypePersonnage.Commandant, 1 },
            { TypePersonnage.Mercenaire, 2 },
            { TypePersonnage.Androide, 3 }
        };

        return sortColumn switch
        {
            AppConstants.XmlElements.Puissance => SortByPuissance(source, typeOrder),
            AppConstants.XmlElements.Nom => SortByName(source, typeOrder),
            AppConstants.XmlElements.Rarete => SortByRarity(source, typeOrder),
            AppConstants.XmlElements.Niveau => SortByLevel(source, typeOrder),
            AppConstants.XmlElements.Type => SortByType(source, typeOrder),
            AppConstants.XmlElements.Rang => SortByRank(source, typeOrder),
            _ => SortByName(source, typeOrder)
        };
    }

    private List<Personnage> SortByPuissance(List<Personnage> source, Dictionary<TypePersonnage, int> typeOrder)
    {
        return sortAscending
            ? [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Type == TypePersonnage.Commandant ? p.Puissance + p.Rang * 20 : p.Puissance)]
            : [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Type == TypePersonnage.Commandant ? p.Puissance + p.Rang * 20 : p.Puissance)];
    }

    private List<Personnage> SortByName(List<Personnage> source, Dictionary<TypePersonnage, int> typeOrder)
    {
        return sortAscending
            ? [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Nom)]
            : [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Nom)];
    }

    private List<Personnage> SortByRarity(List<Personnage> source, Dictionary<TypePersonnage, int> typeOrder)
    {
        return sortAscending
            ? [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Rarete)]
            : [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Rarete)];
    }

    private List<Personnage> SortByLevel(List<Personnage> source, Dictionary<TypePersonnage, int> typeOrder)
    {
        return sortAscending
            ? [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Niveau)]
            : [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Niveau)];
    }

    private List<Personnage> SortByType(List<Personnage> source, Dictionary<TypePersonnage, int> typeOrder)
    {
        return sortAscending
            ? [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Type)]
            : [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Type)];
    }

    private List<Personnage> SortByRank(List<Personnage> source, Dictionary<TypePersonnage, int> typeOrder)
    {
        return sortAscending
            ? [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Rang)]
            : [.. source.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Rang)];
    }

    private void SortBy(string column)
    {
        if (sortColumn == column)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
        ApplyFiltersAndSorting();
    }

    internal void SortByPuissance() => SortBy(AppConstants.XmlElements.Puissance);
    internal void SortByNom() => SortBy(AppConstants.XmlElements.Nom);
    internal void SortByRarete() => SortBy(AppConstants.XmlElements.Rarete);
    internal void SortByNiveau() => SortBy(AppConstants.XmlElements.Niveau);
    internal void SortByRang() => SortBy(AppConstants.XmlElements.Rang);

    internal void HandleSearchInput(ChangeEventArgs e)
    {
        OnSearchChanged(e.Value?.ToString() ?? "");
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        // Ne filtrer que si au moins 2 caractères sont saisis
        if (searchTerm.Length >= 2 || string.IsNullOrWhiteSpace(searchTerm))
        {
            ApplyFiltersAndSorting();
        }
    }

    internal void ClearSearch()
    {
        searchTerm = "";
        ApplyFiltersAndSorting();
    }

    internal static MarkupString GetRankStars(int rank)
    {
        var starsBuilder = new System.Text.StringBuilder();
        for (int i = 1; i <= 7; i++)
        {
            if (i <= rank)
            {
                starsBuilder.Append("<span style='color: #FFD700;'>★</span>");
            }
            else
            {
                starsBuilder.Append("<span style='color: #CCCCCC;'>☆</span>");
            }
        }
        return new MarkupString(starsBuilder.ToString());
    }

    internal void ToggleSelection(int id)
    {
        if (!selectedPersonnages.Remove(id))
        {
            selectedPersonnages.Add(id);
        }
    }

    private void SelectAll()
    {
        if (selectedPersonnages.Count == personnagesFiltres.Count)
        {
            selectedPersonnages.Clear();
        }
        else
        {
            selectedPersonnages = [.. personnagesFiltres.Select(p => p.Id)];
        }
    }

    internal void ShowBulkEditModal()
    {
        if (selectedPersonnages.Any())
        {
            showBulkEditModal = true;
        }
    }

    internal async Task ApplyBulkEdit()
    {
        if (string.IsNullOrEmpty(bulkEditProperty) || selectedPersonnages.Count == 0)
            return;

        foreach (var id in selectedPersonnages)
        {
            var personnage = personnages.FirstOrDefault(p => p.Id == id);
            if (personnage != null)
            {
                switch (bulkEditProperty)
                {
                    case "Niveau":
                        if (int.TryParse(bulkEditValue, out int niveau))
                            personnage.Niveau = niveau;
                        break;
                    case "TypeAttaque":
                        if (Enum.TryParse<TypeAttaque>(bulkEditValue, out var typeAttaqueValue))
                            personnage.TypeAttaque = typeAttaqueValue;
                        break;
                    case "Selectionne":
                        if (bool.TryParse(bulkEditValue, out var selectionValue))
                            personnage.Selectionne = selectionValue;
                        break;
                }
                PersonnageService.Update(personnage);
            }
        }

        await LoadPersonnagesAsync();
        selectedPersonnages.Clear();
        showBulkEditModal = false;
        bulkEditProperty = "";
        bulkEditValue = "";
    }

    internal void ShowAddModal()
    {
        currentPersonnage = new Personnage();
        isEditing = false;
        showModal = true;
        StateHasChanged();
    }

    internal void EditPersonnage(Personnage personnage)
    {
        // Ouvrir la modale de détail directement en mode édition
        ModalService.Open<CharacterManager.Components.Modal.DetailPersonnageModal>(
            new Dictionary<string, object>
            {
                { "PersonnageId", personnage.Id },
                { "StartInEdit", true }
            },
            ModalSize.XL
        );
    }

    internal void DeletePersonnage(int id)
    {
        PersonnageService.Delete(id);
        _ = InvokeAsync(async () => await LoadPersonnagesAsync());
    }

    internal void SavePersonnage()
    {
        if (isEditing)
        {
            PersonnageService.Update(currentPersonnage);
        }
        else
        {
            PersonnageService.Add(currentPersonnage);
        }

        _ = InvokeAsync(async () =>
        {
            await LoadPersonnagesAsync();
            CloseModal();
        });
    }

    private void CloseModal()
    {
        showModal = false;
        currentPersonnage = new Personnage();
        StateHasChanged();
    }


    internal async Task DeleteSelectedPersonnages()
    {
        if (selectedPersonnages.Count != 0)
        {
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"Êtes-vous sûr de vouloir supprimer {selectedPersonnages.Count} personnage(s) sélectionné(s) ? Cette action est irréversible.");
            if (confirmed)
            {
                foreach (var id in selectedPersonnages)
                {
                    PersonnageService.Delete(id);
                }
                await LoadPersonnagesAsync();
                selectedPersonnages.Clear();
            }
        }
    }

    internal async Task ResetAll()
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"Êtes-vous sûr de vouloir supprimer toutes les données ? Cette action est irréversible.");
        if (confirmed)
        {
            PersonnageService.DeleteAll();
            selectedPersonnages.Clear();
            LuciePieces.Clear();
            lucieHouse = null;
            
            await LoadPersonnagesAsync();
            await LoadLucieHouseAsync();
        }
    }

    internal void ViewPersonnage(int id)
    {
        ModalService.Open<CharacterManager.Components.Modal.DetailPersonnageModal>(
            new Dictionary<string, object> { { "PersonnageId", id } },
            ModalSize.XL
        );
    }

    internal async Task ExportToPML()
    {
        try
        {
            // Exporter uniquement les personnages sélectionnés s'il y en a, sinon exporter la liste filtrée
            var personnagesAExporter = selectedPersonnages.Count > 0
                ? personnagesFiltres.Where(p => selectedPersonnages.Contains(p.Id))
                : personnagesFiltres;

            var pmlBytes = await PmlImportService.ExporterInventairePmlAsync(personnagesAExporter);
            var fileName = $"{AppConstants.ExportPrefixes.Inventaire}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";

            // Utiliser JavaScript pour télécharger le fichier
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pmlBytes));
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync(JsAlert, $"Erreur lors de l'export: {ex.Message}");
        }
    }

    internal async Task HandleImportInventaire(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null)
        {
            return;
        }

        var isSupported = file.Name.EndsWith(AppConstants.FileExtensions.Pml, StringComparison.OrdinalIgnoreCase)
                       || file.Name.EndsWith(AppConstants.FileExtensions.Xml, StringComparison.OrdinalIgnoreCase);

        if (!isSupported)
        {
            await JSRuntime.InvokeVoidAsync(JsAlert, "Veuillez sélectionner un fichier PML ou XML.");
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var result = await PmlImportService.ImportPmlAsync(
                stream,
                file.Name,
                importInventory: true,
                importTemplates: false,
                importBestSquad: false,
                importHistories: false,
                importLeagueHistory: false);

            var importMessage = result.SuccessCount > 0
                ? $"{result.SuccessCount} personnage(s) importé(s) avec succès."
                : "Aucun personnage importé.";

            if (!string.IsNullOrEmpty(result.Error))
            {
                importMessage += $"\nErreur: {result.Error}";
            }

            if (result.Errors.Count > 0)
            {
                var preview = string.Join("\n", result.Errors.Take(3));
                importMessage += $"\nDétails (aperçu):\n{preview}";
            }

            await JSRuntime.InvokeVoidAsync(JsAlert, importMessage);
            await LoadPersonnagesAsync();
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync(JsAlert, $"Erreur lors de l'import: {ex.Message}");
        }
    }

    // ===== Lucie House Management =====

    internal Toast? toastRef;

    internal async Task LoadLucieHouseAsync()
    {
        await EnsureLuciePieceAspectColumnsAsync(force: false);

        try
        {
            lucieHouse = await DbContext.LucieHouses
                .Include(l => l.Pieces)
                .FirstOrDefaultAsync();
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[LucieHouse] Missing aspect columns detected at query time; applying hotfix and retrying.");
            await EnsureLuciePieceAspectColumnsAsync(force: true);
            DbContext.ChangeTracker.Clear();
            try
            {
                lucieHouse = await DbContext.LucieHouses
                    .Include(l => l.Pieces)
                    .FirstOrDefaultAsync();
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Fallback: raw loader without relying on aspect columns mapping
                Console.WriteLine("[LucieHouse] Fallback raw loader engaged.");
                lucieHouse = await LoadLucieHouseFallbackRawAsync();
            }
        }

        if (lucieHouse == null)
        {
            lucieHouse = LucieHouse.CreerDefaut();
            DbContext.LucieHouses.Add(lucieHouse);
            await DbContext.SaveChangesAsync();
        }
    }

    private async Task<LucieHouse?> LoadLucieHouseFallbackRawAsync()
    {
        await using var conn = DbContext.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM LucieHouses ORDER BY Id LIMIT 1";
        var lucieIdObj = await cmd.ExecuteScalarAsync();

        if (lucieIdObj == null)
        {
            return null;
        }

        var lucieId = Convert.ToInt32(lucieIdObj);
        var result = new LucieHouse { Id = lucieId, Pieces = new List<Piece>() };

        await using var piecesCmd = conn.CreateCommand();
        piecesCmd.CommandText = "SELECT Id, Nom, Niveau, Puissance, Selectionnee FROM Pieces WHERE LucieHouseId = @id";
        var p = piecesCmd.CreateParameter();
        p.ParameterName = "@id";
        p.Value = lucieId;
        piecesCmd.Parameters.Add(p);

        await using var reader = await piecesCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var piece = new Piece
            {
                Id = reader.GetInt32(0),
                Nom = reader.GetString(1),
                Niveau = reader.GetInt32(2),
                Selectionnee = reader.GetInt32(4) != 0,
            };
            // Initialize aspects to safe defaults
            piece.AspectsTactiques.Nom = "Aspects tactiques";
            piece.AspectsTactiques.Puissance = 0;
            piece.AspectsStrategiques.Nom = "Aspects stratégiques";
            piece.AspectsStrategiques.Puissance = 0;
            result.Pieces.Add(piece);
        }

        return result;
    }

    private async Task EnsureLuciePieceAspectColumnsAsync(bool force)
    {
        try
        {
            // Quick guard in case table does not exist yet.
            if (!await TableExistsAsync("Pieces"))
            {
                return;
            }

            const string hydratedTactiques = "{\"Nom\":\"Aspects tactiques\",\"Puissance\":0,\"Bonus\":[]}";
            const string hydratedStrategiques = "{\"Nom\":\"Aspects stratégiques\",\"Puissance\":0,\"Bonus\":[]}";

            // Always check if column exists before adding it
            if (!await ColumnExistsAsync("Pieces", "AspectsTactiques"))
            {
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Pieces ADD COLUMN AspectsTactiques TEXT NOT NULL DEFAULT '';");
            }

            if (!await ColumnExistsAsync("Pieces", "AspectsStrategiques"))
            {
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Pieces ADD COLUMN AspectsStrategiques TEXT NOT NULL DEFAULT '';");
            }

            // Parameterize values to avoid EF1002 warnings
            // Force parameter only affects whether we update existing rows with default values
            if (force)
            {
                await DbContext.Database.ExecuteSqlAsync($"UPDATE Pieces SET AspectsTactiques = {hydratedTactiques} WHERE AspectsTactiques IS NULL OR AspectsTactiques = '';");
                await DbContext.Database.ExecuteSqlAsync($"UPDATE Pieces SET AspectsStrategiques = {hydratedStrategiques} WHERE AspectsStrategiques IS NULL OR AspectsStrategiques = '';");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LucieHouse] Column ensure failed: {ex.Message}");
        }
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        // Skip preliminary EF execution to avoid EF1002; use manual reader below.

        try
        {
            var conn = DbContext.Database.GetDbConnection();
            var shouldClose = conn.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                await conn.OpenAsync();
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"PRAGMA table_info({table});";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(1);
                    if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                if (shouldClose && conn.State == System.Data.ConnectionState.Open)
                {
                    await conn.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LucieHouse] ColumnExistsAsync error for {table}.{column}: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> TableExistsAsync(string table)
    {
        try
        {
            var conn = DbContext.Database.GetDbConnection();
            var shouldClose = conn.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                await conn.OpenAsync();
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name;";
                var param = cmd.CreateParameter();
                param.ParameterName = "@name";
                param.Value = table;
                cmd.Parameters.Add(param);
                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
            finally
            {
                if (shouldClose && conn.State == System.Data.ConnectionState.Open)
                {
                    await conn.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LucieHouse] TableExistsAsync error for {table}: {ex.Message}");
            return false;
        }
    }

    private async Task UpdatePieceField(int pieceId, string field, string value)
    {
        if (lucieHouse == null) return;

        var piece = lucieHouse.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece == null) return;

        switch (field)
        {
            case "Niveau":
                if (int.TryParse(value, out var niveau))
                {
                    piece.Niveau = niveau;
                }
                break;
            case "PuissanceStrategique":
                if (int.TryParse(value, out var puissance))
                {
                    piece.AspectsStrategiques.Puissance = puissance;
                }
                break;
            case "PuissanceTactique":
                if (int.TryParse(value, out var puissanceTactique))
                {
                    piece.AspectsTactiques.Puissance = puissanceTactique;
                }
                break;
        }

        await EnsureLuciePieceAspectColumnsAsync(force: false);
        DbContext.Pieces.Update(piece);
        await DbContext.SaveChangesAsync();
        await InvokeAsync(StateHasChanged);
        toastRef?.Show($"{piece.Nom} - {field} mis à jour: {value}", "success");
    }

    internal async Task UpdatePiecePuissance(int pieceId, string value)
    {
        if (lucieHouse == null) return;

        var piece = lucieHouse.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece != null && int.TryParse(value, out _))
        {
            await EnsureLuciePieceAspectColumnsAsync(force: false);
            DbContext.Pieces.Update(piece);
            await DbContext.SaveChangesAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    internal async Task TogglePieceSelection(int pieceId)
    {
        if (lucieHouse == null) return;

        var piece = lucieHouse.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece == null) return;

        if (piece.Selectionnee)
        {
            piece.Selectionnee = false;
        }
        else
        {
            if (lucieHouse.PeutSelectionner())
            {
                piece.Selectionnee = true;
            }
            else
            {
                toastRef?.Show($"Maximum {LucieHouse.MaxPiecesSelectionnees} pièces peuvent être sélectionnées", "warning");
                return;
            }
        }

        await EnsureLuciePieceAspectColumnsAsync(force: true);
        DbContext.Pieces.Update(piece);
        await DbContext.SaveChangesAsync();
        await InvokeAsync(StateHasChanged);
    }

    internal Task HandleInvalidDrop(string message)
    {
        toastRef?.Show(message, "warning");
        return Task.CompletedTask;
    }

    // Drag & Drop depuis les cartes
    internal int? currentlyDraggedId;

    internal void HandleDragStart(DragEventArgs e, Personnage personnage)
    {
        currentlyDraggedId = personnage.Id;
        StateHasChanged();
    }

    /// <summary>
    /// Retourne le style à appliquer à une image personnage
    /// Si l'URL est vide, affiche un fond lightblue
    /// </summary>
}
