using CharacterManager.Components; 
using CharacterManager.Server.Data;
using CharacterManager.Server.Services; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
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

builder.Services.AddScoped<PersonnageService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<HistoriqueEscouadeService>();
builder.Services.AddScoped<ClientLocalizationService>();
// AppImageService no longer used for categorization; DI registration removed
builder.Services.AddSingleton<PersonnageImageConfigService>();
builder.Services.AddSingleton<AppVersionService>();
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddHttpClient<UpdateService>();
builder.Services.AddHttpClient();  // Pour les appels HTTP du ClientLocalizationService
builder.Services.AddScoped<ProfileService>();

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
    try
    {
        // Get pending migrations
        var pendingMigrations = db.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            // Apply pending migrations
            db.Database.Migrate();
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

                // Ensure Profiles table exists
                var createProfilesSql = @"CREATE TABLE IF NOT EXISTS Profiles (
                    Id INTEGER NOT NULL CONSTRAINT PK_Profiles PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    AdultMode INTEGER NOT NULL DEFAULT 0,
                    Language TEXT NOT NULL DEFAULT 'fr'
                );";
                db.Database.ExecuteSqlRaw(createProfilesSql);
                Console.WriteLine("[DB] Ensured Profiles table exists.");

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
        var settings = db.AppSettings.FirstOrDefault();
        if (settings == null)
        {
            var newSettings = new CharacterManager.Server.Models.AppSettings
            {
                IsAdultModeEnabled = true
            };
            db.AppSettings.Add(newSettings);
            db.SaveChanges();
            Console.WriteLine("[Init] Created default AppSettings.");
        }

        // Initialize default images removed (switch to personnages-config.json)

        // Initialize personnages image configuration
        var configService = scope.ServiceProvider.GetRequiredService<PersonnageImageConfigService>();
        configService.LoadConfiguration();
        Console.WriteLine("[Init] Personnages image configuration loaded.");

        // Seed default admin from configuration
        var adminUser = app.Configuration["Admin:Username"] ?? "admin";
        var adminPass = app.Configuration["Admin:Password"] ?? "admin";
        var profileService = scope.ServiceProvider.GetRequiredService<ProfileService>();
        if (!db.Profiles.Any(p => p.Username == adminUser))
        {
            await profileService.CreateUserAsync(adminUser, adminPass, "admin");
            Console.WriteLine($"[Init] Seeded admin user '{adminUser}'.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Initialization error: {ex.Message}");
    }
}

// Security pipeline
app.UseAuthentication();
app.UseAuthorization();

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
