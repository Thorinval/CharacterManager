using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateHistoriqueModificationsPerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cette migration consolide les enregistrements du même jour pour le même personnage/champ
            // Elle garde le premier enregistrement et met à jour sa nouvelle valeur avec la dernière du jour
            
            migrationBuilder.Sql(@"
-- Créer une table temporaire avec les enregistrements consolidés
CREATE TABLE HistoriquesModifications_Temp (
    Id INTEGER PRIMARY KEY,
    TypeEntite INTEGER NOT NULL,
    EntiteId INTEGER NOT NULL,
    NomEntite TEXT NOT NULL,
    TypeModification INTEGER NOT NULL,
    DateModification TEXT NOT NULL,
    ChampModifie TEXT,
    AncienneValeur TEXT,
    NouvelleValeur TEXT,
    Description TEXT,
    DateInsertion TEXT NOT NULL,
    DateMiseAJour TEXT NOT NULL
);

-- Insérer les enregistrements consolidés
INSERT INTO HistoriquesModifications_Temp
SELECT 
    MIN(h1.Id) as Id,
    h1.TypeEntite,
    h1.EntiteId,
    h1.NomEntite,
    h1.TypeModification,
    COALESCE((SELECT MAX(DateModification) FROM HistoriquesModifications h2 
     WHERE h2.TypeEntite = h1.TypeEntite 
     AND h2.EntiteId = h1.EntiteId
     AND h2.NomEntite = h1.NomEntite
     AND h2.TypeModification = h1.TypeModification
     AND h2.ChampModifie = h1.ChampModifie
     AND DATE(h2.DateModification) = DATE(h1.DateModification)), h1.DateModification) as DateModification,
    h1.ChampModifie,
    h1.AncienneValeur,
    (SELECT NouvelleValeur FROM HistoriquesModifications h2 
     WHERE h2.TypeEntite = h1.TypeEntite 
     AND h2.EntiteId = h1.EntiteId
     AND h2.NomEntite = h1.NomEntite
     AND h2.TypeModification = h1.TypeModification
     AND h2.ChampModifie = h1.ChampModifie
     AND DATE(h2.DateModification) = DATE(h1.DateModification)
     ORDER BY h2.DateModification DESC LIMIT 1) as NouvelleValeur,
    h1.Description,
    COALESCE((SELECT MIN(DateModification) FROM HistoriquesModifications h2 
     WHERE h2.TypeEntite = h1.TypeEntite 
     AND h2.EntiteId = h1.EntiteId
     AND h2.NomEntite = h1.NomEntite
     AND h2.TypeModification = h1.TypeModification
     AND h2.ChampModifie = h1.ChampModifie
     AND DATE(h2.DateModification) = DATE(h1.DateModification)), h1.DateModification) as DateInsertion,
    COALESCE((SELECT MAX(DateModification) FROM HistoriquesModifications h2 
     WHERE h2.TypeEntite = h1.TypeEntite 
     AND h2.EntiteId = h1.EntiteId
     AND h2.NomEntite = h1.NomEntite
     AND h2.TypeModification = h1.TypeModification
     AND h2.ChampModifie = h1.ChampModifie
     AND DATE(h2.DateModification) = DATE(h1.DateModification)), h1.DateModification) as DateMiseAJour
FROM HistoriquesModifications h1
GROUP BY 
    h1.TypeEntite,
    h1.EntiteId,
    h1.NomEntite,
    h1.TypeModification,
    DATE(h1.DateModification),
    h1.ChampModifie;

-- Supprimer tous les enregistrements originaux
DELETE FROM HistoriquesModifications;

-- Insérer les enregistrements consolidés
INSERT INTO HistoriquesModifications (Id, TypeEntite, EntiteId, NomEntite, TypeModification, DateModification, ChampModifie, AncienneValeur, NouvelleValeur, Description, DateInsertion, DateMiseAJour)
SELECT Id, TypeEntite, EntiteId, NomEntite, TypeModification, DateModification, ChampModifie, AncienneValeur, NouvelleValeur, Description, DateInsertion, DateMiseAJour
FROM HistoriquesModifications_Temp;

-- Supprimer la table temporaire
DROP TABLE HistoriquesModifications_Temp;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cette migration ne peut pas être annulée facilement car elle consolide les données
            // L'utilisateur devrait restaurer à partir de la sauvegarde s'il veut annuler
            migrationBuilder.Sql("-- Migration de consolidation ne peut pas être annulée. Restaurez la sauvegarde si nécessaire.");
        }
    }
}
