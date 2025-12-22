using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastImportedFileName = table.Column<string>(type: "TEXT", nullable: false),
                    LastImportedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAdultModeEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoriquesEscouade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateEnregistrement = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PuissanceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Classement = table.Column<int>(type: "INTEGER", nullable: true),
                    DonneesEscouadeJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesEscouade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LucieHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LucieHouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personnages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Rarete = table.Column<int>(type: "INTEGER", nullable: false),
                    Niveau = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Rang = table.Column<int>(type: "INTEGER", nullable: false),
                    Puissance = table.Column<int>(type: "INTEGER", nullable: false),
                    Selectionne = table.Column<bool>(type: "INTEGER", nullable: false),
                    PA = table.Column<int>(type: "INTEGER", nullable: false),
                    PV = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Faction = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrlDetail = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrlPreview = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrlSelected = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrlHeader = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TypeAttaque = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    AdultMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", nullable: false),
                    HashAlgorithm = table.Column<string>(type: "TEXT", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LockoutUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PuissanceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PersonnagesJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pieces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Niveau = table.Column<int>(type: "INTEGER", nullable: false),
                    Selectionnee = table.Column<bool>(type: "INTEGER", nullable: false),
                    AspectsTactiques = table.Column<string>(type: "TEXT", nullable: false),
                    AspectsStrategiques = table.Column<string>(type: "TEXT", nullable: false),
                    Puissance = table.Column<int>(type: "INTEGER", nullable: false),
                    BonusTactiques = table.Column<string>(type: "TEXT", nullable: false),
                    BonusStrategiques = table.Column<string>(type: "TEXT", nullable: false),
                    LucieHouseId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pieces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pieces_LucieHouses_LucieHouseId",
                        column: x => x.LucieHouseId,
                        principalTable: "LucieHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Capacites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: false),
                    PersonnageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capacites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capacites_Personnages_PersonnageId",
                        column: x => x.PersonnageId,
                        principalTable: "Personnages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capacites_PersonnageId",
                table: "Capacites",
                column: "PersonnageId");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_LucieHouseId",
                table: "Pieces",
                column: "LucieHouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Capacites");

            migrationBuilder.DropTable(
                name: "HistoriquesEscouade");

            migrationBuilder.DropTable(
                name: "Pieces");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "RoadmapNotes");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Personnages");

            migrationBuilder.DropTable(
                name: "LucieHouses");
        }
    }
}
