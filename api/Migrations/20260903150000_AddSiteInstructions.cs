using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds project site instructions (2026-09-03) — a written instruction to site under a short
    /// title, with where it applies: the project's Site Instructions page and the Control
    /// Centre's Internal-pathway "create new → Site instruction". No FKs — the same string-keyed
    /// arrangement as defects and inventory; the handlers own any cascades. SI-#### numbers are
    /// the mailbox tag stems. Id timestamped AFTER every migration already on disk (the
    /// 2026-08-29 inventory lesson).
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260903150000_AddSiteInstructions")]
    public partial class AddSiteInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteInstructions",
                columns: table => new
                {
                    SiteInstructionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Instruction = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SiteInstructions", x => x.SiteInstructionId));

            migrationBuilder.CreateIndex(name: "IX_SiteInstructions_ProjectId", table: "SiteInstructions", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_SiteInstructions_Number", table: "SiteInstructions", column: "Number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SiteInstructions");
        }
    }
}
