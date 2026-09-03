using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds the KPI register (2026-09-03): KpiPeople — the people KPIs are filed under (a portal
    /// user by directory email, or someone added by name alone) — and KpiEmails, an email marked
    /// as a KPI against one of them, readable by administrators only. No FKs (a revoked user's
    /// KPIs survive; the handlers own the person link). Nothing is tagged in the mailbox — the
    /// row is the mark, with the email's envelope snapshotted and its ids kept for opening it
    /// live. Timestamped AFTER every migration already on disk (20260902120000) so the scoped
    /// "script from the last applied id" flow cannot skip it.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260903120000_AddKpiEmails")]
    public partial class AddKpiEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KpiPeople",
                columns: table => new
                {
                    KpiPersonId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_KpiPeople", x => x.KpiPersonId));

            migrationBuilder.CreateIndex(name: "IX_KpiPeople_Email", table: "KpiPeople", column: "Email");

            migrationBuilder.CreateTable(
                name: "KpiEmails",
                columns: table => new
                {
                    KpiEmailId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PersonId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ConversationId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    MarkedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MarkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_KpiEmails", x => x.KpiEmailId));

            migrationBuilder.CreateIndex(name: "IX_KpiEmails_PersonId", table: "KpiEmails", column: "PersonId");
            migrationBuilder.CreateIndex(name: "IX_KpiEmails_Number", table: "KpiEmails", column: "Number");
            migrationBuilder.CreateIndex(name: "IX_KpiEmails_InternetMessageId", table: "KpiEmails", column: "InternetMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "KpiEmails");
            migrationBuilder.DropTable(name: "KpiPeople");
        }
    }
}
