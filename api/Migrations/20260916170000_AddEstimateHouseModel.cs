using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The estimate's 3D model (2026-09-16): three additive columns on LeadEstimates —
    /// HouseModelJson (the definition, nvarchar(max), NULL until drafted), HouseModelSource and
    /// HouseModelSetAt. Apply before or with the deploy. Script: add-estimate-house-model.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260916170000_AddEstimateHouseModel")]
    public partial class AddEstimateHouseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HouseModelJson",
                table: "LeadEstimates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HouseModelSource",
                table: "LeadEstimates",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HouseModelSetAt",
                table: "LeadEstimates",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HouseModelJson", table: "LeadEstimates");
            migrationBuilder.DropColumn(name: "HouseModelSource", table: "LeadEstimates");
            migrationBuilder.DropColumn(name: "HouseModelSetAt", table: "LeadEstimates");
        }
    }
}
