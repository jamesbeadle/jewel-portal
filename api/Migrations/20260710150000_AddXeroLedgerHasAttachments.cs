using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Flags ledger lines whose invoice has attachments in Xero (the supplier's
    // document, published by Dext), so the allocation page can offer an in-place
    // invoice viewer. Refreshed on every sync; the documents themselves are
    // fetched from Xero on demand — nothing is stored in JPMS.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260710150000_AddXeroLedgerHasAttachments")]
    public partial class AddXeroLedgerHasAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAttachments",
                table: "XeroLedgerLines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HasAttachments", table: "XeroLedgerLines");
        }
    }
}
