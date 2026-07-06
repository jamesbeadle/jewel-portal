using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Programme dependencies and baselines. ProgrammeTaskLinks carries finish-to-start
    /// dependencies between programme tasks; ProgrammeBaselines/ProgrammeBaselineTasks snapshot the
    /// whole programme at a point in time so movement can be measured (and delay notices evidenced)
    /// against it.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260706150000_AddProgrammeLinksAndBaselines")]
    public partial class AddProgrammeLinksAndBaselines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgrammeTaskLinks",
                columns: table => new
                {
                    ProgrammeTaskLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PredecessorTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SuccessorTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LagDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeTaskLinks", x => x.ProgrammeTaskLinkId);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeBaselines",
                columns: table => new
                {
                    ProgrammeBaselineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TakenByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeBaselines", x => x.ProgrammeBaselineId);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeBaselineTasks",
                columns: table => new
                {
                    ProgrammeBaselineTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgrammeBaselineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgrammeTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PlannedStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PlannedEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeBaselineTasks", x => x.ProgrammeBaselineTaskId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgrammeTaskLinks");

            migrationBuilder.DropTable(
                name: "ProgrammeBaselines");

            migrationBuilder.DropTable(
                name: "ProgrammeBaselineTasks");
        }
    }
}
