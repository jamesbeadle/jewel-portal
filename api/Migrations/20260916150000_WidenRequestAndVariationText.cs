using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// A pasted RFI longer than 2,048 characters failed to save (2026-09-16). The request's
    /// Description, ResponseText and ImpactIfLate were nvarchar(2048) while the RFI form's
    /// sibling sections already allowed 4,000, and the variation raised from an RFI copies the
    /// description across, so its column (VariationOrderQuotes, SubcontractorVariationRequests)
    /// was the same wall one step later. All five go to nvarchar(max), as the site note's
    /// description did on 2026-09-16: the next long paste must not find a new ceiling. Widening
    /// only — no data changes, no index touches these columns. Safe to apply before or with the
    /// deploy. Scoped script: api/Migrations/widen-request-and-variation-text.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260916150000_WidenRequestAndVariationText")]
    public partial class WidenRequestAndVariationText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Requests", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "ResponseText", table: "Requests", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ImpactIfLate", table: "Requests", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "VariationOrderQuotes", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "SubcontractorVariationRequests", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(2048)", oldMaxLength: 2048);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Requests", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "ResponseText", table: "Requests", type: "nvarchar(2048)", maxLength: 2048, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ImpactIfLate", table: "Requests", type: "nvarchar(2048)", maxLength: 2048, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "VariationOrderQuotes", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Description", table: "SubcontractorVariationRequests", type: "nvarchar(2048)", maxLength: 2048, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
        }
    }
}
