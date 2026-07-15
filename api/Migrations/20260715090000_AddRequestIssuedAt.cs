using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests gain an explicit IssuedAt: when the official document (RFI / NOD / EOT) was issued
    // to the correspondent. Distinct from RaisedAt (when the request was raised in the register);
    // set and updated by the user, never stamped automatically. Nullable — existing requests are
    // left without an issue date rather than guessing one from RaisedAt.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260715090000_AddRequestIssuedAt")]
    public partial class AddRequestIssuedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IssuedAt",
                table: "Requests",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "Requests");
        }
    }
}
