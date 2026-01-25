using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToPersonnageClassement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoriquesClassement_Personnages_CommandantId",
                table: "HistoriquesClassement");

            migrationBuilder.DropTable(
                name: "HistoriqueClassementAndroides");

            migrationBuilder.DropTable(
                name: "HistoriqueClassementMercenaires");

            migrationBuilder.DropIndex(
                name: "IX_HistoriquesClassement_CommandantId",
                table: "HistoriquesClassement");

            migrationBuilder.DropColumn(
                name: "CommandantId",
                table: "HistoriquesClassement");

            migrationBuilder.CreateTable(
                name: "PersonnagesClassement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdOrigine = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Rarete = table.Column<int>(type: "INTEGER", nullable: false),
                    Niveau = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Rang = table.Column<int>(type: "INTEGER", nullable: false),
                    Puissance = table.Column<int>(type: "INTEGER", nullable: false),
                    HistoriqueClassementAndroideId = table.Column<int>(type: "INTEGER", nullable: true),
                    HistoriqueClassementCommandantId = table.Column<int>(type: "INTEGER", nullable: true),
                    HistoriqueClassementMercenaireId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnagesClassement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementAndroideId",
                        column: x => x.HistoriqueClassementAndroideId,
                        principalTable: "HistoriquesClassement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementCommandantId",
                        column: x => x.HistoriqueClassementCommandantId,
                        principalTable: "HistoriquesClassement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementMercenaireId",
                        column: x => x.HistoriqueClassementMercenaireId,
                        principalTable: "HistoriquesClassement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnagesClassement_HistoriqueClassementAndroideId",
                table: "PersonnagesClassement",
                column: "HistoriqueClassementAndroideId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnagesClassement_HistoriqueClassementCommandantId",
                table: "PersonnagesClassement",
                column: "HistoriqueClassementCommandantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonnagesClassement_HistoriqueClassementMercenaireId",
                table: "PersonnagesClassement",
                column: "HistoriqueClassementMercenaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonnagesClassement");

            migrationBuilder.AddColumn<int>(
                name: "CommandantId",
                table: "HistoriquesClassement",
                type: "INTEGER",
                nullable: true);

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
                name: "IX_HistoriquesClassement_CommandantId",
                table: "HistoriquesClassement",
                column: "CommandantId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueClassementAndroides_HistoriqueClassement1Id",
                table: "HistoriqueClassementAndroides",
                column: "HistoriqueClassement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueClassementMercenaires_MercenairesId",
                table: "HistoriqueClassementMercenaires",
                column: "MercenairesId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriquesClassement_Personnages_CommandantId",
                table: "HistoriquesClassement",
                column: "CommandantId",
                principalTable: "Personnages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
