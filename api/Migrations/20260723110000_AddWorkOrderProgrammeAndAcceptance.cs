using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Purchase-order print + portal acceptance:
    //  - WorkOrders gains programme fields printed in the PO's Programme section: ProgrammeStart
    //    (ScheduledCompletion remains the target completion date) and free-text ProgrammeNotes.
    //  - WorkOrders gains electronic-acceptance fields stamped when the supplier's signed-in
    //    portal contact accepts the issued order: AcceptedAt / AcceptedByEmail / AcceptedByName.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260723110000_AddWorkOrderProgrammeAndAcceptance")]
    public partial class AddWorkOrderProgrammeAndAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProgrammeStart", table: "WorkOrders", type: "datetimeoffset", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammeNotes", table: "WorkOrders", type: "nvarchar(2000)", maxLength: 2000,
                nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedAt", table: "WorkOrders", type: "datetimeoffset", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByEmail", table: "WorkOrders", type: "nvarchar(256)", maxLength: 256,
                nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByName", table: "WorkOrders", type: "nvarchar(256)", maxLength: 256,
                nullable: false, defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProgrammeStart", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "ProgrammeNotes", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "AcceptedAt", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "AcceptedByEmail", table: "WorkOrders");
            migrationBuilder.DropColumn(name: "AcceptedByName", table: "WorkOrders");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
