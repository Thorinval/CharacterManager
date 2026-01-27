# Script de nettoyage des modifications Lucie avec AncienneValeur=0
# Supprime les anciennes modifications aberrantes avant la ré-importation

param(
    [string]$DbPath = "d:\Devs\CharacterManager\CharacterManager\charactermanager.db"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Nettoyage Lucie - Modifications aberrantes" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $DbPath)) {
    Write-Host "ERREUR: Base de donnees non trouvee" -ForegroundColor Red
    Write-Host "Chemin: $DbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Base trouvee: $DbPath" -ForegroundColor Green
Write-Host ""

# Charger le driver SQLite
$SqliteDll = Get-ChildItem -Path "d:\Devs\CharacterManager" -Recurse -Filter "System.Data.SQLite.dll" | Select-Object -First 1
if ($SqliteDll) {
    [System.Reflection.Assembly]::LoadFrom($SqliteDll.FullName) | Out-Null
    Write-Host "Driver SQLite charge" -ForegroundColor Green
} else {
    Write-Host "Utilisation du provider SQLite par defaut" -ForegroundColor Yellow
}

$ConnectionString = "Data Source=$DbPath;Cache=Shared"

try {
    $Connection = New-Object System.Data.SQLite.SQLiteConnection($ConnectionString)
    $Connection.Open()
    Write-Host "Connexion etablie avec succes" -ForegroundColor Green
} catch {
    Write-Host "Erreur de connexion: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "--- SCAN AVANT NETTOYAGE ---" -ForegroundColor Yellow

Write-Host ""
Write-Host "--- AVANT NETTOYAGE ---" -ForegroundColor Yellow

# Compter les modifications à supprimer
try {
    $countCommand = $Connection.CreateCommand()
    $countCommand.CommandText = @"
SELECT COUNT(*) as Total,
       SUM(CASE WHEN EntiteId IN (-1, -2) AND AncienneValeur = '0' THEN 1 ELSE 0 END) as Aberrantes
FROM HistoriquesModifications
WHERE TypeEntite = 0  -- TypeEntite.Piece = 0
  AND ChampModifie LIKE '%LuciePuissance%'
"@
    
    $reader = $countCommand.ExecuteReader()
    if ($reader.Read()) {
        $total = $reader["Total"]
        $aberrantes = $reader["Aberrantes"]
        Write-Host "Total modifications Lucie: $total" -ForegroundColor Cyan
        Write-Host "Modifications à supprimer: $aberrantes" -ForegroundColor Yellow
    }
    $reader.Close()
} catch {
    Write-Host "⚠️  Erreur lors du comptage: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Affichage des modifications à supprimer:" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Afficher les détails des modifications à supprimer
try {
    $selectCommand = $Connection.CreateCommand()
    $selectCommand.CommandText = @"
SELECT 
    Id,
    EntiteId,
    ChampModifie,
    AncienneValeur,
    NouvelleValeur,
    DateModification
FROM HistoriquesModifications
WHERE TypeEntite = 0  -- TypeEntite.Piece
  AND EntiteId IN (-1, -2)
  AND AncienneValeur = '0'
  AND (ChampModifie LIKE '%PuissanceLucie%' OR ChampModifie = 'Puissance Lucie sélectionnée' OR ChampModifie = 'Puissance Lucie max')
ORDER BY DateModification DESC
"@
    
    $reader = $selectCommand.ExecuteReader()
    $count = 0
    while ($reader.Read()) {
        $count++
        $id = $reader["Id"]
        $entiteId = $reader["EntiteId"]
        $type = if ($entiteId -eq -1) { "Sélectionnée" } else { "Max" }
        $ancienne = $reader["AncienneValeur"]
        $nouvelle = $reader["NouvelleValeur"]
        $date = $reader["DateModification"]
        
        Write-Host "  [$count] ID=$id | Lucie $type | $ancienne → $nouvelle | $date" -ForegroundColor Gray
    }
    $reader.Close()
    
    if ($count -eq 0) {
        Write-Host "  (Aucune modification à supprimer)" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Erreur lors de la lecture: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

# Demander confirmation
$response = Read-Host "Confirmer la suppression ? (oui/non)"

if ($response -ne "oui" -and $response -ne "o" -and $response -ne "yes" -and $response -ne "y") {
    Write-Host "Opération annulée." -ForegroundColor Yellow
    $Connection.Close()
    exit 0
}

Write-Host ""
Write-Host "Suppression en cours..." -ForegroundColor Yellow

# Supprimer les modifications
try {
    $deleteCommand = $Connection.CreateCommand()
    $deleteCommand.CommandText = @"
DELETE FROM HistoriquesModifications
WHERE TypeEntite = 0  -- TypeEntite.Piece
  AND EntiteId IN (-1, -2)
  AND AncienneValeur = '0'
  AND (ChampModifie LIKE '%PuissanceLucie%' OR ChampModifie = 'Puissance Lucie sélectionnée' OR ChampModifie = 'Puissance Lucie max')
"@
    
    $rowsDeleted = $deleteCommand.ExecuteNonQuery()
    Write-Host "✓ $rowsDeleted modifications supprimées" -ForegroundColor Green
} catch {
    Write-Host "❌ Erreur lors de la suppression: $_" -ForegroundColor Red
    $Connection.Close()
    exit 1
}

Write-Host ""
Write-Host "--- APRÈS NETTOYAGE ---" -ForegroundColor Yellow

# Vérifier après nettoyage
try {
    $verifyCommand = $Connection.CreateCommand()
    $verifyCommand.CommandText = @"
SELECT COUNT(*) as Total
FROM HistoriquesModifications
WHERE TypeEntite = 0  -- TypeEntite.Piece
  AND ChampModifie LIKE '%LuciePuissance%'
"@
    
    $reader = $verifyCommand.ExecuteReader()
    if ($reader.Read()) {
        $totalAfter = $reader["Total"]
        Write-Host "Total modifications Lucie restantes: $totalAfter" -ForegroundColor Cyan
    }
    $reader.Close()
} catch {
    Write-Host "⚠️  Erreur lors de la vérification: $_" -ForegroundColor Yellow
}

$Connection.Close()

Write-Host ""
Write-Host "✓ Nettoyage terminé avec succès!" -ForegroundColor Green
Write-Host ""
Write-Host "Prochaine étape: Ré-importer les classements via l'application" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
