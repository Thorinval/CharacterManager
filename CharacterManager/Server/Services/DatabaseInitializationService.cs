using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service for handling database initialization, migrations, and schema updates
/// </summary>
public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(ApplicationDbContext db, ILogger<DatabaseInitializationService> logger)
    {
        _db = db;
        _logger = logger;
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
}




