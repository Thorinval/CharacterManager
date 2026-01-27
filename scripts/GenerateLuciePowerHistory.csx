#r "nuget: Microsoft.Extensions.DependencyInjection, 9.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 9.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.Sqlite, 9.0.0"
#r "nuget: Microsoft.Extensions.Configuration.UserSecrets, 9.0.0"
#r "..\\CharacterManager\\bin\\Debug\\net9.0\\CharacterManager.dll"

using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

try
{
    await RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine("❌ Erreur fatale avant exécution:");
    Console.WriteLine(ex.ToString());
    Environment.Exit(1);
}

async Task RunAsync()
{
    // Configuration de l'encodage console
    Console.OutputEncoding = Encoding.UTF8;

    Console.WriteLine("=== Génération de l'historique de puissance Lucie ===");
    Console.WriteLine();

    // Configuration des services
    var services = new ServiceCollection();

    // Chemin vers la base de données
    var dbPath = Path.Combine(Environment.CurrentDirectory, "charactermanager.db");
    Console.WriteLine($"Base de données: {dbPath}");

    if (!File.Exists(dbPath))
    {
        Console.WriteLine("❌ Base de données introuvable!");
        Environment.Exit(1);
    }

    // Configuration de la base de données SQLite
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

    // Configuration des services
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    services.AddScoped<IHistoriqueModificationService, HistoriqueModificationService>();
    services.AddScoped<IPersonnageService, PersonnageService>();
    services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();

    var serviceProvider = services.BuildServiceProvider();

    using var scope = serviceProvider.CreateScope();
    var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    var historiqueService = scope.ServiceProvider.GetRequiredService<IHistoriqueModificationService>();
    var personnageService = scope.ServiceProvider.GetRequiredService<IPersonnageService>();

    Console.WriteLine();
    Console.WriteLine("Début de la génération...");
    Console.WriteLine();

    try
    {
        var (classementsTraites, joursTraites) = await dbInitService.GenerateLuciePowerHistoryAsync(historiqueService, personnageService);
        
        Console.WriteLine();
        Console.WriteLine("=== Résumé ===");
        Console.WriteLine($"✓ Classements traités: {classementsTraites}");
        Console.WriteLine($"✓ Jours de modifications traités: {joursTraites}");
        Console.WriteLine();
        Console.WriteLine("✓ Génération terminée avec succès!");
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("❌ Erreur lors de l'exécution du script:");
        Console.WriteLine(ex.ToString());
        Environment.Exit(1);
    }
}
