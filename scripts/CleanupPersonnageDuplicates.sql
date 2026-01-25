-- Script de nettoyage des doublons dans la table Personnages
-- Garde l'entrée avec l'ID le plus petit (la plus ancienne) et met à jour les références

-- 1. Identifier les doublons (noms identiques en ignorant la casse)
WITH Duplicates AS (
    SELECT 
        UPPER(Nom) as NomUpper,
        MIN(Id) as IdToKeep,
        GROUP_CONCAT(Id) as AllIds,
        COUNT(*) as DuplicateCount
    FROM Personnages
    GROUP BY UPPER(Nom)
    HAVING COUNT(*) > 1
),
IdsToDelete AS (
    SELECT 
        p.Id as IdToDelete,
        d.IdToKeep,
        p.Nom as NomToDelete
    FROM Personnages p
    INNER JOIN Duplicates d ON UPPER(p.Nom) = d.NomUpper
    WHERE p.Id != d.IdToKeep
)
SELECT 
    IdToDelete,
    IdToKeep,
    NomToDelete,
    (SELECT Nom FROM Personnages WHERE Id = IdToKeep) as NomToKeep
FROM IdsToDelete;

-- 2. Mettre à jour les références dans HistoriquesModifications (sans EXISTS)
WITH DuplicateMap AS (
    SELECT 
        p.Id AS IdToDelete,
        MIN(p2.Id) AS IdToKeep
    FROM Personnages p
    JOIN Personnages p2 ON UPPER(p2.Nom) = UPPER(p.Nom)
    GROUP BY p.Id
    HAVING MIN(p2.Id) <> p.Id
)
UPDATE HistoriquesModifications
SET EntiteId = (
    SELECT dm.IdToKeep
    FROM DuplicateMap dm
    WHERE dm.IdToDelete = HistoriquesModifications.EntiteId
)
WHERE TypeEntite = 0 -- TypeEntite.Personnage
AND EntiteId IN (SELECT IdToDelete FROM DuplicateMap);

-- 3. Mettre à jour les références dans Templates (PersonnageIdsJson)
-- Note: SQLite ne supporte pas JSON_MODIFY directement, il faudra le faire en C#
-- Cette partie sera gérée par du code C#

-- 4. Supprimer les doublons
DELETE FROM Personnages
WHERE Id IN (
    SELECT p.Id
    FROM Personnages p
    INNER JOIN (
        SELECT 
            UPPER(Nom) as NomUpper,
            MIN(Id) as IdToKeep
        FROM Personnages
        GROUP BY UPPER(Nom)
        HAVING COUNT(*) > 1
    ) d ON UPPER(p.Nom) = d.NomUpper AND p.Id != d.IdToKeep
);

-- 5. Vérifier qu'il n'y a plus de doublons
SELECT 
    UPPER(Nom) as Nom,
    COUNT(*) as Count,
    GROUP_CONCAT(Id) as Ids
FROM Personnages
GROUP BY UPPER(Nom)
HAVING COUNT(*) > 1;
