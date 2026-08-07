using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Attachments kept on a work order for record keeping — the quote the order was raised
    // against, a signed copy, a photo of the scope. Bytes live in the work-order-attachments
    // blob container; this table is the register the Work Orders views read. Attachments never
    // reach the supplier: the purchase-order email and printed PO ignore them entirely.
    //
    // Written as guarded raw SQL, matching AddArchitectInstructionsAndRequestAttachments: the
    // migration is then safe to re-run and safe against a database where the table was created
    // by hand.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260807190000_AddWorkOrderAttachments")]
    public partial class AddWorkOrderAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[WorkOrderAttachments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkOrderAttachments] (
        [WorkOrderAttachmentId] nvarchar(64)   NOT NULL,
        [WorkOrderId]           nvarchar(64)   NOT NULL,
        [ProjectId]             nvarchar(64)   NOT NULL,
        [FileName]              nvarchar(256)  NOT NULL,
        [ContentType]           nvarchar(128)  NOT NULL,
        [FileSizeBytes]         bigint         NOT NULL,
        [BlobRef]               nvarchar(1024) NOT NULL,
        [Source]                int            NOT NULL,
        [AddedAt]               datetimeoffset NOT NULL,
        [AddedByEmail]          nvarchar(256)  NOT NULL,
        CONSTRAINT [PK_WorkOrderAttachments] PRIMARY KEY ([WorkOrderAttachmentId])
    );
END
");
            // Always read per order; JPMS declares no FK relationships, so EF's index convention
            // never fires and the index has to be asked for explicitly.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkOrderAttachments_WorkOrderId'
               AND object_id = OBJECT_ID(N'[dbo].[WorkOrderAttachments]'))
    CREATE INDEX [IX_WorkOrderAttachments_WorkOrderId] ON [dbo].[WorkOrderAttachments] ([WorkOrderId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[WorkOrderAttachments];");
        }
    }
}
