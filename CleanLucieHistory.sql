-- Suppression de tous les enregistrements d'historique pour Lucie (EntiteId = -1 et -2)
-- Ces données ont été créées incorrectement lors de l'insertion des classements
-- et ne représentent pas de véritables modifications de pièces

-- Vérifier combien d'enregistrements seront supprimés
SELECT 
    COUNT(*) as NombreTotal,
    SUM(CASE WHEN EntiteId = -1 THEN 1 ELSE 0 END) as LucieSelect,
    SUM(CASE WHEN EntiteId = -2 THEN 1 ELSE 0 END) as LucieMax
FROM HistoriquesModifications
WHERE TypeEntite = 2 -- TypeEntite.Piece
  AND (EntiteId = -1 OR EntiteId = -2);

-- Suppression des enregistrements
DELETE FROM HistoriquesModifications
WHERE TypeEntite = 2 -- TypeEntite.Piece
  AND (EntiteId = -1 OR EntiteId = -2);

-- Vérification après suppression
SELECT COUNT(*) as RestantApresSupp
FROM HistoriquesModifications
WHERE TypeEntite = 2
  AND (EntiteId = -1 OR EntiteId = -2);
