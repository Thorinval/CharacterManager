Import-Module PSSQLite

$dbPath = 'd:\Devs\CharacterManager\CharacterManager\charactermanager.db'

Write-Host "=== DIAGNOSTIC LUCIE MODIFICATIONS ==="

# Voir TOUTES les modifications Lucie
$allLucieQuery = @"
SELECT 
    Id,
    EntiteId,
    TypeEntite,
    ChampModifie,
    AncienneValeur,
    NouvelleValeur,
    DateModification,
    TypeModification
FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
ORDER BY DateModification DESC
LIMIT 20
"@

Write-Host "`nTous les modifications Lucie (dernières 20) :"
$result = Invoke-SqliteQuery -DataSource $dbPath -Query $allLucieQuery
$result | Format-Table -AutoSize

# Vérifier le nombre exact avec AncienneValeur=0 (string)
$countString0 = @"
SELECT COUNT(*) as count
FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
  AND AncienneValeur = '0'
"@

Write-Host "`nAvec AncienneValeur = '0' (string):"
$r1 = Invoke-SqliteQuery -DataSource $dbPath -Query $countString0
Write-Host "Count: $($r1.count)"

# Vérifier le nombre exact avec AncienneValeur=0 (int)
$countInt0 = @"
SELECT COUNT(*) as count
FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
  AND AncienneValeur = 0
"@

Write-Host "`nAvec AncienneValeur = 0 (int):"
$r2 = Invoke-SqliteQuery -DataSource $dbPath -Query $countInt0
Write-Host "Count: $($r2.count)"

# Vérifier NULL
$countNull = @"
SELECT COUNT(*) as count
FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
  AND (AncienneValeur IS NULL OR AncienneValeur = '')
"@

Write-Host "`nAvec AncienneValeur NULL ou vide:"
$r3 = Invoke-SqliteQuery -DataSource $dbPath -Query $countNull
Write-Host "Count: $($r3.count)"

# Total Lucie
$countTotal = @"
SELECT COUNT(*) as count
FROM HistoriquesModifications
WHERE EntiteId IN (-1, -2)
"@

Write-Host "`nTotal modifications Lucie:"
$r4 = Invoke-SqliteQuery -DataSource $dbPath -Query $countTotal
Write-Host "Count: $($r4.count)"
