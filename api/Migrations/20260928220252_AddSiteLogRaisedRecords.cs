using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The records a worker's daily log raises beside its note (2026-09-28): a Site Instruction
    /// carries who gave it on site, on which working day and whether it was verbal, and both a
    /// Site Instruction and a defect carry the day's note (progress update) they came off and
    /// whose log it was. Additive only: nullable or defaulted columns and two indexes, no data
    /// changes. Safe to apply before or with the deploy. Scoped script:
    /// api/Migrations/add-site-log-raised-records.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260928220252_AddSiteLogRaisedRecords")]
    public partial class AddSiteLogRaisedRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "GivenBy", table: "SiteInstructions", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTimeOffset>(name: "GivenOn", table: "SiteInstructions", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsVerbal", table: "SiteInstructions", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "ProgressUpdateId", table: "SiteInstructions", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "RaisedByEmail", table: "SiteInstructions", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ProgressUpdateId", table: "Defects", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "RaisedByEmail", table: "Defects", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.CreateIndex(name: "IX_SiteInstructions_ProgressUpdateId", table: "SiteInstructions", column: "ProgressUpdateId");
            migrationBuilder.CreateIndex(name: "IX_Defects_ProgressUpdateId", table: "Defects", column: "ProgressUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_SiteInstructions_ProgressUpdateId", table: "SiteInstructions");
            migrationBuilder.DropIndex(name: "IX_Defects_ProgressUpdateId", table: "Defects");
            migrationBuilder.DropColumn(name: "GivenBy", table: "SiteInstructions");
            migrationBuilder.DropColumn(name: "GivenOn", table: "SiteInstructions");
            migrationBuilder.DropColumn(name: "IsVerbal", table: "SiteInstructions");
            migrationBuilder.DropColumn(name: "ProgressUpdateId", table: "SiteInstructions");
            migrationBuilder.DropColumn(name: "RaisedByEmail", table: "SiteInstructions");
            migrationBuilder.DropColumn(name: "ProgressUpdateId", table: "Defects");
            migrationBuilder.DropColumn(name: "RaisedByEmail", table: "Defects");
        }
    }
}
