using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The pathway split's audit trail (docs/Pathway-Split-Platform-Flow-Plan.md §4): one
    // append-only table of client-facing interactions — triage decisions on client-pathway
    // threads, records created/linked from email, drafted client correspondence, wall
    // refusals, snapshots. Indexed for the register's newest-first read and its most
    // selective filter (project).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260722100000_AddAuditEvents")]
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    AuditEventId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Pathway = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordType = table.Column<int>(type: "int", nullable: true),
                    RecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InternetMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    WebLink = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.AuditEventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ProjectId",
                table: "AuditEvents",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
