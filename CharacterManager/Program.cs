using CharacterManager.Components; 
using CharacterManager.Server.Data;
using CharacterManager.Server.Services; 
using Microsoft.EntityFrameworkCore; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=charactermanager.db"));

builder.Services.AddScoped<PersonnageService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddSingleton<AppVersionService>();
builder.Services.AddHttpClient<UpdateService>();

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration error: {ex.Message}");
    }
}

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
