using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Tender-only prospects: companies minted so a bid-package tender list could hold them
    // (quick-add or the local web search) are no longer full directory entries. IsProspect
    // marks them; the Directory and its pickers hide prospects until they are promoted — the
    // "Add to directory" act on a submitted tender, or automatically on winning an award —
    // so the directory stays a curated list of companies judged worth working with. Every
    // existing record predates the flag and stays in the directory (defaultValue: false).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260819100000_AddSubcontractorProspectFlag")]
    public partial class AddSubcontractorProspectFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProspect", table: "Subcontractors", type: "bit",
                nullable: false, defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsProspect", table: "Subcontractors");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
