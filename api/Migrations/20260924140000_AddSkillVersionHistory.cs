using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Every past version of a stored skill and its reference documents can be audited and restored
    /// (2026-09-24, Nigel). SkillRevisions keeps when a version was written and the name, pin and
    /// active flag it had; SkillReferences gains a version, and SkillReferenceRevisions keeps the
    /// text each reference save replaced. Additive — apply before or with the deploy.
    /// Script: add-skill-version-history.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260924140000_AddSkillVersionHistory")]
    public partial class AddSkillVersionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName", table: "SkillRevisions", type: "nvarchar(256)",
                maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned", table: "SkillRevisions", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "IsActive", table: "SkillRevisions", type: "bit", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WrittenAt", table: "SkillRevisions", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "Version", table: "SkillReferences", type: "int", nullable: false, defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SkillReferenceRevisions",
                columns: table => new
                {
                    SkillReferenceRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RefKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WrittenByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WrittenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReplacedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SkillReferenceRevisions", x => x.SkillReferenceRevisionId));

            migrationBuilder.CreateIndex(
                name: "IX_SkillReferenceRevisions_SkillKey_RefKey_Version",
                table: "SkillReferenceRevisions",
                columns: new[] { "SkillKey", "RefKey", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SkillReferenceRevisions");
            migrationBuilder.DropColumn(name: "Version", table: "SkillReferences");
            migrationBuilder.DropColumn(name: "WrittenAt", table: "SkillRevisions");
            migrationBuilder.DropColumn(name: "IsActive", table: "SkillRevisions");
            migrationBuilder.DropColumn(name: "IsPinned", table: "SkillRevisions");
            migrationBuilder.DropColumn(name: "DisplayName", table: "SkillRevisions");
        }
    }
}
