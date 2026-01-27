Import-Module PSSQLite

$dbPath = 'd:\Devs\CharacterManager\CharacterManager\charactermanager.db'

Write-Host "Suppression des modifications Lucie avec AncienneValeur=0..."

$deleteQuery = @"
DELETE FROM HistoriquesModifications 
WHERE EntiteId IN (-1, -2) 
  AND AncienneValeur = 0
"@

Invoke-SqliteQuery -DataSource $dbPath -Query $deleteQuery | Out-Null

$countQuery = @"
SELECT COUNT(*) as remaining 
FROM HistoriquesModifications 
WHERE EntiteId IN (-1, -2) 
  AND AncienneValeur = 0
"@

$result = Invoke-SqliteQuery -DataSource $dbPath -Query $countQuery
Write-Host "Modifications restantes avec AncienneValeur=0: $($result.remaining)"

