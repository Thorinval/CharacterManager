using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    public partial class AddTeamPowerTimeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamPowerTimelineRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPower = table.Column<int>(type: "INTEGER", nullable: false),
                    DateInsertion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPowerTimelineRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamPowerTimelineRecords_Date_Type",
                table: "TeamPowerTimelineRecords",
                columns: new[] { "Date", "Type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamPowerTimelineRecords");
        }
    }
}
