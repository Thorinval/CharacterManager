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
    public IWebHostEnvironment WebHostEnvironment { get; set; } = null!;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    private List<Personnage> personnages = [];
    private List<Personnage> personnagesFiltres = [];
    private LucieHouse? lucieHouse;
    private List<Piece> LuciePieces => lucieHouse?.Pieces ?? [];
    private List<Piece> luciePiecesFiltres = [];
    private bool showModal = false;
    private bool isEditing = false;
    private Personnage currentPersonnage = new();

    // Tri
    private string sortColumn = AppConstants.XmlElements.Puissance;
    private bool sortAscending = false;

    // Sélection multiple
    private HashSet<int> selectedPersonnages = [];
    private bool showBulkEditModal = false;
    private string bulkEditProperty = "";
    private string bulkEditValue = "";

    // Filtre Commandants
    private bool ShowOnlyCommandants = false;
    private bool ShowOnlyMercenaires = false;
    private bool ShowOnlyAndroides = false;
    private bool ShowOnlyLucyRooms = false;

    private void ToggleShowOnlyCommandants(ChangeEventArgs e)
    {
        ShowOnlyCommandants = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    private void ToggleShowOnlyMercenaires(ChangeEventArgs e)
    {
        ShowOnlyMercenaires = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    private void ToggleShowOnlyAndroides(ChangeEventArgs e)
    {
        ShowOnlyAndroides = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    private void ToggleShowOnlyLucyRooms(ChangeEventArgs e)
    {
        ShowOnlyLucyRooms = (bool?)e.Value == true;
        ApplyFiltersAndSorting();
    }

    private bool SelectAllChecked
    {
        get => selectedPersonnages.Count == personnagesFiltres.Count && personnagesFiltres.Count > 0;
        set => SelectAll();
    }

    private IEnumerable<IGrouping<TypePersonnage, Personnage>> GroupedPersonnages =>
        personnagesFiltres.GroupBy(p => p.Type)
            .OrderBy(g => g.Key == TypePersonnage.Commandant ? 1 : g.Key == TypePersonnage.Mercenaire ? 2 : 3);

    // Filtre
    private string searchTerm = "";

    // Mode d'affichage
    private string viewMode = "grid";

    protected override async Task OnInitializedAsync()
    {
        await LoadPersonnagesAsync();
        await LoadLucieHouseAsync();
        // Charger un template si présent dans l'URL
        var uri = new Uri(Navigation.Uri);
        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals("templateId", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(Uri.UnescapeDataString(kv[1]), out var templateId))
                    {
                        showTemplateEditor = true;
                        templates = [.. PersonnageService.GetAllTemplates()];
                        selectedTemplateId = templateId;
                        _ = InvokeAsync(async () => await LoadSelectedTemplate());
                    }
                }
            }
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private string GetViewModeClass(string mode)
    {
        return viewMode == mode ? "btn-primary" : "btn-outline-secondary";
    }

    private async Task ChangeNiveauPiece(int pieceId, int delta)
    {
        var piece = lucieHouse?.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece != null)
        {
            int newValue = Math.Max(0, piece.Niveau + delta);
            await Task.Run(() => UpdatePieceField(pieceId, "Niveau", newValue.ToString()));
        }
    }

    private async Task ChangePuissance(int personnageId, int delta)
    {
        var personnage = personnagesFiltres.FirstOrDefault(p => p.Id == personnageId);
        if (personnage != null)
        {
            int newValue = Math.Max(0, personnage.Puissance + delta);
            await Task.Run(() => UpdatePersonnageField(personnageId, "Puissance", newValue.ToString()));
        }
    }


    private async Task ChangePuissancePiece(int pieceId, TypeBonus typeBonus, int delta)
    {
        var piece = lucieHouse?.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece != null)
        {
            int newValue = 0;
            string typepuissance = typeBonus == TypeBonus.Tactique ? "PuissanceTactique" : "PuissanceStrategique";

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

    private string GetContainerClass()
    {
        return viewMode == "grid" ? "personnages-grid" : "personnages-list";
    }

    private static string GetContainerClassCompact()
    {
        return "personnages-grid-compact";
    }

    private static string GetRarityClass(Rarete rarete)
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
                case "Niveau":
                    if (int.TryParse(value, out int niveau) && niveau >= 1 && niveau <= 200)
                    {
                        personnage.Niveau = niveau;
                    }
                    break;
                case "Rang":
                    if (int.TryParse(value, out int rang) && rang >= 0 && rang <= 7)
                    {
                        personnage.Rang = rang;
                    }
                    break;
                case "Puissance":
                    if (int.TryParse(value, out int puissance) && puissance >= 0)
                    {
                        personnage.Puissance = puissance;
                    }
                    break;
                case "Selectionne":
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

    private void UpdateRankFromStar(int personnageId, int clickedStar, int currentRank)
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

    private record FilterOption(InventoryFilter Value, string LocalizationKey);

    private readonly List<FilterOption> FilterOptions =
    [
        new(InventoryFilter.Tous, "inventory.showAll"),
        new(InventoryFilter.Commandants, "inventory.showOnlyCommandants"),
        new(InventoryFilter.Mercenaires, "inventory.showOnlyMercenaires"),
        new(InventoryFilter.Androides, "inventory.showOnlyAndroides"),
        new(InventoryFilter.LucyRooms, "inventory.showOnlyLucyRooms")
    ];

    private void OnFilterClicked(InventoryFilter clicked)
    {
        if (SelectedFilter == clicked)
            return;

        SelectedFilter = clicked;
        ApplyFiltersAndSorting();
    }

    // 🔢 Compteur dynamique pour les badges
    private int GetCount(InventoryFilter filter)
    {
        return filter switch
        {
            InventoryFilter.Tous => personnages.Count + LuciePieces.Count,
            InventoryFilter.Commandants => personnages.Count(p => p.Type == TypePersonnage.Commandant),
            InventoryFilter.Mercenaires => personnages.Count(p => p.Type == TypePersonnage.Mercenaire),
            InventoryFilter.Androides => personnages.Count(p => p.Type == TypePersonnage.Androïde),
            InventoryFilter.LucyRooms => LuciePieces.Count,
            _ => 0
        };
    }

    private string GetBadgeColor(InventoryFilter filter)
    {
        IEnumerable<Personnage> list = filter switch
        {
            InventoryFilter.Tous => personnages,
            InventoryFilter.Commandants => personnages.Where(p => p.Type == TypePersonnage.Commandant),
            InventoryFilter.Mercenaires => personnages.Where(p => p.Type == TypePersonnage.Mercenaire),
            InventoryFilter.Androides => personnages.Where(p => p.Type == TypePersonnage.Androïde),
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
        IEnumerable<Personnage> filtered = personnages;

        IEnumerable<Piece> filteredPieces = LuciePieces ?? [];

        // 🔍 Filtre de recherche
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filtered = filtered.Where(p =>
                p.Nom.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || p.Rarete.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || p.Type.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || p.Role.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || p.Faction.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || p.TypeAttaque.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || p.Selectionne.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );

            filteredPieces = filteredPieces.Where(p =>
                p.Nom.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );
        }

        filtered = SelectedFilter switch
        {
            InventoryFilter.Tous => filtered,
            InventoryFilter.Commandants => filtered.Where(p => p.Type == TypePersonnage.Commandant),
            InventoryFilter.Mercenaires => filtered.Where(p => p.Type == TypePersonnage.Mercenaire),
            InventoryFilter.Androides => filtered.Where(p => p.Type == TypePersonnage.Androïde),
            InventoryFilter.LucyRooms => filtered.Where(p => p.Type == TypePersonnage.Inconnu),
            _ => filtered
        };

        filteredPieces = SelectedFilter switch
        {
            InventoryFilter.Tous => filteredPieces,
            InventoryFilter.LucyRooms => filteredPieces,
            _ => filteredPieces.Where(p => false) // Empty
        };

        // On matérialise la liste
        personnagesFiltres = [.. filtered];

        luciePiecesFiltres = [.. filteredPieces];

        // 📊 Tri : ordre par type puis par colonne
        var typeOrder = new Dictionary<TypePersonnage, int>
    {
        { TypePersonnage.Commandant, 1 },
        { TypePersonnage.Mercenaire, 2 },
        { TypePersonnage.Androïde, 3 }
    };

        personnagesFiltres = sortColumn switch
        {
            "Puissance" => sortAscending
                ? [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Type == TypePersonnage.Commandant ? p.Puissance + p.Rang * 20 : p.Puissance)]
                : [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Type == TypePersonnage.Commandant ? p.Puissance + p.Rang * 20 : p.Puissance)],

            "Nom" => sortAscending
                ? [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Nom)]
                : [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Nom)],

            "Rarete" => sortAscending
                ? [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Rarete)]
                : [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Rarete)],

            "Niveau" => sortAscending
                ? [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Niveau)]
                : [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Niveau)],

            "Type" => sortAscending
                ? [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Type)]
                : [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Type)],

            "Rang" => sortAscending
                ? [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Rang)]
                : [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenByDescending(p => p.Rang)],

            _ => [.. personnagesFiltres.OrderBy(p => typeOrder.GetValueOrDefault(p.Type, 99)).ThenBy(p => p.Nom)]
        };
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

    private void HandleSearchInput(ChangeEventArgs e)
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

    private void ClearSearch()
    {
        searchTerm = "";
        ApplyFiltersAndSorting();
    }

    private static MarkupString GetRankStars(int rank)
    {
        var stars = "";
        for (int i = 1; i <= 7; i++)
        {
            if (i <= rank)
            {
                stars += "<span style='color: #FFD700;'>★</span>";
            }
            else
            {
                stars += "<span style='color: #CCCCCC;'>☆</span>";
            }
        }
        return new MarkupString(stars);
    }

    private void ToggleSelection(int id)
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

    private void ShowBulkEditModal()
    {
        if (selectedPersonnages.Any())
        {
            showBulkEditModal = true;
        }
    }

    private async Task ApplyBulkEdit()
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

    private void ShowAddModal()
    {
        currentPersonnage = new Personnage();
        isEditing = false;
        showModal = true;
        StateHasChanged();
    }

    private void EditPersonnage(Personnage personnage)
    {
        currentPersonnage = new Personnage
        {
            Id = personnage.Id,
            Nom = personnage.Nom,
            Rarete = personnage.Rarete,
            Niveau = personnage.Niveau,
            Type = personnage.Type,
            Rang = personnage.Rang,
            Puissance = personnage.Puissance,
            PA = personnage.PA,
            PV = personnage.PV,
            Role = personnage.Role,
            Faction = personnage.Faction,
            Description = personnage.Description,
            Selectionne = personnage.Selectionne,
            TypeAttaque = personnage.TypeAttaque
        };
        isEditing = true;
        showModal = true;
        StateHasChanged();
    }

    private void DeletePersonnage(int id)
    {
        PersonnageService.Delete(id);
        _ = InvokeAsync(async () => await LoadPersonnagesAsync());
    }

    private void SavePersonnage()
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

    private void SortByNom() => SortBy("Nom");
    private void SortByRarete() => SortBy("Rarete");
    private void SortByNiveau() => SortBy("Niveau");
    private void SortByRang() => SortBy("Rang");
    private void SortByPuissance() => SortBy("Puissance");

    private async Task DeleteSelectedPersonnages()
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

    private void ViewPersonnage(int id)
    {
        Navigation.NavigateTo($"/detail-personnage/{id}");
    }

    private async Task ExportToPML()
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
            await JSRuntime.InvokeVoidAsync("alert", $"Erreur lors de l'export: {ex.Message}");
        }
    }

    // ===== Template Methods =====

    private bool showTemplateEditor = false;
    private Toast? toastRef;
    private string templateNom = string.Empty;
    private string templateDescription = string.Empty;
    private List<Personnage?> templatePersonnages = [];
    private List<int> templateSelectedIds = [];
    private List<Template> templates = [];
    private int selectedTemplateId = 0;

    private void OpenTemplateEditor()
    {
        showTemplateEditor = true;
        templateNom = string.Empty;
        templateDescription = string.Empty;
        templatePersonnages.Clear();
        templateSelectedIds.Clear();
        templates = [.. PersonnageService.GetAllTemplates()];
        selectedTemplateId = 0;
        viewMode = "grid";
    }

    private void CancelTemplateCreation()
    {
        showTemplateEditor = false;
        templateNom = string.Empty;
        templateDescription = string.Empty;
        templatePersonnages.Clear();
        templateSelectedIds.Clear();
        selectedTemplateId = 0;
    }

    private async Task HandleTemplateSelectionChanged(List<int> selectedIds)
    {
        templateSelectedIds = selectedIds;
        // Recharger les personnages sélectionnés
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

    private async Task LoadLucieHouseAsync()
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

    private async Task UpdatePiecePuissance(int pieceId, string value)
    {
        if (lucieHouse == null) return;

        var piece = lucieHouse.Pieces.FirstOrDefault(p => p.Id == pieceId);
        if (piece != null && int.TryParse(value, out var puissance))
        {
            await EnsureLuciePieceAspectColumnsAsync(force: false);
            DbContext.Pieces.Update(piece);
            await DbContext.SaveChangesAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task TogglePieceSelection(int pieceId)
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

    private async Task SaveTemplate()
    {
        if (string.IsNullOrEmpty(templateNom) || templateSelectedIds.Count == 0)
            return;

        try
        {
            var template = await PersonnageService.CreateTemplateAsync(
                templateNom,
                templateDescription,
                templateSelectedIds
            );

            toastRef?.Show($"Template '{template.Nom}' créé avec succès!", "success");
            CancelTemplateCreation();
            templates = [.. PersonnageService.GetAllTemplates()];
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de la création du template: {ex.Message}", "error");
        }
    }

    private async Task LoadSelectedTemplate()
    {
        if (selectedTemplateId == 0)
            return;

        var template = await PersonnageService.GetTemplateAsync(selectedTemplateId);
        if (template is null)
        {
            toastRef?.Show("Template introuvable", "error");
            return;
        }

        var ids = template.GetPersonnageIds();
        templateSelectedIds = ids;
        templatePersonnages = [];
        foreach (var id in ids)
        {
            var p = PersonnageService.GetById(id);
            if (p != null)
                templatePersonnages.Add(p);
        }
        templateNom = template.Nom;
        templateDescription = template.Description;
        toastRef?.Show($"Template '{template.Nom}' chargé.", "info");
    }

    private async Task ExportTemplateAsPml()
    {
        if (templateSelectedIds.Count == 0)
            return;

        try
        {
            var template = new Template
            {
                Nom = templateNom,
                Description = templateDescription
            };
            template.SetPersonnageIds(templateSelectedIds);

            var pmlBytes = await PmlImportService.ExporterTemplatesPmlAsync(new[] { template });
            var fileName = $"{AppConstants.ExportPrefixes.Template}_{templateNom}_{DateTime.Now.ToString(AppConstants.DateTimeFormats.FileNameDateTime)}{AppConstants.FileExtensions.Pml}";

            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pmlBytes));
        }
        catch (Exception ex)
        {
            toastRef?.Show($"Erreur lors de l'export du template: {ex.Message}", "error");
        }
    }

    private Task HandleInvalidDrop(string message)
    {
        toastRef?.Show(message, "warning");
        return Task.CompletedTask;
    }

    // Drag & Drop depuis les cartes
    private int? currentlyDraggedId;

    private void HandleDragStart(DragEventArgs e, Personnage personnage)
    {
        currentlyDraggedId = personnage.Id;
        StateHasChanged();
    }

    /// <summary>
    /// Retourne le style à appliquer à une image personnage
    /// Si l'URL est vide, affiche un fond lightblue
    /// </summary>
}
