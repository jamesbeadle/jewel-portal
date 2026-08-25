using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds inbound tender enquiries: the enquiry itself (an architect's invitation to tender, on
    /// a Lead-stage project), its PQQ answers, and the files kept on it. No FKs — the same
    /// string-keyed arrangement as bid packages; the handlers own the cascades.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825120000_AddTenderEnquiries")]
    public partial class AddTenderEnquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderEnquiries",
                columns: table => new
                {
                    TenderEnquiryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ArchitectPracticeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ArchitectContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ArchitectContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ScopeSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContractForm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PqqDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenderDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PqqSubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenderSubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_TenderEnquiries", x => x.TenderEnquiryId));

            migrationBuilder.CreateIndex(name: "IX_TenderEnquiries_ProjectId", table: "TenderEnquiries", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_TenderEnquiries_Number", table: "TenderEnquiries", column: "Number");

            migrationBuilder.CreateTable(
                name: "TenderEnquiryAnswers",
                columns: table => new
                {
                    TenderEnquiryAnswerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenderEnquiryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_TenderEnquiryAnswers", x => x.TenderEnquiryAnswerId));

            migrationBuilder.CreateIndex(name: "IX_TenderEnquiryAnswers_TenderEnquiryId", table: "TenderEnquiryAnswers", column: "TenderEnquiryId");

            migrationBuilder.CreateTable(
                name: "TenderEnquiryAttachments",
                columns: table => new
                {
                    TenderEnquiryAttachmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenderEnquiryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AddedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_TenderEnquiryAttachments", x => x.TenderEnquiryAttachmentId));

            migrationBuilder.CreateIndex(name: "IX_TenderEnquiryAttachments_TenderEnquiryId", table: "TenderEnquiryAttachments", column: "TenderEnquiryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TenderEnquiryAttachments");
            migrationBuilder.DropTable(name: "TenderEnquiryAnswers");
            migrationBuilder.DropTable(name: "TenderEnquiries");
        }
    }
}
