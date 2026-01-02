using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPuissanceTotaleToHistoriqueClassement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PuissanceTotal",
                table: "HistoriquesClassement",
                newName: "PuissanceTotale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PuissanceTotale",
                table: "HistoriquesClassement",
                newName: "PuissanceTotal");
        }
    }
}
