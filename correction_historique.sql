-- 1. Met à jour la source des modifications importées (NonSpécifiée → ImportClassement)
UPDATE HistoriquesModifications
SET Source = 3
WHERE Source = 0
  AND EstImportation = 1;

-- 2. Corrige les dates d'insertion/mise à jour pour les imports du 04/12 (adaptez la date si besoin)
UPDATE HistoriquesModifications
SET DateInsertion = '2026-01-27 00:00:00',
    DateMiseAJour = '2026-01-27 00:00:00'
WHERE EstImportation = 1
  AND DateModification BETWEEN '2025-12-04 00:00:00' AND '2025-12-04 23:59:59';

-- 3. Normalise les noms d'entité en majuscules
UPDATE HistoriquesModifications
SET NomEntite = UPPER(NomEntite);

-- 4. (Optionnel) Supprime les doublons exacts (même TypeEntite, EntiteId, ChampModifie, DateModification, NouvelleValeur)
DELETE FROM HistoriquesModifications
WHERE Id NOT IN (
    SELECT MIN(Id)
    FROM HistoriquesModifications
    GROUP BY TypeEntite, EntiteId, ChampModifie, DateModification, NouvelleValeur
);

-- 5. Vérification rapide
SELECT Source, COUNT(*) FROM HistoriquesModifications GROUP BY Source;
SELECT NomEntite, COUNT(*) FROM HistoriquesModifications GROUP BY NomEntite HAVING COUNT(*) > 1;
