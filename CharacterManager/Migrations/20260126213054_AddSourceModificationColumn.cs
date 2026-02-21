using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceModificationColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "HistoriquesModifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "HistoriquesModifications");
        }
    }
}
