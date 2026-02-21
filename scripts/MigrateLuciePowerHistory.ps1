# Script de migration pour créer rétroactivement l'historique de puissance Lucie
# Ce script génère les enregistrements d'historique de puissance Lucie à partir :
# - Des classements existants (PuissanceLucie)
# - Des modifications de pièces existantes
#
# Usage: .\MigrateLuciePowerHistory.ps1

Write-Host "=== Migration de l'historique de puissance Lucie ===" -ForegroundColor Cyan
Write-Host ""

$projectPath = "D:\Devs\CharacterManager\CharacterManager"
$csprojPath = "$projectPath\CharacterManager.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Host "Erreur: Projet CharacterManager introuvable à $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Compilation du projet..." -ForegroundColor Yellow
dotnet build $csprojPath --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erreur de compilation" -ForegroundColor Red
    exit 1
}

Write-Host "Exécution de la migration..." -ForegroundColor Yellow
Write-Host ""

# Créer un script C# temporaire qui utilise les services
$migrationScript = @"
using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

// Configuration de la base de données
var dbPath = Path.Combine(Environment.CurrentDirectory, "charactermanager.db");
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(`$"Data Source={dbPath}"`));

services.AddLogging(builder => builder.AddConsole());
services.AddScoped<IHistoriqueModificationService, HistoriqueModificationService>();
services.AddScoped<IPersonnageService, PersonnageService>();

var serviceProvider = services.BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var historiqueService = scope.ServiceProvider.GetRequiredService<IHistoriqueModificationService>();
var personnageService = scope.ServiceProvider.GetRequiredService<IPersonnageService>();

Console.WriteLine("Connexion à la base de données: {0}", dbPath);
Console.WriteLine("");

// 1. Générer l'historique à partir des classements
Console.WriteLine("[1/3] Traitement des classements existants...");
var classements = await context.HistoriquesClassement
    .OrderBy(h => h.DateEnregistrement)
    .ToListAsync();

Console.WriteLine("  Trouvé {0} classements", classements.Count);

int classementCount = 0;
foreach (var classement in classements)
{
    var dateClassement = classement.DateEnregistrement.ToDateTime(TimeOnly.MinValue);
    var puissanceLucie = classement.PuissanceLucie;
    
    // Vérifier si un enregistrement existe déjà
    var existeDeja = await context.HistoriquesModifications
        .AnyAsync(h => h.TypeEntite == TypeEntite.Piece
            && h.EntiteId == -1
            && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee
            && h.DateModification.Date == dateClassement.Date);
    
    if (!existeDeja && puissanceLucie > 0)
    {
        await historiqueService.EnregistrerPuissanceLucieAsync(false, puissanceLucie, dateClassement);
        await historiqueService.EnregistrerPuissanceLucieAsync(true, puissanceLucie, dateClassement);
        classementCount++;
        Console.WriteLine("  ✓ {0}: Puissance {1}", classement.DateEnregistrement, puissanceLucie);
    }
}

Console.WriteLine("  → {0} classements traités", classementCount);
Console.WriteLine("");

// 2. Générer l'historique à partir des modifications de pièces
Console.WriteLine("[2/3] Traitement des modifications de pièces...");
var modificationsPieces = await context.HistoriquesModifications
    .Where(h => h.TypeEntite == TypeEntite.Piece
        && h.EntiteId > 0
        && (h.ChampModifie == "AspectsTactiques.Puissance" 
            || h.ChampModifie == "AspectsStrategiques.Puissance"
            || h.ChampModifie == "Selectionnee"))
    .OrderBy(h => h.DateModification)
    .ToListAsync();

Console.WriteLine("  Trouvé {0} modifications de pièces", modificationsPieces.Count);

var joursAvecModifications = modificationsPieces
    .Select(m => m.DateModification.Date)
    .Distinct()
    .OrderBy(d => d)
    .ToList();

Console.WriteLine("  Sur {0} jours distincts", joursAvecModifications.Count);

int jourCount = 0;
foreach (var jour in joursAvecModifications)
{
    // Vérifier si un enregistrement existe déjà
    var existeDeja = await context.HistoriquesModifications
        .AnyAsync(h => h.TypeEntite == TypeEntite.Piece
            && h.EntiteId == -1
            && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee
            && h.DateModification.Date == jour);
    
    if (!existeDeja)
    {
        // Calculer la puissance à cette date (simulation simplifiée)
        // On prend la puissance actuelle comme référence
        var puissanceSelectionnee = personnageService.GetPuissanceLucieEscouade();
        var puissanceMax = personnageService.GetPuissanceMaxLucieEscouade();
        
        if (puissanceSelectionnee > 0 || puissanceMax > 0)
        {
            await historiqueService.EnregistrerPuissanceLucieAsync(false, puissanceSelectionnee, jour);
            await historiqueService.EnregistrerPuissanceLucieAsync(true, puissanceMax, jour);
            jourCount++;
            Console.WriteLine("  ✓ {0:yyyy-MM-dd}: Sélection={1}, Max={2}", jour, puissanceSelectionnee, puissanceMax);
        }
    }
}

Console.WriteLine("  → {0} jours traités", jourCount);
Console.WriteLine("");

// 3. Vérification finale
Console.WriteLine("[3/3] Vérification des enregistrements créés...");
var totalLucieSelectionnee = await context.HistoriquesModifications
    .CountAsync(h => h.TypeEntite == TypeEntite.Piece
        && h.EntiteId == -1
        && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee);

var totalLucieMax = await context.HistoriquesModifications
    .CountAsync(h => h.TypeEntite == TypeEntite.Piece
        && h.EntiteId == -2
        && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieMax);

Console.WriteLine("  Enregistrements PuissanceLucieSelectionnee: {0}", totalLucieSelectionnee);
Console.WriteLine("  Enregistrements PuissanceLucieMax: {0}", totalLucieMax);
Console.WriteLine("");

Console.WriteLine("Migration terminée avec succès !");
"@

$tempFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.csx'
$migrationScript | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "Exécution de la migration avec dotnet-script..." -ForegroundColor Yellow
Write-Host ""

# Vérifier si dotnet-script est installé
$hasScript = dotnet tool list -g | Select-String "dotnet-script"
if (-not $hasScript) {
    Write-Host "Installation de dotnet-script..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-script
}

# Exécuter le script
Push-Location $projectPath
dotnet script $tempFile
$exitCode = $LASTEXITCODE
Pop-Location

Remove-Item $tempFile -ErrorAction SilentlyContinue

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "=== Migration terminée avec succès ===" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=== Migration échouée ===" -ForegroundColor Red
}

exit $exitCode
