using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Revoking a user used to DELETE their DirectoryUsers row and role rows outright, which left
    // no record of who had been revoked and nothing to restore. Revocation is now a soft state on
    // the row itself: RevokedAt (null = active) and RevokedBy (the administrator who did it). The
    // role rows survive a revocation so a restore puts the user back exactly as they were; the
    // hard delete still exists (DeleteDirectoryUser) but only for rows already revoked. Every
    // existing row is active, so both columns backfill to null.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260730100000_AddDirectoryUserRevocation")]
    public partial class AddDirectoryUserRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt", table: "DirectoryUsers", type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedBy", table: "DirectoryUsers", type: "nvarchar(256)",
                maxLength: 256, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RevokedAt", table: "DirectoryUsers");
            migrationBuilder.DropColumn(name: "RevokedBy", table: "DirectoryUsers");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
