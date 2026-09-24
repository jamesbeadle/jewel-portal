using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The Contractor's Report as issued Report 30 reads (2026-09-24, from By France Report 31): an
    /// entered Building Control contact line for a project with no Building Control case, carried
    /// week to week, and the photographs Section 9 leaves out of a selected update. Additive only.
    /// Script: add-contractors-report-contact-and-photo-choice.sql. Apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260924180000_AddContractorsReportContactAndPhotoChoice")]
    public partial class AddContractorsReportContactAndPhotoChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "BuildingControlContact", table: "ContractorsReports", type: "nvarchar(512)", maxLength: 512, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ExcludedPhotoIdsJson", table: "ContractorsReports", type: "nvarchar(max)", nullable: false, defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BuildingControlContact", table: "ContractorsReports");
            migrationBuilder.DropColumn(name: "ExcludedPhotoIdsJson", table: "ContractorsReports");
        }
    }
}
