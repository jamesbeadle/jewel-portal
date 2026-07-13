using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Subcontractor CRM Feature 3 (docs/06-backlog/subcontractor-crm-scope.md §6):
    //  - New SubcontractorVariationRequests table: a sub proposes and prices a change against one of
    //    their work orders from the portal; acceptance creates a VOQ that runs the normal pipeline.
    //  - WorkOrders gains VariationOrderId (nullable): set on the NEW work order issued to instruct
    //    an approved variation order (existing orders are never uplifted).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260713120000_AddSubcontractorVariationRequests")]
    public partial class AddSubcontractorVariationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubcontractorVariationRequests",
                columns: table => new
                {
                    VariationRequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ProposedValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    VariationOrderQuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorVariationRequests", x => x.VariationRequestId);
                });

            migrationBuilder.AddColumn<string>(
                name: "VariationOrderId", table: "WorkOrders", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SubcontractorVariationRequests");
            migrationBuilder.DropColumn(name: "VariationOrderId", table: "WorkOrders");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
