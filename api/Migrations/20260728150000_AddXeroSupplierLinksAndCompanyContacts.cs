using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Directory ↔ Xero import + consolidation support:
    //  - SubcontractorXeroLinks — one row per directory record ↔ Xero contact link, written by
    //    "Import from Xero". XeroContactId is unique (a Xero supplier imports once); consolidation
    //    re-points links to the master record, so "linked to Xero" survives a merge.
    //  - CompanyContacts — the additional people on a directory record. Consolidation keeps every
    //    merged record's contact details as one of these, and Xero imports add the Xero contact
    //    persons; Purpose ("Accounts", "Projects"…) says what the contact is used for.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260728150000_AddXeroSupplierLinksAndCompanyContacts")]
    public partial class AddXeroSupplierLinksAndCompanyContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubcontractorXeroLinks",
                columns: table => new
                {
                    SubcontractorXeroLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroContactId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorXeroLinks", x => x.SubcontractorXeroLinkId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorXeroLinks_XeroContactId",
                table: "SubcontractorXeroLinks",
                column: "XeroContactId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorXeroLinks_SubcontractorId",
                table: "SubcontractorXeroLinks",
                column: "SubcontractorId");

            migrationBuilder.CreateTable(
                name: "CompanyContacts",
                columns: table => new
                {
                    CompanyContactId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyContacts", x => x.CompanyContactId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContacts_SubcontractorId",
                table: "CompanyContacts",
                column: "SubcontractorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SubcontractorXeroLinks");
            migrationBuilder.DropTable(name: "CompanyContacts");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
