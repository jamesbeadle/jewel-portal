using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests gain an explicit ClosedAt: the date the request was closed, chosen by the user at
    // close time (defaults to today, may be backdated when the closure is only recorded later).
    // Nullable; existing closed requests are left without a close date rather than guessing one.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260710120000_AddRequestClosedAt")]
    public partial class AddRequestClosedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                table: "Requests",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Requests");
        }
    }
}
