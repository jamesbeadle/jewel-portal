using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddValuationReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValuationLineItems",
                columns: table => new
                {
                    ValuationLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ElementType = table.Column<int>(type: "int", nullable: false),
                    SectionCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VariationRef = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    VariationTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuationLineItems", x => x.ValuationLineItemId);
                });

            migrationBuilder.CreateTable(
                name: "ValuationClaims",
                columns: table => new
                {
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClaimNumber = table.Column<int>(type: "int", nullable: false),
                    ClaimDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionReleasePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PreapprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ContractSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetVariations = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RevisedContractSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalWorksComplete = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionHeld = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionReleased = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CertifiedToDate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentDueExVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuationClaims", x => x.ValuationClaimId);
                });

            migrationBuilder.CreateTable(
                name: "ClaimLines",
                columns: table => new
                {
                    ClaimLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PercentComplete = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CumulativeClaimed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PeriodIncrement = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimLines", x => x.ClaimLineId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValuationLineItems_ProjectId",
                table: "ValuationLineItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationClaims_ProjectId",
                table: "ValuationClaims",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimLines_ValuationClaimId",
                table: "ClaimLines",
                column: "ValuationClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClaimLines");
            migrationBuilder.DropTable(name: "ValuationClaims");
            migrationBuilder.DropTable(name: "ValuationLineItems");
        }
    }
}
