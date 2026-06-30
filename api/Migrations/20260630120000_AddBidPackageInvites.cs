using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(JpmsContext))]
    [Migration("20260630120000_AddBidPackageInvites")]
    public partial class AddBidPackageInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BidPackageRecipients",
                columns: table => new
                {
                    RecipientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidPackageRecipients", x => x.RecipientId);
                });

            migrationBuilder.CreateTable(
                name: "BidPackageLineItems",
                columns: table => new
                {
                    LineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidPackageLineItems", x => x.LineItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BidPackageRecipients");
            migrationBuilder.DropTable(name: "BidPackageLineItems");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // The runtime migrator applies Up()/Down() directly; it does not read this model. The
            // design-time model diff for future migrations uses JpmsContextModelSnapshot (updated to
            // include these two tables). Declared explicitly so the migration compiles regardless of
            // whether the base BuildTargetModel is virtual or abstract.
        }
    }
}
