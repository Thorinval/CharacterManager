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
                name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementId",
                table: "PersonnagesClassement");

            migrationBuilder.RenameColumn(
                name: "HistoriqueClassementId",
                table: "PersonnagesClassement",
                newName: "HistoriqueClassementAndroideId");

            migrationBuilder.RenameIndex(
                name: "IX_PersonnagesClassement_HistoriqueClassementId",
                table: "PersonnagesClassement",
                newName: "IX_PersonnagesClassement_HistoriqueClassementAndroideId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementAndroideId",
                table: "PersonnagesClassement",
                column: "HistoriqueClassementAndroideId",
                principalTable: "HistoriquesClassement",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementAndroideId",
                table: "PersonnagesClassement");

            migrationBuilder.RenameColumn(
                name: "HistoriqueClassementAndroideId",
                table: "PersonnagesClassement",
                newName: "HistoriqueClassementId");

            migrationBuilder.RenameIndex(
                name: "IX_PersonnagesClassement_HistoriqueClassementAndroideId",
                table: "PersonnagesClassement",
                newName: "IX_PersonnagesClassement_HistoriqueClassementId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonnagesClassement_HistoriquesClassement_HistoriqueClassementId",
                table: "PersonnagesClassement",
                column: "HistoriqueClassementId",
                principalTable: "HistoriquesClassement",
                principalColumn: "Id");
        }
    }
}
