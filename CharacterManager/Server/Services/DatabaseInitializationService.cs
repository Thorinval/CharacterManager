using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service for handling database initialization, migrations, and schema updates
/// </summary>
public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly IServiceProvider? _serviceProvider;

    public DatabaseInitializationService(ApplicationDbContext db, ILogger<DatabaseInitializationService> logger, IServiceProvider? serviceProvider = null)
    {
        _db = db;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes the database with migrations and ensures all tables and columns exist
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            var pendingMigrations = await _db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await _db.Database.MigrateAsync();
                await EnsureLuciePieceAspectColumnsAsync();
            }
            else
            {
                await EnsureCreatedAsync();
            }

            await EnsureAppSettingsColumnsAsync();
            await EnsurePersonnagesColumnsAsync();
            await CleanupLegacyTablesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration error during InitializeDatabaseAsync");
        }
    }

    /// <summary>
    /// Initializes default AppSettings and checks database state
    /// </summary>
    public async Task InitializeAppSettingsAndCheckStateAsync()
    {
        await InitializeAppSettingsAndCheckStateAsync(_serviceProvider);
    }

    /// <summary>
    /// Initializes default AppSettings and checks database state, with optional service provider for timeline seeding
    /// </summary>
    public async Task InitializeAppSettingsAndCheckStateAsync(IServiceProvider? serviceProvider)
    {
        try
        {
            var settings = await _db.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (settings == null)
            {
                var newSettings = new AppSettings
                {
                    IsAdultModeEnabled = true,
                    Language = "fr"
                };
                _db.AppSettings.Add(newSettings);
                await _db.SaveChangesAsync();
                Console.WriteLine("[Init] Created default AppSettings - Adult Mode: Enabled, Language: fr");
            }
            else
            {
                var adultModeStatus = settings.IsAdultModeEnabled ? "Enabled" : "Disabled";
                var language = string.IsNullOrEmpty(settings.Language) ? "fr" : settings.Language;
                if (string.IsNullOrEmpty(settings.Language))
                {
                    settings.Language = "fr";
                    await _db.SaveChangesAsync();
                    Console.WriteLine("[Init] Updated Language to default: fr");
                }
                Console.WriteLine($"[Init] Loaded AppSettings - Adult Mode: {adultModeStatus}, Language: {language}");
            }

            bool dbIsEmpty = !await _db.Personnages.AnyAsync() && !await _db.LucieHouses.AnyAsync();
            if (dbIsEmpty)
            {
                Console.WriteLine("[Init] La base de données est vide. Préparez l'import d'un fichier .pml pour initialisation.");
            }

            // Seed timeline if empty (non-blocking)
            if (serviceProvider != null)
            {
                try
                {
                    var timelineService = serviceProvider.GetService<ITeamPowerTimelineService>();
                    if (timelineService != null)
                    {
                        await timelineService.SeedIfEmptyAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to seed timeline; it will be populated on first data change");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialization error during InitializeAppSettingsAndCheckStateAsync");
        }
    }

    private async Task EnsureCreatedAsync()
    {
        await _db.Database.EnsureCreatedAsync();

        const string appSettingsSql = @"CREATE TABLE IF NOT EXISTS AppSettings (
            Id INTEGER NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY AUTOINCREMENT,
            LastImportedFileName TEXT NOT NULL DEFAULT '',
            LastImportedDate TEXT NULL,
            LastExportDate TEXT NULL,
            IsAdultModeEnabled INTEGER NOT NULL DEFAULT 1,
            ThumbnailHeightPx INTEGER NOT NULL DEFAULT 110
        );";
        await _db.Database.ExecuteSqlRawAsync(appSettingsSql);

        const string templatesSql = @"CREATE TABLE IF NOT EXISTS Templates (
            Id INTEGER NOT NULL CONSTRAINT PK_Templates PRIMARY KEY AUTOINCREMENT,
            Nom TEXT NOT NULL,
            Description TEXT NULL,
            PuissanceTotal INTEGER NOT NULL,
            DateCreation TEXT NOT NULL,
            DateModification TEXT NULL,
            PersonnagesJson TEXT NOT NULL
        );";
        await _db.Database.ExecuteSqlRawAsync(templatesSql);

        const string historiquesEscouadeSql = @"CREATE TABLE IF NOT EXISTS HistoriquesEscouade (
            Id INTEGER NOT NULL CONSTRAINT PK_HistoriquesEscouade PRIMARY KEY AUTOINCREMENT,
            DateEnregistrement TEXT NOT NULL,
            PuissanceTotal INTEGER NOT NULL,
            Classement INTEGER NULL,
            DonneesEscouadeJson TEXT NOT NULL
        );";
        await _db.Database.ExecuteSqlRawAsync(historiquesEscouadeSql);

        const string lucieHousesSql = @"CREATE TABLE IF NOT EXISTS LucieHouses (
            Id INTEGER NOT NULL CONSTRAINT PK_LucieHouses PRIMARY KEY AUTOINCREMENT
        );";
        await _db.Database.ExecuteSqlRawAsync(lucieHousesSql);

        const string piecesSql = @"CREATE TABLE IF NOT EXISTS Pieces (
            Id INTEGER NOT NULL CONSTRAINT PK_Pieces PRIMARY KEY AUTOINCREMENT,
            Nom TEXT NOT NULL,
            Niveau INTEGER NOT NULL,
            Puissance INTEGER NOT NULL,
            Selectionnee INTEGER NOT NULL,
            BonusTactiques TEXT NOT NULL,
            BonusStrategiques TEXT NOT NULL,
            AspectsTactiques TEXT NOT NULL,
            AspectsStrategiques TEXT NOT NULL,
            LucieHouseId INTEGER,
            FOREIGN KEY (LucieHouseId) REFERENCES LucieHouses (Id) ON DELETE CASCADE
        );";
        await _db.Database.ExecuteSqlRawAsync(piecesSql);
        
        _logger.LogInformation("[DB] Ensured all core tables exist: AppSettings, Templates, HistoriquesEscouade, LucieHouses, Pieces.");
        await EnsureLuciePieceAspectColumnsAsync();

        const string profilesSql = @"CREATE TABLE IF NOT EXISTS Profiles (
            Id INTEGER NOT NULL CONSTRAINT PK_Profiles PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            AdultMode INTEGER NOT NULL DEFAULT 0,
            Language TEXT NOT NULL DEFAULT 'fr',
            Role TEXT NOT NULL DEFAULT 'utilisateur',
            PasswordHash TEXT NOT NULL DEFAULT '',
            PasswordSalt TEXT NOT NULL DEFAULT '',
            HashAlgorithm TEXT NOT NULL DEFAULT 'PBKDF2',
            FailedLoginCount INTEGER NOT NULL DEFAULT 0,
            LockoutUntil TEXT NULL
        );";
        await _db.Database.ExecuteSqlRawAsync(profilesSql);
        Console.WriteLine("[DB] Ensured Profiles table exists.");

        await EnsureProfileColumnsAsync();
    }

    private async Task EnsureLuciePieceAspectColumnsAsync()
    {
        if (!await TableExistsAsync("Pieces"))
            return;

        const string hydratedTactiques = "{\"Nom\":\"Aspects tactiques\",\"Puissance\":0,\"Bonus\":[]}";
        const string hydratedStrategiques = "{\"Nom\":\"Aspects stratégiques\",\"Puissance\":0,\"Bonus\":[]}";

        try
        {
            if (!await ColumnExistsAsync("Pieces", "AspectsTactiques"))
            {
                await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Pieces ADD COLUMN AspectsTactiques TEXT NOT NULL DEFAULT '';");
                _logger.LogInformation("[DB] Added AspectsTactiques column to Pieces.");
            }

            if (!await ColumnExistsAsync("Pieces", "AspectsStrategiques"))
            {
                await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Pieces ADD COLUMN AspectsStrategiques TEXT NOT NULL DEFAULT '';");
                _logger.LogInformation("[DB] Added AspectsStrategiques column to Pieces.");
            }

            await _db.Database.ExecuteSqlAsync($"UPDATE Pieces SET AspectsTactiques = {hydratedTactiques} WHERE AspectsTactiques IS NULL OR AspectsTactiques = '';");
            await _db.Database.ExecuteSqlAsync($"UPDATE Pieces SET AspectsStrategiques = {hydratedStrategiques} WHERE AspectsStrategiques IS NULL OR AspectsStrategiques = '';");
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "[DB] Error ensuring Lucie aspects columns");
        }
    }

    private async Task EnsureAppSettingsColumnsAsync()
    {
        const string appSettingsTableName = "AppSettings";

        if (!await TableExistsAsync(appSettingsTableName))
            return;

        if (!await ColumnExistsAsync(appSettingsTableName, "IsAdultModeEnabled"))
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("ALTER TABLE AppSettings ADD COLUMN IsAdultModeEnabled INTEGER NOT NULL DEFAULT 1;");
                _logger.LogInformation("[DB] Added IsAdultModeEnabled to AppSettings.");
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "[DB] Could not add IsAdultModeEnabled");
            }
        }

        if (!await ColumnExistsAsync(appSettingsTableName, "Language"))
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("ALTER TABLE AppSettings ADD COLUMN Language TEXT NOT NULL DEFAULT 'fr';");
                _logger.LogInformation("[DB] Added Language to AppSettings.");
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "[DB] Could not add Language");
            }
        }
    }

    private async Task EnsurePersonnagesColumnsAsync()
    {
        if (!await TableExistsAsync("Personnages"))
            return;

        if (!await ColumnExistsAsync("Personnages", "ImageUrlHeader"))
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Personnages ADD COLUMN ImageUrlHeader TEXT NOT NULL DEFAULT '';");
                _logger.LogInformation("[DB] Added ImageUrlHeader to Personnages.");
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "[DB] Could not add ImageUrlHeader");
            }
        }
    }

    private async Task EnsureProfileColumnsAsync()
    {
        const string profilesTableName = "Profiles";

        if (!await TableExistsAsync(profilesTableName))
            return;

        var columnsToAdd = new[]
        {
            ("Role", "ALTER TABLE Profiles ADD COLUMN Role TEXT NOT NULL DEFAULT 'utilisateur';", "Role"),
            ("PasswordHash", "ALTER TABLE Profiles ADD COLUMN PasswordHash TEXT NOT NULL DEFAULT '';", "PasswordHash"),
            ("PasswordSalt", "ALTER TABLE Profiles ADD COLUMN PasswordSalt TEXT NOT NULL DEFAULT '';", "PasswordSalt"),
            ("HashAlgorithm", "ALTER TABLE Profiles ADD COLUMN HashAlgorithm TEXT NOT NULL DEFAULT 'PBKDF2';", "HashAlgorithm"),
            ("FailedLoginCount", "ALTER TABLE Profiles ADD COLUMN FailedLoginCount INTEGER NOT NULL DEFAULT 0;", "FailedLoginCount"),
            ("LockoutUntil", "ALTER TABLE Profiles ADD COLUMN LockoutUntil TEXT NULL;", "LockoutUntil")
        };

        foreach (var (column, sql, displayName) in columnsToAdd)
        {
            if (!await ColumnExistsAsync(profilesTableName, column))
            {
                try
                {
                    await _db.Database.ExecuteSqlRawAsync(sql);
                    _logger.LogInformation("[DB] Added {DisplayName} column to Profiles.", displayName);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "[DB] Could not add {DisplayName}", displayName);
                }
            }
        }
    }

    private async Task CleanupLegacyTablesAsync()
    {
        if (await TableExistsAsync("AppImages"))
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS AppImages;");
                _logger.LogInformation("[DB] Dropped legacy AppImages table.");
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "[DB] Could not drop AppImages");
            }
        }
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        try
        {
            // Check if using a relational database provider
            if (!_db.Database.IsRelational())
            {
                // For in-memory database, check if the property exists in the entity model
                var entityType = _db.Model.GetEntityTypes()
                    .FirstOrDefault(e => e.GetTableName()?.Equals(table, StringComparison.OrdinalIgnoreCase) == true);
                
                if (entityType != null)
                {
                    return entityType.GetProperties()
                        .Any(p => p.Name.Equals(column, StringComparison.OrdinalIgnoreCase));
                }
                return false;
            }

            using var conn = (SqliteConnection)_db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table});";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DB] ColumnExistsAsync failed for table {Table} column {Column}", table, column);
        }
        return false;
    }

    private async Task<bool> TableExistsAsync(string table)
    {
        try
        {
            // Check if using a relational database provider
            if (_db.Database.IsRelational())
            {
                using var conn = (SqliteConnection)_db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name;";
                cmd.Parameters.AddWithValue("@name", table);
                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
            else
            {
                // For in-memory database, check if the entity type exists in the model
                var entityType = _db.Model.FindEntityType(table);
                if (entityType != null)
                {
                    return true;
                }
                
                // Also check by table name in the model
                return _db.Model.GetEntityTypes().Any(e => 
                    e.GetTableName()?.Equals(table, StringComparison.OrdinalIgnoreCase) == true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DB] TableExistsAsync failed for table {Table}", table);
        }
        return false;
    }

        /// <summary>
        /// Génère rétroactivement l'historique de puissance Lucie à partir des classements et modifications existants
        /// </summary>
        public async Task<(int ClassementsTraites, int JoursTraites)> GenerateLuciePowerHistoryAsync(IHistoriqueModificationService historiqueService, IPersonnageService personnageService)
        {
            int classementCount = 0;
            int jourCount = 0;

            try
            {
                _logger.LogInformation("[LucieHistory] Début de la génération de l'historique de puissance Lucie");

                // 1. Traiter les classements existants
                var classements = await _db.HistoriquesClassement
                    .OrderBy(h => h.DateEnregistrement)
                    .ToListAsync();

                _logger.LogInformation("[LucieHistory] Trouvé {Count} classements", classements.Count);

                foreach (var classement in classements)
                {
                    var dateClassement = classement.DateEnregistrement.ToDateTime(TimeOnly.MinValue);
                    var puissanceLucie = classement.PuissanceLucie;

                    // Vérifier si un enregistrement existe déjà
                    var existeDeja = await _db.HistoriquesModifications
                        .AnyAsync(h => h.TypeEntite == TypeEntite.Piece
                            && h.EntiteId == -1
                            && h.ChampModifie == Constants.StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee
                            && h.DateModification.Date == dateClassement.Date);

                    if (!existeDeja && puissanceLucie > 0)
                    {
                        await historiqueService.EnregistrerPuissanceLucieAsync(false, puissanceLucie, dateClassement);
                        await historiqueService.EnregistrerPuissanceLucieAsync(true, puissanceLucie, dateClassement);
                        classementCount++;
                        _logger.LogInformation("[LucieHistory] ✓ {Date}: Puissance {Power}", classement.DateEnregistrement, puissanceLucie);
                    }
                }

                _logger.LogInformation("[LucieHistory] {Count} classements traités", classementCount);

                // 2. Traiter les modifications de pièces
                var modificationsPieces = await _db.HistoriquesModifications
                    .Where(h => h.TypeEntite == TypeEntite.Piece
                        && h.EntiteId > 0
                        && (h.ChampModifie == "AspectsTactiques.Puissance"
                            || h.ChampModifie == "AspectsStrategiques.Puissance"
                            || h.ChampModifie == "Selectionnee"))
                    .OrderBy(h => h.DateModification)
                    .ToListAsync();

                _logger.LogInformation("[LucieHistory] Trouvé {Count} modifications de pièces", modificationsPieces.Count);

                var joursAvecModifications = modificationsPieces
                    .Select(m => m.DateModification.Date)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                _logger.LogInformation("[LucieHistory] Sur {Count} jours distincts", joursAvecModifications.Count);

                foreach (var jour in joursAvecModifications)
                {
                    // Vérifier si un enregistrement existe déjà
                    var existeDeja = await _db.HistoriquesModifications
                        .AnyAsync(h => h.TypeEntite == TypeEntite.Piece
                            && h.EntiteId == -1
                            && h.ChampModifie == Constants.StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee
                            && h.DateModification.Date == jour);

                    if (!existeDeja)
                    {
                        // Utiliser la puissance actuelle comme valeur de référence
                        var puissanceSelectionnee = personnageService.GetPuissanceLucieEscouade();
                        var puissanceMax = personnageService.GetPuissanceMaxLucieEscouade();

                        if (puissanceSelectionnee > 0 || puissanceMax > 0)
                        {
                            await historiqueService.EnregistrerPuissanceLucieAsync(false, puissanceSelectionnee, jour);
                            await historiqueService.EnregistrerPuissanceLucieAsync(true, puissanceMax, jour);
                            jourCount++;
                            _logger.LogInformation("[LucieHistory] ✓ {Date:yyyy-MM-dd}: Sélection={Selection}, Max={Max}", jour, puissanceSelectionnee, puissanceMax);
                        }
                    }
                }

                _logger.LogInformation("[LucieHistory] {Count} jours traités", jourCount);

                // 3. Vérification finale
                var totalLucieSelectionnee = await _db.HistoriquesModifications
                    .CountAsync(h => h.TypeEntite == TypeEntite.Piece
                        && h.EntiteId == -1
                        && h.ChampModifie == Constants.StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee);

                var totalLucieMax = await _db.HistoriquesModifications
                    .CountAsync(h => h.TypeEntite == TypeEntite.Piece
                        && h.EntiteId == -2
                        && h.ChampModifie == Constants.StatisticsConstants.HistoryFields.PuissanceLucieMax);

                _logger.LogInformation("[LucieHistory] Enregistrements PuissanceLucieSelectionnee: {Count}", totalLucieSelectionnee);
                _logger.LogInformation("[LucieHistory] Enregistrements PuissanceLucieMax: {Count}", totalLucieMax);
                _logger.LogInformation("[LucieHistory] Génération terminée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LucieHistory] Erreur lors de la génération de l'historique");
                throw;
            }

            return (classementCount, jourCount);
        }
}




