using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameChangesToRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChangeRecordId",
                table: "ChangeRecords",
                newName: "RequestId");

            migrationBuilder.RenameTable(
                name: "ChangeRecords",
                newName: "Requests");

            migrationBuilder.AddColumn<string>(
                name: "RaisedTo",
                table: "Requests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrawingRef",
                table: "Requests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResponseDue",
                table: "Requests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedDrawingSpec",
                table: "Requests",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Requests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientNotes",
                table: "Requests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RaisedTo", table: "Requests");
            migrationBuilder.DropColumn(name: "DrawingRef", table: "Requests");
            migrationBuilder.DropColumn(name: "ResponseDue", table: "Requests");
            migrationBuilder.DropColumn(name: "RelatedDrawingSpec", table: "Requests");
            migrationBuilder.DropColumn(name: "InternalNotes", table: "Requests");
            migrationBuilder.DropColumn(name: "ClientNotes", table: "Requests");

            migrationBuilder.RenameTable(
                name: "Requests",
                newName: "ChangeRecords");

            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "ChangeRecords",
                newName: "ChangeRecordId");
        }
    }
}
