using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Claims documents for the Schedule tab.
    //
    // 1. LadClaims — the Liquidated Damages claims the client notifies against Jewel (the
    //    counterpart to Jewel's own NOD/EOT request notices). Sequential Number renders as
    //    "LAD-0001", which doubles as the mailbox tag stem — same mechanism as to-do items.
    // 2. Requests gains RelatedNodRequestId — an EOT's optional provenance back to the Notice
    //    of Delay it arises from (JCT ICD 2024 cl. 2.19 -> 2.20). Nullable; existing requests
    //    are untouched.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260703090000_AddLadClaimsAndRequestNodLink")]
    public partial class AddLadClaimsAndRequestNodLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LadClaims",
                columns: table => new
                {
                    LadClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    PeriodFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DaysClaimed = table.Column<int>(type: "int", nullable: false),
                    RatePerWeek = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LadClaims", x => x.LadClaimId);
                });

            // EOT -> NoD provenance: the Notice of Delay request an EOT arises from. Optional.
            migrationBuilder.AddColumn<string>(
                name: "RelatedNodRequestId",
                table: "Requests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedNodRequestId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "LadClaims");
        }
    }
}
