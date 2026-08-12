using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Document Control (DocumentControlItemEntity + PaymentCertificateEntity): the register of
    // email attachments sent over from the Control Centre, and the per-project payment certificate
    // register that filing can create rows in. Both keep their own blob copies (BlobRef) so neither
    // register is orphaned by mailbox or queue housekeeping. House style: loose string ids, no FK
    // constraints. Purely additive — deploy order with the code doesn't matter, but run it first
    // anyway; until it runs, every Document Control page read 500s on the missing tables.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260812180000_AddDocumentControl")]
    public partial class AddDocumentControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentControlItems",
                columns: table => new
                {
                    DocumentControlItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),

                    // The source email: Graph ids while the mailbox still has it, envelope snapshot forever.
                    MessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AttachmentId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),

                    // The file itself, held in the document-control blob store.
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),

                    ProjectIdHint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),

                    // DocumentControlStatus: 0 Pending, 1 Filed, 2 Discarded.
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),

                    // Stamped when the item is filed or discarded.
                    ResolvedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FiledAsKind = table.Column<int>(type: "int", nullable: true),
                    FiledRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FiledLabel = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DocumentControlItems", x => x.DocumentControlItemId));

            // The send handler's duplicate check reads by MessageId; the queue views split on Status.
            migrationBuilder.CreateIndex(
                name: "IX_DocumentControlItems_MessageId",
                table: "DocumentControlItems",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentControlItems_Status",
                table: "DocumentControlItems",
                column: "Status");

            migrationBuilder.CreateTable(
                name: "PaymentCertificates",
                columns: table => new
                {
                    PaymentCertificateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IssuedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),

                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),

                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    // Provenance: the Document Control item this certificate was filed from, when it came that way.
                    SourceDocumentControlItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_PaymentCertificates", x => x.PaymentCertificateId));

            // The register's one read is "every certificate on this project".
            migrationBuilder.CreateIndex(
                name: "IX_PaymentCertificates_ProjectId",
                table: "PaymentCertificates",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DocumentControlItems");
            migrationBuilder.DropTable(name: "PaymentCertificates");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
