using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds the site address to projects (address line, town, postcode). Town + Postcode drive the
    /// "find local subcontractors" search near the project on bid packages.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260706170000_AddProjectAddress")]
    public partial class AddProjectAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Projects",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Town",
                table: "Projects",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Projects",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AddressLine", table: "Projects");
            migrationBuilder.DropColumn(name: "Town", table: "Projects");
            migrationBuilder.DropColumn(name: "Postcode", table: "Projects");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // The runtime migrator applies Up()/Down() directly; the design-time model diff uses
            // JpmsContextModelSnapshot (updated to include the project address columns).
        }
    }
}
