# Script pour nettoyer la base de données
$dbPath = "d:\Devs\CharacterManager\CharacterManager\charactermanager.db"

Write-Host "Nettoyage de la base de données..." -ForegroundColor Yellow

# Utiliser ADO.NET avec Microsoft.Data.Sqlite
Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\9.0.0\System.Data.Common.dll"

try {
    # Créer la connexion manuellement
    $connectionString = "Data Source=$dbPath"
    
    # Exécuter via dotnet
    $sql1 = "DROP TABLE IF EXISTS HistoriquesModifications"
    $sql2 = "DELETE FROM __EFMigrationsHistory WHERE MigrationId = '20260111113147_AddHistoriqueModifications'"
    
    Write-Host "Suppression de la table HistoriquesModifications..."
    dotnet ef dbcontext scaffold "Data Source=$dbPath" Microsoft.EntityFrameworkCore.Sqlite --force --context TempDbContext --output-dir TempModels | Out-Null
    
    # Alternative: créer un script SQL et l'exécuter via DatabaseInitializationService
    Write-Host "`nPour nettoyer manuellement:" -ForegroundColor Cyan
    Write-Host "1. DROP TABLE IF EXISTS HistoriquesModifications;" -ForegroundColor Green
    Write-Host "2. DELETE FROM __EFMigrationsHistory WHERE MigrationId = '20260111113147_AddHistoriqueModifications';" -ForegroundColor Green
}
catch {
    Write-Host "Erreur: $_" -ForegroundColor Red
}
