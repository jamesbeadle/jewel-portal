using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Imagine + proposals (2026-09-06, the third Sales migration of the day): the lead's private
    /// imagine link (Leads.ImagineToken, unique where set), the ImagineRounds and ImagineImages
    /// tables behind the public /imagine/{token} page and its AI renders, and SalesProposals — the
    /// scoping/pricing stage with its acceptance record. Additive only; no FKs, as everywhere
    /// else. Id timestamped after every migration on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260906200000_AddImagine")]
    public partial class AddImagine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ImagineToken", table: "Leads", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ImagineTokenIssuedAt", table: "Leads", type: "datetimeoffset", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_Leads_ImagineToken", table: "Leads", column: "ImagineToken", unique: true, filter: "[ImagineToken] IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "ImagineRounds",
                columns: table => new
                {
                    RoundId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Brief = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BasedOnImageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProspectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProspectEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_ImagineRounds", x => x.RoundId));
            migrationBuilder.CreateIndex(name: "IX_ImagineRounds_LeadId", table: "ImagineRounds", column: "LeadId");
            migrationBuilder.CreateIndex(name: "IX_ImagineRounds_RequestedAt", table: "ImagineRounds", column: "RequestedAt");

            migrationBuilder.CreateTable(
                name: "ImagineImages",
                columns: table => new
                {
                    ImageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RoundId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Liked = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ImagineImages", x => x.ImageId));
            migrationBuilder.CreateIndex(name: "IX_ImagineImages_LeadId", table: "ImagineImages", column: "LeadId");
            migrationBuilder.CreateIndex(name: "IX_ImagineImages_RoundId", table: "ImagineImages", column: "RoundId");

            migrationBuilder.CreateTable(
                name: "SalesProposals",
                columns: table => new
                {
                    ProposalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeroImageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AcceptedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AcceptedOptionIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptedPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AcceptedClientHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeclinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SalesProposals", x => x.ProposalId));
            migrationBuilder.CreateIndex(name: "IX_SalesProposals_LeadId", table: "SalesProposals", column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SalesProposals");
            migrationBuilder.DropTable(name: "ImagineImages");
            migrationBuilder.DropTable(name: "ImagineRounds");
            migrationBuilder.DropIndex(name: "IX_Leads_ImagineToken", table: "Leads");
            migrationBuilder.DropColumn(name: "ImagineToken", table: "Leads");
            migrationBuilder.DropColumn(name: "ImagineTokenIssuedAt", table: "Leads");
        }
    }
}
