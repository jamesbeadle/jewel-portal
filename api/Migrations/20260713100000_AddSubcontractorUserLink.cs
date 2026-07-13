using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Links a directory user (login) to a subcontractor record, so portal endpoints can scope a
    // Role.Subcontractor session to its own company's data. Nullable: internal users stay unlinked.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260713100000_AddSubcontractorUserLink")]
    public partial class AddSubcontractorUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubcontractorId", table: "DirectoryUsers", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SubcontractorId", table: "DirectoryUsers");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
