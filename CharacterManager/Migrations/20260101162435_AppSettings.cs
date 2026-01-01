using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriquesEscouade");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastExportDate",
                table: "AppSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastExportDate",
                table: "AppSettings");

            migrationBuilder.CreateTable(
                name: "HistoriquesEscouade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Classement = table.Column<int>(type: "INTEGER", nullable: true),
                    DateEnregistrement = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DonneesEscouadeJson = table.Column<string>(type: "TEXT", nullable: false),
                    PuissanceTotal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesEscouade", x => x.Id);
                });
        }
    }
}
