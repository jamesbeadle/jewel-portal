using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds project inventory items (a product held for the job and where it's kept) — the
    /// project's Inventory tab and the Control Centre's Supplier-pathway "create new → Inventory
    /// item". No FKs — the same string-keyed arrangement as defects; the handlers own any
    /// cascades. INV-#### numbers are the mailbox tag stems.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260828130000_AddInventoryItems")]
    public partial class AddInventoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    InventoryItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProductDetails = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LocationDetails = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_InventoryItems", x => x.InventoryItemId));

            migrationBuilder.CreateIndex(name: "IX_InventoryItems_ProjectId", table: "InventoryItems", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_InventoryItems_Number", table: "InventoryItems", column: "Number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InventoryItems");
        }
    }
}
