using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests now carry four statuses answering "whose court is the ball in?":
    //   Needs action (0, the value legacy Open rows already hold), Open (1, the value legacy
    //   Awaiting-response rows already hold), Closed (4) and Needs variation (6, new).
    // The 0/1 rows keep their stored values and simply take the new meaning — a request that was
    // "Awaiting response" is now Open (with the architect), and one that was "Open" now reads
    // Needs action (the ball is with us). Data-only cleanup here: the retired Approved(2),
    // Rejected(3) and Responded(5) statuses all represented finished conversations, so their rows
    // become Closed, taking the most truthful close date already on the row (an existing close
    // date, else the response date, else the issue date, else the raised stamp).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260723090000_ConsolidateRequestStatuses")]
    public partial class ConsolidateRequestStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Requests] SET [ClosedAt] = COALESCE([ClosedAt], [RespondedAt], [IssuedAt], [RaisedAt]), [Status] = 4 WHERE [Status] IN (2, 3, 5);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible by design: once consolidated there is no record of which Closed rows
            // were previously Approved, Rejected or Responded, so Down leaves the data as-is.
        }
    }
}
