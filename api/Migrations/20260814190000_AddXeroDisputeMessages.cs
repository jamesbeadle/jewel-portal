using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The dispute discussion thread for Xero cost allocation (2026-08-14): one row per message on
    // a disputed ledger line. The line's Disputed state itself needs no schema — AllocationStatus
    // is already an int and 4 (Disputed) is just a new value — so this table is the whole change.
    // Purely additive: safe to apply before the deploy while the old code is still running.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260814190000_AddXeroDisputeMessages")]
    public partial class AddXeroDisputeMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XeroDisputeMessages",
                columns: table => new
                {
                    XeroDisputeMessageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroDisputeMessages", x => x.XeroDisputeMessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XeroDisputeMessages_XeroLedgerLineId",
                table: "XeroDisputeMessages",
                column: "XeroLedgerLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XeroDisputeMessages");
        }
    }
}
