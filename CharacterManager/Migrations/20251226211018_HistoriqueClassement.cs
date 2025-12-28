using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class HistoriqueClassement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HistoriqueClassementId",
                table: "Pieces",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistoriquesClassement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateEnregistrement = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ligue = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandantId = table.Column<int>(type: "INTEGER", nullable: true),
                    PuissanceTotal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesClassement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriquesClassement_Personnages_CommandantId",
                        column: x => x.CommandantId,
                        principalTable: "Personnages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Classement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Valeur = table.Column<int>(type: "INTEGER", nullable: false),
                    HistoriqueClassementId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classement_HistoriquesClassement_HistoriqueClassementId",
                        column: x => x.HistoriqueClassementId,
                        principalTable: "HistoriquesClassement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoriqueClassementAndroides",
                columns: table => new
                {
                    AndroidesId = table.Column<int>(type: "INTEGER", nullable: false),
                    HistoriqueClassement1Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueClassementAndroides", x => new { x.AndroidesId, x.HistoriqueClassement1Id });
                    table.ForeignKey(
                        name: "FK_HistoriqueClassementAndroides_HistoriquesClassement_HistoriqueClassement1Id",
                        column: x => x.HistoriqueClassement1Id,
                        principalTable: "HistoriquesClassement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoriqueClassementAndroides_Personnages_AndroidesId",
                        column: x => x.AndroidesId,
                        principalTable: "Personnages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoriqueClassementMercenaires",
                columns: table => new
                {
                    HistoriqueClassementId = table.Column<int>(type: "INTEGER", nullable: false),
                    MercenairesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueClassementMercenaires", x => new { x.HistoriqueClassementId, x.MercenairesId });
                    table.ForeignKey(
                        name: "FK_HistoriqueClassementMercenaires_HistoriquesClassement_HistoriqueClassementId",
                        column: x => x.HistoriqueClassementId,
                        principalTable: "HistoriquesClassement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoriqueClassementMercenaires_Personnages_MercenairesId",
                        column: x => x.MercenairesId,
                        principalTable: "Personnages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_HistoriqueClassementId",
                table: "Pieces",
                column: "HistoriqueClassementId");

            migrationBuilder.CreateIndex(
                name: "IX_Classement_HistoriqueClassementId",
                table: "Classement",
                column: "HistoriqueClassementId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueClassementAndroides_HistoriqueClassement1Id",
                table: "HistoriqueClassementAndroides",
                column: "HistoriqueClassement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueClassementMercenaires_MercenairesId",
                table: "HistoriqueClassementMercenaires",
                column: "MercenairesId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesClassement_CommandantId",
                table: "HistoriquesClassement",
                column: "CommandantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_HistoriquesClassement_HistoriqueClassementId",
                table: "Pieces",
                column: "HistoriqueClassementId",
                principalTable: "HistoriquesClassement",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_HistoriquesClassement_HistoriqueClassementId",
                table: "Pieces");

            migrationBuilder.DropTable(
                name: "Classement");

            migrationBuilder.DropTable(
                name: "HistoriqueClassementAndroides");

            migrationBuilder.DropTable(
                name: "HistoriqueClassementMercenaires");

            migrationBuilder.DropTable(
                name: "HistoriquesClassement");

            migrationBuilder.DropIndex(
                name: "IX_Pieces_HistoriqueClassementId",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "HistoriqueClassementId",
                table: "Pieces");
        }
    }
}
