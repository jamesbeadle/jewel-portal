using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The secret behind a work order's acceptance link (2026-09-23, Nigel's ask: a supplier with
    /// no portal login accepts the order from the purchase-order email): WorkOrders.AcceptanceToken,
    /// unique where set, and when it was minted. Nullable, additive — apply before or with the
    /// deploy. Script: add-work-order-acceptance-token.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260923120000_AddWorkOrderAcceptanceToken")]
    public partial class AddWorkOrderAcceptanceToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptanceToken",
                table: "WorkOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptanceTokenIssuedAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AcceptanceToken",
                table: "WorkOrders",
                column: "AcceptanceToken",
                unique: true,
                filter: "[AcceptanceToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_WorkOrders_AcceptanceToken", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "AcceptanceTokenIssuedAt", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "AcceptanceToken", table: "WorkOrders");
        }
    }
}
