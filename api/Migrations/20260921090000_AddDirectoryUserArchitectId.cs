using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The architect's identity on a login (2026-09-21): DirectoryUsers.ArchitectId, the architect
    /// twin of ClientId and SubcontractorId. Set by the architect portal invite; read by
    /// ArchitectScope to confine an architect login to the projects that name their practice.
    /// Nullable, additive — apply before or with the deploy. Script: add-directory-user-architect-id.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260921090000_AddDirectoryUserArchitectId")]
    public partial class AddDirectoryUserArchitectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchitectId",
                table: "DirectoryUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ArchitectId", table: "DirectoryUsers");
        }
    }
}
