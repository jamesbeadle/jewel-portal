using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Client conversations on RFIs and Variation Orders (2026-08-31).
    ///
    /// Three additive pieces:
    /// - RequestMessages.ParentMessageId — threads the request conversation: a reply points at the
    ///   in-app message it answers; null means top-level (all existing rows, and every email leg).
    /// - VariationOrderMessages — a variation order's own in-app conversation. Requests had one
    ///   (RequestMessages); variations only had live tagged email. Same typed-message shape,
    ///   without the mailbox columns.
    /// - DirectoryUsers.ClientId — links a login to a client account, the way SubcontractorId
    ///   links a portal subcontractor. Client portal endpoints scope every read/write to it.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260831160000_AddClientConversations")]
    public partial class AddClientConversations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentMessageId", table: "RequestMessages",
                type: "nvarchar(64)", maxLength: 64, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId", table: "DirectoryUsers",
                type: "nvarchar(64)", maxLength: 64, nullable: true);

            migrationBuilder.CreateTable(
                name: "VariationOrderMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VariationOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AuthorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ParentMessageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_VariationOrderMessages", x => x.MessageId));

            migrationBuilder.CreateIndex(
                name: "IX_VariationOrderMessages_VariationOrderId",
                table: "VariationOrderMessages",
                column: "VariationOrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VariationOrderMessages");
            migrationBuilder.DropColumn(name: "ClientId", table: "DirectoryUsers");
            migrationBuilder.DropColumn(name: "ParentMessageId", table: "RequestMessages");
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
