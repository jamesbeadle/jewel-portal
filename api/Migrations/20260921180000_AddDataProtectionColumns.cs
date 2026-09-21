using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The data-protection task (2026-09-21): a lead records whether the prospect said we may
    /// keep in touch and when they asked us to stop (Leads.MarketingConsentGivenAt /
    /// MarketingConsentWithdrawnAt); a worker records when they were retired
    /// (Workers.RetiredAt); and the audit trail is indexed by date so the retention sweep can
    /// retire rows by age. All nullable, additive — apply before or with the deploy.
    /// Script: add-data-protection-columns.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260921180000_AddDataProtectionColumns")]
    public partial class AddDataProtectionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MarketingConsentGivenAt", table: "Leads", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MarketingConsentWithdrawnAt", table: "Leads", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetiredAt", table: "Workers", type: "datetimeoffset", nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt", table: "AuditEvents", column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_AuditEvents_OccurredAt", table: "AuditEvents");
            migrationBuilder.DropColumn(name: "RetiredAt", table: "Workers");
            migrationBuilder.DropColumn(name: "MarketingConsentWithdrawnAt", table: "Leads");
            migrationBuilder.DropColumn(name: "MarketingConsentGivenAt", table: "Leads");
        }
    }
}
