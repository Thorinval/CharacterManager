using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddDatesToHistoriqueModifications : Migration
    {
        private const string TableName = "HistoriquesModifications";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateInsertion",
                table: TableName,
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 24, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateMiseAJour",
                table: TableName,
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 24, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateInsertion",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "DateMiseAJour",
                table: TableName);
        }
    }
}
