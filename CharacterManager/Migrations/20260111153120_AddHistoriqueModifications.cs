using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoriqueModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoriquesModifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeEntite = table.Column<int>(type: "INTEGER", nullable: false),
                    EntiteId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomEntite = table.Column<string>(type: "TEXT", nullable: false),
                    TypeModification = table.Column<int>(type: "INTEGER", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChampModifie = table.Column<string>(type: "TEXT", nullable: true),
                    AncienneValeur = table.Column<string>(type: "TEXT", nullable: true),
                    NouvelleValeur = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesModifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesModifications_DateModification",
                table: "HistoriquesModifications",
                column: "DateModification");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriquesModifications");
        }
    }
}
