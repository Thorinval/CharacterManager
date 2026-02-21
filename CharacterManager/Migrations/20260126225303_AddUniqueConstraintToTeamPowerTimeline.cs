using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterManager.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToTeamPowerTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Try to drop the existing non-unique index if it exists (safe for both fresh DB and existing DB)
            try
            {
                migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TeamPowerTimelineRecords_Date_Type;");
            }
            catch
            {
                // Ignore if index doesn't exist
            }

            // Recreate as unique index
            migrationBuilder.CreateIndex(
                name: "IX_TeamPowerTimelineRecords_Date_Type",
                table: "TeamPowerTimelineRecords",
                columns: new[] { "Date", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TeamPowerTimelineRecords_Date_Type;");

            // Recreate as non-unique index (safe only if table exists)
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS IX_TeamPowerTimelineRecords_Date_Type ON TeamPowerTimelineRecords(Date, Type);");
        }
    }
}
