using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Valuation claims gain a free-text period name ("June 2026") so a claim can be
    // identified by the month it values rather than only its sequential number. Existing
    // rows default to empty — the UI falls back to "Claim n" until a name is set.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260721090000_AddValuationClaimName")]
    public partial class AddValuationClaimName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ValuationClaims",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ValuationClaims");
        }
    }
}
