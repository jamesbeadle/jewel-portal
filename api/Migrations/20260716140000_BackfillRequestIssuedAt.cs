using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests now show a single date — "Date issued" — everywhere; RaisedAt survives only as the
    // internal created-on audit stamp (and default register ordering). Data-only backfill: every
    // existing request without an issue date takes its raised date as the issue date, so no row is
    // left dateless in the register or on the official document. New requests stamp IssuedAt on
    // creation (RaiseRequestHandler / mailbox intake), so the column stays populated from here on.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260716140000_BackfillRequestIssuedAt")]
    public partial class BackfillRequestIssuedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Requests] SET [IssuedAt] = [RaisedAt] WHERE [IssuedAt] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible by design: once backfilled there is no record of which rows had a
            // user-set issue date beforehand, so Down leaves the data as-is.
        }
    }
}
