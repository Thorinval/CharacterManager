using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultTimestampsForHistorique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ajouter DEFAULT CURRENT_TIMESTAMP pour DateInsertion et DateMiseAJour
            // Pour SQLite, on doit recréer la table
            migrationBuilder.Sql(@"
                -- Créer une table temporaire avec le même schéma
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
                    DateInsertion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    DateMiseAJour TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                
                -- Copier les données de l'ancienne table
                INSERT INTO HistoriquesModifications_Temp 
                SELECT Id, TypeEntite, EntiteId, NomEntite, TypeModification, DateModification, 
                       ChampModifie, AncienneValeur, NouvelleValeur, Description, DateInsertion, DateMiseAJour
                FROM HistoriquesModifications;
                
                -- Supprimer l'ancienne table
                DROP TABLE HistoriquesModifications;
                
                -- Renommer la table temporaire
                ALTER TABLE HistoriquesModifications_Temp RENAME TO HistoriquesModifications;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Pour Down, on ne modifie rien car c'est juste des defaults
            // Les données restent inchangées
        }
    }
}
