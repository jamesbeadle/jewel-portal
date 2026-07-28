using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Postal address on the company directory record: AddressLine (street line(s)) and Postcode
    // join the existing Town/County so a purchase order can print the supplier's full address
    // letter-style in its Sub/Vendor block. Both default to "" — the directory's Edit details
    // dialog and the add-company form are where they get filled in.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260728120000_AddSubcontractorAddress")]
    public partial class AddSubcontractorAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine", table: "Subcontractors", type: "nvarchar(256)",
                maxLength: 256, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Postcode", table: "Subcontractors", type: "nvarchar(32)",
                maxLength: 32, nullable: false, defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AddressLine", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "Postcode", table: "Subcontractors");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
