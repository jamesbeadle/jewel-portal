using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Useful Information notes (UsefulInformationNoteEntity): titled free-text notes kept against
    // a project for internal use — door codes, key safe locations, site access notes. Internal
    // roles read and edit alike (UsefulInformationRoles); external roles never see them. House
    // style: loose string ids, no FK constraints. Purely additive — deploy order with the code
    // doesn't matter, but run it first anyway.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260811220000_AddUsefulInformationNotes")]
    public partial class AddUsefulInformationNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsefulInformationNotes",
                columns: table => new
                {
                    UsefulInformationNoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_UsefulInformationNotes", x => x.UsefulInformationNoteId));

            // The tab's one read is "every note on this project".
            migrationBuilder.CreateIndex(
                name: "IX_UsefulInformationNotes_ProjectId",
                table: "UsefulInformationNotes",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UsefulInformationNotes");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
