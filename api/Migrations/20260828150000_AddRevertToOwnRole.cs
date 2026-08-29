using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds DirectoryUsers.RevertToOwnRole (2026-08-28): the per-user opt-in, administered on
    /// Admin → Users, for the "Viewing as" switcher defaulting back to the user's own role
    /// (HomeRoleSelection — their first directory role that isn't Administrator) two hours after a
    /// switch. Built for the Finance Director, whose Administrator view kept sticking across days
    /// because the switcher persisted forever. Not null, default 0 — everyone else keeps today's
    /// sticky behaviour exactly. Additive — apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260828150000_AddRevertToOwnRole")]
    public partial class AddRevertToOwnRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RevertToOwnRole",
                table: "DirectoryUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevertToOwnRole",
                table: "DirectoryUsers");
        }
    }
}
