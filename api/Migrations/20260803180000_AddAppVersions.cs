using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The announced app version: a single row ("current") that Admin → System bumps and the API
    // reports on every response header (VersionStampMiddleware) and from /api/version. The number
    // was originally meant to be stamped into BuildVersion by the deploy workflow, but nothing
    // stamps it any more — every build reports "dev" and the UpdateToast could never fire. Making
    // the version a row hands the administrator the button instead. Seeded at 1 so the header
    // rides on every response from the moment the schema lands: open tabs baseline on it, and the
    // FIRST published bump already prompts them.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260803180000_AddAppVersions")]
    public partial class AddAppVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppVersions",
                columns: table => new
                {
                    AppVersionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AppVersions", x => x.AppVersionId));

            // Wrapped in sp_executesql so the batch still compiles if a later migration ever drops
            // the table — inline raw SQL is what permanently poisoned the full idempotent script
            // (see CLAUDE.md, Database migrations).
            migrationBuilder.Sql(@"EXEC sp_executesql N'
                IF NOT EXISTS (SELECT 1 FROM AppVersions WHERE AppVersionId = N''current'')
                    INSERT INTO AppVersions (AppVersionId, Version, PublishedAt, PublishedBy)
                    VALUES (N''current'', 1, SYSDATETIMEOFFSET(), N'''')'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppVersions");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
