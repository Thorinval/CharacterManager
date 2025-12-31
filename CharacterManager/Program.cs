using CharacterManager.Components; 
using CharacterManager.Server.Data;
using CharacterManager.Server.Services; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=charactermanager.db"));

// Register ProfileService BEFORE PersonnageService (dependency order)
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<PersonnageService>();
builder.Services.AddScoped<PmlImportService>();
builder.Services.AddScoped<HistoriqueClassementService>();
builder.Services.AddScoped<ClientLocalizationService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<RoadmapService>();

// AppImageService no longer used for categorization; DI registration removed
builder.Services.AddSingleton<PersonnageImageConfigService>();
builder.Services.AddSingleton<AppVersionService>();
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<LanguageContextService>();  // Service de contexte de langue
builder.Services.AddSingleton<AdultModeNotificationService>();  // Service singleton pour notification mode adulte
builder.Services.AddSingleton<IModalService, ModalService>();


builder.Services.AddHttpClient<UpdateService>();
builder.Services.AddHttpClient();  // Pour les appels HTTP du ClientLocalizationService

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pmlImportService = scope.ServiceProvider.GetRequiredService<PmlImportService>();
    bool ColumnExists(string table, string column)
    {
        try
        {
            using var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return false;
    }

    bool TableExists(string table)
    {
        try
        {
            using var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name;";
            cmd.Parameters.AddWithValue("@name", table);
            var result = cmd.ExecuteScalar();
            return result != null;
        }
        catch { }
        return false;
    }

    void EnsureLuciePieceAspectColumns()
    {
        if (!TableExists("Pieces"))
        {
            return;
        }

        // Use empty defaults to avoid braces parsing in EF, then hydrate values explicitly.
        const string hydratedTactiques = "{\"Nom\":\"Aspects tactiques\",\"Puissance\":0,\"Bonus\":[]}";
        const string hydratedStrategiques = "{\"Nom\":\"Aspects stratégiques\",\"Puissance\":0,\"Bonus\":[]}";

        try
        {
            if (!ColumnExists("Pieces", "AspectsTactiques"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Pieces ADD COLUMN AspectsTactiques TEXT NOT NULL DEFAULT '';");
                Console.WriteLine("[DB] Added AspectsTactiques column to Pieces.");
            }

            if (!ColumnExists("Pieces", "AspectsStrategiques"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Pieces ADD COLUMN AspectsStrategiques TEXT NOT NULL DEFAULT '';");
                Console.WriteLine("[DB] Added AspectsStrategiques column to Pieces.");
            }

            db.Database.ExecuteSql($"UPDATE Pieces SET AspectsTactiques = {hydratedTactiques} WHERE AspectsTactiques IS NULL OR AspectsTactiques = '';");
            db.Database.ExecuteSql($"UPDATE Pieces SET AspectsStrategiques = {hydratedStrategiques} WHERE AspectsStrategiques IS NULL OR AspectsStrategiques = '';");
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"[DB] Error ensuring Lucie aspects columns: {ex.Message}");
        }
    }
    try
    {
        // Get pending migrations
        var pendingMigrations = db.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            // Apply pending migrations
            db.Database.Migrate();
            EnsureLuciePieceAspectColumns();
        }
        else
        {
            // Ensure database and tables exist
            db.Database.EnsureCreated();

            // Hotfix: Ensure 'AppSettings' table exists for legacy DBs
            var createAppSettingsSql = @"CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY AUTOINCREMENT,
                LastImportedFileName TEXT NOT NULL DEFAULT '',
                LastImportedDate TEXT NULL,
                IsAdultModeEnabled INTEGER NOT NULL DEFAULT 1,
                ThumbnailHeightPx INTEGER NOT NULL DEFAULT 110
            );";
            db.Database.ExecuteSqlRaw(createAppSettingsSql);
            Console.WriteLine("[DB] Ensured AppSettings table exists.");

            // Hotfix: Ensure 'Templates' table exists in existing SQLite DBs without migrations
            // This avoids 'no such table: Templates' when upgrading from versions without the Templates entity.
            var createTemplatesSql = @"CREATE TABLE IF NOT EXISTS Templates (
                Id INTEGER NOT NULL CONSTRAINT PK_Templates PRIMARY KEY AUTOINCREMENT,
                Nom TEXT NOT NULL,
                Description TEXT NULL,
                PuissanceTotal INTEGER NOT NULL,
                DateCreation TEXT NOT NULL,
                DateModification TEXT NULL,
                PersonnagesJson TEXT NOT NULL
            );";
            db.Database.ExecuteSqlRaw(createTemplatesSql);
            Console.WriteLine("[DB] Ensured Templates table exists.");

            // Hotfix: Ensure 'HistoriquesEscouade' table exists
            var createHistoriquesEscouadeSql = @"CREATE TABLE IF NOT EXISTS HistoriquesEscouade (
                Id INTEGER NOT NULL CONSTRAINT PK_HistoriquesEscouade PRIMARY KEY AUTOINCREMENT,
                DateEnregistrement TEXT NOT NULL,
                PuissanceTotal INTEGER NOT NULL,
                Classement INTEGER NULL,
                DonneesEscouadeJson TEXT NOT NULL
            );";
            db.Database.ExecuteSqlRaw(createHistoriquesEscouadeSql);
            Console.WriteLine("[DB] Ensured HistoriquesEscouade table exists.");

            // Hotfix: Ensure 'LucieHouses' table exists
            var createLucieHousesSql = @"CREATE TABLE IF NOT EXISTS LucieHouses (
                Id INTEGER NOT NULL CONSTRAINT PK_LucieHouses PRIMARY KEY AUTOINCREMENT
            );";
            db.Database.ExecuteSqlRaw(createLucieHousesSql);
            Console.WriteLine("[DB] Ensured LucieHouses table exists.");

            // Hotfix: Ensure 'Pieces' table exists
            var createPiecesSql = @"CREATE TABLE IF NOT EXISTS Pieces (
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
            db.Database.ExecuteSqlRaw(createPiecesSql);
            Console.WriteLine("[DB] Ensured Pieces table exists.");
            EnsureLuciePieceAspectColumns();

                // Ensure Profiles table exists
                var createProfilesSql = @"CREATE TABLE IF NOT EXISTS Profiles (
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
                db.Database.ExecuteSqlRaw(createProfilesSql);
                Console.WriteLine("[DB] Ensured Profiles table exists.");

            // Hotfix: Add missing Profile columns if they don't exist
            if (TableExists("Profiles"))
            {
                if (!ColumnExists("Profiles", "Role"))
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Profiles ADD COLUMN Role TEXT NOT NULL DEFAULT 'utilisateur';");
                        Console.WriteLine("[DB] Added Role column to Profiles.");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex)
                    {
                        Console.WriteLine($"[DB] Could not add Role: {ex.Message}");
                    }
                }
                
                if (!ColumnExists("Profiles", "PasswordHash"))
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Profiles ADD COLUMN PasswordHash TEXT NOT NULL DEFAULT '';");
                        Console.WriteLine("[DB] Added PasswordHash column to Profiles.");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex)
                    {
                        Console.WriteLine($"[DB] Could not add PasswordHash: {ex.Message}");
                    }
                }
                
                if (!ColumnExists("Profiles", "PasswordSalt"))
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Profiles ADD COLUMN PasswordSalt TEXT NOT NULL DEFAULT '';");
                        Console.WriteLine("[DB] Added PasswordSalt column to Profiles.");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex)
                    {
                        Console.WriteLine($"[DB] Could not add PasswordSalt: {ex.Message}");
                    }
                }
                
                if (!ColumnExists("Profiles", "HashAlgorithm"))
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Profiles ADD COLUMN HashAlgorithm TEXT NOT NULL DEFAULT 'PBKDF2';");
                        Console.WriteLine("[DB] Added HashAlgorithm column to Profiles.");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex)
                    {
                        Console.WriteLine($"[DB] Could not add HashAlgorithm: {ex.Message}");
                    }
                }
                
                if (!ColumnExists("Profiles", "FailedLoginCount"))
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Profiles ADD COLUMN FailedLoginCount INTEGER NOT NULL DEFAULT 0;");
                        Console.WriteLine("[DB] Added FailedLoginCount column to Profiles.");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex)
                    {
                        Console.WriteLine($"[DB] Could not add FailedLoginCount: {ex.Message}");
                    }
                }
                
                if (!ColumnExists("Profiles", "LockoutUntil"))
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Profiles ADD COLUMN LockoutUntil TEXT NULL;");
                        Console.WriteLine("[DB] Added LockoutUntil column to Profiles.");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex)
                    {
                        Console.WriteLine($"[DB] Could not add LockoutUntil: {ex.Message}");
                    }
                }
            }

            // AppImages table initialization removed (unused)
        }
        // Hotfix: Add missing 'IsAdultModeEnabled' column to AppSettings table if it doesn't exist
        if (TableExists("AppSettings") && !ColumnExists("AppSettings", "IsAdultModeEnabled"))
        {
            try
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE AppSettings ADD COLUMN IsAdultModeEnabled INTEGER NOT NULL DEFAULT 1;");
                Console.WriteLine("[DB] Added IsAdultModeEnabled to AppSettings.");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                Console.WriteLine($"[DB] Could not add IsAdultModeEnabled: {ex.Message}");
            }
        }
        
        // Hotfix: Add missing 'Language' column to AppSettings table if it doesn't exist
        if (TableExists("AppSettings") && !ColumnExists("AppSettings", "Language"))
        {
            try
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE AppSettings ADD COLUMN Language TEXT NOT NULL DEFAULT 'fr';");
                Console.WriteLine("[DB] Added Language to AppSettings.");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                Console.WriteLine($"[DB] Could not add Language: {ex.Message}");
            }
        }
        
        // Removed ThumbnailHeightPx column hotfix (unused)
        
        // Hotfix: Add missing 'ImageUrlHeader' column to Personnages table if it doesn't exist
        if (TableExists("Personnages") && !ColumnExists("Personnages", "ImageUrlHeader"))
        {
            try
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Personnages ADD COLUMN ImageUrlHeader TEXT NOT NULL DEFAULT '';");
                Console.WriteLine("[DB] Added ImageUrlHeader to Personnages.");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                Console.WriteLine($"[DB] Could not add ImageUrlHeader: {ex.Message}");
            }
        }

        // Removed AppImages category column hotfix (unused)

        // Cleanup: Drop legacy AppImages table if present
        if (TableExists("AppImages"))
        {
            try
            {
                db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS AppImages;");
                Console.WriteLine("[DB] Dropped legacy AppImages table.");
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"[DB] Could not drop AppImages: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration error: {ex.Message}");
    }

    // Initialize default AppSettings and images
    try
    {
        var settings = db.AppSettings.OrderBy(x => x.Id).FirstOrDefault();
        if (settings == null)
        {
            var newSettings = new CharacterManager.Server.Models.AppSettings
            {
                IsAdultModeEnabled = true,
                Language = "fr"
            };
            db.AppSettings.Add(newSettings);
            db.SaveChanges();
            Console.WriteLine("[Init] Created default AppSettings - Adult Mode: Enabled, Language: fr");
        }
        else
        {
            // Appliquer les paramètres existants
            var adultModeStatus = settings.IsAdultModeEnabled ? "Enabled" : "Disabled";
            var language = string.IsNullOrEmpty(settings.Language) ? "fr" : settings.Language;
            // Si la langue n'est pas définie, définir par défaut
            if (string.IsNullOrEmpty(settings.Language))
            {
                settings.Language = "fr";
                db.SaveChanges();
                Console.WriteLine("[Init] Updated Language to default: fr");
            }
            Console.WriteLine($"[Init] Loaded AppSettings - Adult Mode: {adultModeStatus}, Language: {language}");
        }

        // Vérification si la base est vide (aucun personnage, template, historique ou profil)
        bool dbIsEmpty = !db.Personnages.Any()
            && !db.Templates.Any()
            && !db.HistoriquesEscouade.Any()
            && !db.Profiles.Any();

        if (dbIsEmpty)
        {
            Console.WriteLine("[Init] La base de données est vide. Préparez l'import d'un fichier .pml pour initialisation.");
            // TODO: Ajouter ici la logique pour demander à l'utilisateur un fichier .pml et lancer l'import
            // Exemple d'appel : await pmlImportService.ImportPmlAsync(stream, "import.pml");
        }

        // NOTE: PersonnageImageConfigService now uses filesystem-based detection
        // Images in /images/personnages/adult/ are automatically treated as adult content
        // No JSON configuration file needed

        // Admin seeding removed - handled dynamically in Login page
        // If no profiles exist, Login page will create admin/admin by default
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Initialization error: {ex.Message}");
    }
}

