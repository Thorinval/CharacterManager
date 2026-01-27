-- Script de nettoyage des modifications Lucie avec AncienneValeur='0'
-- Supprime les anciennes modifications aberrantes avant la ré-importation

-- 1. Afficher les modifications à supprimer
SELECT 'AVANT NETTOYAGE' as Phase, COUNT(*) as Total
FROM HistoriquesModifications
WHERE TypeEntite = 0  
  AND EntiteId IN (-1, -2)
  AND AncienneValeur = '0';

SELECT 'Modifications Lucie a supprimer:' as Info,
       Id,
       CASE WHEN EntiteId = -1 THEN 'Selectionnee' ELSE 'Max' END as Type,
       AncienneValeur,
       NouvelleValeur,
       DateModification
FROM HistoriquesModifications
WHERE TypeEntite = 0  
  AND EntiteId IN (-1, -2)
  AND AncienneValeur = '0'
ORDER BY DateModification DESC;

-- 2. Supprimer les modifications aberrantes
DELETE FROM HistoriquesModifications
WHERE TypeEntite = 0  
  AND EntiteId IN (-1, -2)
  AND AncienneValeur = '0';

-- 3. Afficher le résultat
SELECT 'APRES NETTOYAGE' as Phase, COUNT(*) as Total
FROM HistoriquesModifications
WHERE TypeEntite = 0  
  AND EntiteId IN (-1, -2);

-- 4. Vérification finale
SELECT 'Resume' as Info,
       COUNT(*) as ModificationsLucieRestantes
FROM HistoriquesModifications
WHERE TypeEntite = 0  
  AND EntiteId IN (-1, -2);
