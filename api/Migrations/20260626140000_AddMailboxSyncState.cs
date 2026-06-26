using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMailboxSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailboxSyncStates",
                columns: table => new
                {
                    Mailbox = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BacklogImported = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxSyncStates", x => x.Mailbox);
                });

            // Database-level backstop against duplicate intake (belt-and-braces with the
            // app-level idempotency check in the ingestion layer). Deferred from the first
            // intake migration; added now that ingestion writes to this table.
            migrationBuilder.CreateIndex(
                name: "IX_IntakeEmails_InternetMessageId",
                table: "IntakeEmails",
                column: "InternetMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IntakeEmails_InternetMessageId",
                table: "IntakeEmails");

            migrationBuilder.DropTable(
                name: "MailboxSyncStates");
        }
    }
}
