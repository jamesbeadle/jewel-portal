using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // SCHEMA ONLY. Adds the company-directory columns to Subcontractors. The directory DATA (the
    // 246-record master sheet) is NOT seeded here — it is third-party contact data kept out of git
    // and loaded manually via scripts/seed-subcontractors.sql (sqlcmd), like the other seed scripts.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260630130000_AddCompanyDirectory")]
    public partial class AddCompanyDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category", table: "Subcontractors", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MobileNumber", table: "Subcontractors", type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Town", table: "Subcontractors", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "County", table: "Subcontractors", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website", table: "Subcontractors", type: "nvarchar(512)", maxLength: 512, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pli", table: "Subcontractors", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PliExpiry", table: "Subcontractors", type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Category", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "MobileNumber", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "Town", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "County", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "Website", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "Pli", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "PliExpiry", table: "Subcontractors");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
