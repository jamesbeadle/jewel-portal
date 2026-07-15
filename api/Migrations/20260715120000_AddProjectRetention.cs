using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // One row per project: client-retention terms (e.g. 5% / 2.5% / 12 months) plus the
    // confirmed state of the two release milestones. Forecast amounts and due dates are
    // calculated from valuation figures, never stored.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260715120000_AddProjectRetention")]
    public partial class AddProjectRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectRetentions",
                columns: table => new
                {
                    ProjectRetentionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CompletionReleasePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefectsPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    PracticalCompletionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionReleaseConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionReleaseAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FinalReleaseConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalReleaseAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRetentions", x => x.ProjectRetentionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRetentions_ProjectId",
                table: "ProjectRetentions",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProjectRetentions");
        }
    }
}
