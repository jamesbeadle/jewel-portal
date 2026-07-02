using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Structured RFI form + client assignment.
    //
    // 1. RequestItems — the itemised queries of an official RFI sheet (Item / Drawing Ref /
    //    Member-Area / Query / Response), one row per numbered item, ordered by Position.
    // 2. Requests gains the remaining document sections: BasisOfQueries, ResponseActionRequired
    //    and ImpactIfLate. All nullable — existing requests render exactly as before.
    // 3. Projects gains ClientId — a project is assigned to a client account, and requests fall
    //    back to the project's client when they carry no client link of their own.
    // 4. Clients gains RequestEmailPreference — where issued request documents are addressed
    //    (0 = Architect, 1 = PrimaryContact, 2 = Both). Defaults to 0, today's behaviour.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260702150000_AddRfiFormAndClientAssignment")]
    public partial class AddRfiFormAndClientAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestItems",
                columns: table => new
                {
                    RequestItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    DrawingRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    MemberArea = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Query = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Response = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestItems", x => x.RequestItemId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_RequestId",
                table: "RequestItems",
                column: "RequestId");

            migrationBuilder.AddColumn<string>(
                name: "BasisOfQueries",
                table: "Requests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseActionRequired",
                table: "Requests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactIfLate",
                table: "Requests",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Projects",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestEmailPreference",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestItems");

            migrationBuilder.DropColumn(name: "BasisOfQueries", table: "Requests");
            migrationBuilder.DropColumn(name: "ResponseActionRequired", table: "Requests");
            migrationBuilder.DropColumn(name: "ImpactIfLate", table: "Requests");
            migrationBuilder.DropColumn(name: "ClientId", table: "Projects");
            migrationBuilder.DropColumn(name: "RequestEmailPreference", table: "Clients");
        }
    }
}