// Security pipeline
app.UseAuthentication();
app.UseAuthorization();

// Login endpoint for handling authentication (avoids Blazor Server SignalR conflict)
app.MapPost("/api/login", async (HttpContext context, ProfileService profileService) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect("/login?error=required");
        return;
    }

    // Bootstrap default admin if no profiles exist
    var allProfiles = await profileService.GetAllAsync();
    if (allProfiles == null || !allProfiles.Any())
    {
        await profileService.CreateUserAsync("admin", "admin", "admin");
        Console.WriteLine("[Login] No profiles found - created default admin account (admin/admin)");
    }

    var profile = await profileService.GetByUsernameAsync(username);
    if (profile == null)
    {
        await profileService.RegisterLoginAttemptAsync(username, false);
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    if (profile.LockoutUntil.HasValue && profile.LockoutUntil.Value > DateTimeOffset.UtcNow)
    {
        var remaining = (int)(profile.LockoutUntil.Value - DateTimeOffset.UtcNow).TotalMinutes;
        context.Response.Redirect($"/login?error=locked&minutes={remaining}");
        return;
    }

    if (!profileService.VerifyPassword(profile, password))
    {
        await profileService.RegisterLoginAttemptAsync(username, false);
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    await profileService.RegisterLoginAttemptAsync(username, true);

    var claims = new List<System.Security.Claims.Claim>
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, profile.Role)
    };
    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    context.Response.Redirect("/");
});

// Logout endpoint to avoid SignalR response conflicts
app.MapGet("/api/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/login");
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.UseAntiforgery();

// Serve static files from wwwroot (icons, images, css)
app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
