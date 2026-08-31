using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds AiActionSkills (2026-08-31): skills attached to connector actions, or to whole action
    /// areas, so describe_action inlines the attached doctrine next to the argument schema. The
    /// actions are code (AiActionRegistry); the skills are rows; this table is the edge between
    /// them, curated on the AI Actions admin page. Additive — apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260831090000_AddAiActionSkills")]
    public partial class AddAiActionSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiActionSkills",
                columns: table => new
                {
                    ActionSkillId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TargetKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SkillKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AttachedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AttachedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiActionSkills", x => x.ActionSkillId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiActionSkills_Target_Skill",
                table: "AiActionSkills",
                columns: new[] { "TargetKind", "TargetKey", "SkillKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiActionSkills");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
