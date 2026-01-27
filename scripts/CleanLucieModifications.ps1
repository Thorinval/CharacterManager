# Script pour nettoyer les modifications Lucie avec AncienneValeur=0

$dbPath = "$PSScriptRoot\..\CharacterManager\charactermanager.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "Erreur: Base de donnees non trouvee: $dbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Nettoyage des modifications Lucie avec AncienneValeur=0..." -ForegroundColor Cyan
Write-Host "Base de donnees: $dbPath" -ForegroundColor Gray

# Importer le module SQLite
Install-Module -Name PSSQLite -Force -Scope CurrentUser -SkipPublisherCheck | Out-Null
Import-Module PSSQLite

# Requete pour compter les modifications a supprimer
$countQuery = @"
SELECT COUNT(*) as count
FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
  AND TypeEntite = 0
  AND (ChampModifie = 'PuissanceLucieSelectionnee' OR ChampModifie = 'PuissanceLucieMax')
  AND AncienneValeur = '0'
"@

# Requete pour supprimer les modifications
$deleteQuery = @"
DELETE FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
  AND TypeEntite = 0
  AND (ChampModifie = 'PuissanceLucieSelectionnee' OR ChampModifie = 'PuissanceLucieMax')
  AND AncienneValeur = '0'
"@

try {
    Write-Host "Verification en cours..." -ForegroundColor Yellow
    
    # Compter d'abord
    $result = Invoke-SqliteQuery -DataSource $dbPath -Query $countQuery
    $countToDelete = $result.count
    
    if ($countToDelete -eq 0) {
        Write-Host "OK: Aucune modification Lucie a supprimer" -ForegroundColor Green
        exit 0
    }
    
    Write-Host "Nombre de modifications a supprimer: $countToDelete" -ForegroundColor Yellow
    
    # Confirmation
    $response = Read-Host "Etes-vous sur ? (oui/non)"
    if ($response -ne "oui") {
        Write-Host "Annule" -ForegroundColor Red
        exit 0
    }
    
    Write-Host "Suppression en cours..." -ForegroundColor Yellow
    
    # Executer la suppression
    Invoke-SqliteQuery -DataSource $dbPath -Query $deleteQuery | Out-Null
    
    # Verifier le resultat
    $verification = Invoke-SqliteQuery -DataSource $dbPath -Query $countQuery
    $remaining = $verification.count
    
    Write-Host "OK: Suppression effectuee!" -ForegroundColor Green
    Write-Host "Modifications restantes: $remaining" -ForegroundColor Green
    
} catch {
    Write-Host "Erreur lors du nettoyage: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Script termine" -ForegroundColor Green
