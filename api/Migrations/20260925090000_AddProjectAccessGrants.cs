using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The projects an architect login may see, given to the login in Admin → Users (2026-09-25,
    /// Nigel): ProjectAccessGrants, one row per login and project, unique on the pair. Additive
    /// only; no FKs, as everywhere else. Script: add-project-access-grants.sql. Apply before or with
    /// the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260925090000_AddProjectAccessGrants")]
    public partial class AddProjectAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectAccessGrants",
                columns: table => new
                {
                    ProjectAccessGrantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GrantedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ProjectAccessGrants", x => x.ProjectAccessGrantId));
            migrationBuilder.CreateIndex(
                name: "IX_ProjectAccessGrants_Email_ProjectId",
                table: "ProjectAccessGrants",
                columns: new[] { "Email", "ProjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProjectAccessGrants");
        }
    }
}
