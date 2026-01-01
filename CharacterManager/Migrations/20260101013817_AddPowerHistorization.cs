using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerHistorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PuissanceCommandant",
                table: "HistoriquesClassement",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PuissanceLucie",
                table: "HistoriquesClassement",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PuissanceMercenaires",
                table: "HistoriquesClassement",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuissanceCommandant",
                table: "HistoriquesClassement");

            migrationBuilder.DropColumn(
                name: "PuissanceLucie",
                table: "HistoriquesClassement");

            migrationBuilder.DropColumn(
                name: "PuissanceMercenaires",
                table: "HistoriquesClassement");
        }
    }
}
