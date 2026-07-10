using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Pre-RFI request merging: two General requests can be combined into one before either is
    // promoted. Requests gains MergedIntoRequestId (the survivor's id) and MergedAt — set only on
    // the merged-away record, which is closed at the same time and kept as the audit trail.
    // Nullable; existing requests are untouched.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260710090000_AddRequestMergeLink")]
    public partial class AddRequestMergeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MergedIntoRequestId",
                table: "Requests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MergedAt",
                table: "Requests",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "MergedIntoRequestId",
                table: "Requests");
        }
    }
}
