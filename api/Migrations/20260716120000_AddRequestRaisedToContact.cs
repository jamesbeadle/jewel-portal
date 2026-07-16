using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests gain RaisedToContactId: the ProjectContact (Setup tab contact list) that the
    // "Raised to" field was picked from. RaisedTo keeps the denormalised display string so
    // existing rows, documents and tables render unchanged; the id is the structured link.
    // Nullable — legacy free-text rows and non-dropdown callers (e.g. triage) leave it unset.
    // No FK: contacts are removable and a raised request must keep its audit trail regardless.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260716120000_AddRequestRaisedToContact")]
    public partial class AddRequestRaisedToContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RaisedToContactId",
                table: "Requests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaisedToContactId",
                table: "Requests");
        }
    }
}
