using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests gain CriticalPathNudgeDismissed: the RFI detail page nudges "RFI 2 weeks old —
    // tag as critical path?" on open, untagged RFIs; clicking "No" records the decision on the
    // request so the banner never re-asks anyone about that RFI. Defaults to off for every
    // existing row (the nudge shows until answered). Purely additive — expand-first safe.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260807090000_AddRequestCriticalPathNudgeDismissed")]
    public partial class AddRequestCriticalPathNudgeDismissed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CriticalPathNudgeDismissed",
                table: "Requests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalPathNudgeDismissed",
                table: "Requests");
        }
    }
}
