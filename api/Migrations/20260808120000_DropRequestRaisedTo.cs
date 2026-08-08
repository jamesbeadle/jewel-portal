using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests lose RaisedTo and RaisedToContactId: the "Raised to" field carried free text with
    // no behaviour behind it, so the whole concept — the text column, the structured contact link
    // and every surface that showed them — is removed (decision 2026-08-08).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260808120000_DropRequestRaisedTo")]
    public partial class DropRequestRaisedTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaisedTo",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RaisedToContactId",
                table: "Requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RaisedTo",
                table: "Requests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaisedToContactId",
                table: "Requests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
