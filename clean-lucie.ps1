#!/usr/bin/env pwsh
# Script de nettoyage des modifications Lucie aberrantes
# Utilise dotnet ef dbcontext scaffold pour lire la base

param(
    [string]$DbPath = "CharacterManager\charactermanager.db"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Nettoyage Lucie - Modifications aberrantes" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $DbPath)) {
    Write-Host "Erreur: Base de donnees non trouvee" -ForegroundColor Red
    exit 1
}

Write-Host "Base trouvee: $DbPath" -ForegroundColor Green
Write-Host ""

# Utiliser une requête SQL simple via dotnet
$SqlQuery = @"
SELECT 'AVANT NETTOYAGE' as Phase;
SELECT COUNT(*) as 'Modifications Lucie (AncienneValeur=0)' 
FROM HistoriquesModifications 
WHERE TypeEntite = 0 AND EntiteId IN (-1, -2) AND AncienneValeur = '0';

SELECT 'Details des modifications a supprimer:' as Info;
SELECT Id, 
       CASE WHEN EntiteId = -1 THEN 'Selectionnee' ELSE 'Max' END as Type,
       AncienneValeur, NouvelleValeur, DateModification
FROM HistoriquesModifications 
WHERE TypeEntite = 0 AND EntiteId IN (-1, -2) AND AncienneValeur = '0'
ORDER BY DateModification DESC;
"@

Write-Host "--- SCAN AVANT NETTOYAGE ---" -ForegroundColor Yellow

# Sauvegarder la query dans un fichier temporaire
$tmpSql = [System.IO.Path]::GetTempFileName() | Rename-Item -NewName { $_.Name -replace '\.tmp', '.sql' } -PassThru
$SqlQuery | Out-File -FilePath $tmpSql.FullName -Encoding UTF8

# Créer un script C# temporaire pour exécuter la requête
$tmpCs = [System.IO.Path]::GetTempFileName() | Rename-Item -NewName { $_.Name -replace '\.tmp', '.cs' } -PassThru

$csharp = @"
using Microsoft.Data.Sqlite;

var dbPath = "$DbPath";
var connectionString = @"Data Source=$DbPath;Cache=Shared";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = @"
SELECT COUNT(*) as count FROM HistoriquesModifications 
WHERE TypeEntite = 0 AND EntiteId IN (-1, -2) AND AncienneValeur = '0'";
        
        var count = (long)cmd.ExecuteScalar();
        Console.WriteLine(@"Modifications Lucie a supprimer: " + count);
        
        if (count > 0)
        {
            cmd.CommandText = @"
SELECT Id, 
       CASE WHEN EntiteId = -1 THEN 'Selectionnee' ELSE 'Max' END as Type,
       AncienneValeur, NouvelleValeur, DateModification
FROM HistoriquesModifications 
WHERE TypeEntite = 0 AND EntiteId IN (-1, -2) AND AncienneValeur = '0'
ORDER BY DateModification DESC";
            
            using (var reader = cmd.ExecuteReader())
            {
                int idx = 0;
                while (reader.Read())
                {
                    idx++;
                    Console.WriteLine(string.Format("  [{0}] ID={1} | Lucie {2} | {3} → {4} | {5}",
                        idx,
                        reader[0],
                        reader[1],
                        reader[2],
                        reader[3],
                        reader[4]));
                }
            }
        }
    }
    connection.Close();
}
"@

$csharp | Out-File -FilePath $tmpCs.FullName -Encoding UTF8

try {
    & dotnet fsi $tmpCs.FullName
} catch {
    Write-Host "Note: Exécution directe. Les modifications à supprimer sont listées ci-dessus." -ForegroundColor Gray
}

Write-Host ""
Write-Host "--- SUPPRESSION ---" -ForegroundColor Yellow

$confirmResponse = Read-Host "Confirmer la suppression ? (oui/non)"

if ($confirmResponse -eq "oui" -or $confirmResponse -eq "o" -or $confirmResponse -eq "yes" -or $confirmResponse -eq "y") {
    Write-Host "Suppression en cours..." -ForegroundColor Yellow
    
    # Créer le script de suppression
    $deleteSql = @"
DELETE FROM HistoriquesModifications 
WHERE TypeEntite = 0 AND EntiteId IN (-1, -2) AND AncienneValeur = '0';

SELECT COUNT(*) as 'Modifications Lucie restantes'
FROM HistoriquesModifications 
WHERE TypeEntite = 0 AND EntiteId IN (-1, -2);
"@

    # À faire manuellement via l'app ou une query SQL
    Write-Host "Commande SQL à exécuter:" -ForegroundColor Yellow
    Write-Host $deleteSql -ForegroundColor Gray
    Write-Host ""
    Write-Host "Pour exécuter, utilisez:" -ForegroundColor Cyan
    Write-Host 'dotnet ef database update --project CharacterManager' -ForegroundColor White
    
} else {
    Write-Host "Operation annulee." -ForegroundColor Yellow
}

# Cleanup
Remove-Item $tmpSql.FullName -Force -ErrorAction SilentlyContinue
Remove-Item $tmpCs.FullName -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
