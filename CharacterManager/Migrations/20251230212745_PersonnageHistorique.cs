using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class PersonnageHistorique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_HistoriquesClassement_HistoriqueClassementId",
                table: "Pieces");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Pieces",
                type: "TEXT",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IdOrigine",
                table: "Pieces",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Personnages",
                type: "TEXT",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HistoriqueClassementId",
                table: "Personnages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdOrigine",
                table: "Personnages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_HistoriquesClassement_HistoriqueClassementId",
                table: "Pieces",
                column: "HistoriqueClassementId",
                principalTable: "HistoriquesClassement",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_HistoriquesClassement_HistoriqueClassementId",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "IdOrigine",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Personnages");

            migrationBuilder.DropColumn(
                name: "HistoriqueClassementId",
                table: "Personnages");

            migrationBuilder.DropColumn(
                name: "IdOrigine",
                table: "Personnages");

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_HistoriquesClassement_HistoriqueClassementId",
                table: "Pieces",
                column: "HistoriqueClassementId",
                principalTable: "HistoriquesClassement",
                principalColumn: "Id");
        }
    }
}
