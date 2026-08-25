using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // A variation gains a STAGED agreed build-up: the client-agreed priced lines captured before
    // approval (as JSON) so the approve modal opens pre-seeded and the estimate reads the agreed
    // figure — the assistant's variation_build_up dialog writes here. Consumed by approval.
    // Additive only — safe to apply ahead of the deploy.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825130000_AddVariationOrderDraftLines")]
    public partial class AddVariationOrderDraftLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftLinesJson", table: "VariationOrderQuotes",
                type: "nvarchar(max)", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DraftLinesJson", table: "VariationOrderQuotes");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
