using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The Architect's Instruction register: the formal instructions that authorise varied work, and
    // the many-to-many between them and the variations they cover (one instruction routinely
    // instructs several; a variation can rest on more than one).
    //
    // Written as guarded raw SQL, matching AddPerformanceIndexes: the migration is then safe to
    // re-run and safe against a database where the tables were created by hand.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260726100000_AddArchitectInstructionsAndRequestAttachments")]
    public partial class AddArchitectInstructionsAndRequestAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ArchitectInstructions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArchitectInstructions] (
        [ArchitectInstructionId] nvarchar(64)   NOT NULL,
        [ProjectId]              nvarchar(64)   NOT NULL,
        [Number]                 int            NOT NULL,
        [Reference]              nvarchar(64)   NOT NULL,
        [InstructionRef]         nvarchar(128)  NOT NULL,
        [Title]                  nvarchar(256)  NOT NULL,
        [Notes]                  nvarchar(2048) NULL,
        [InstructedAt]           datetimeoffset NULL,
        [ReceivedAt]             datetimeoffset NOT NULL,
        [IssuedByEmail]          nvarchar(256)  NOT NULL,
        [FiledByEmail]           nvarchar(256)  NOT NULL,
        [Source]                 int            NOT NULL,
        [FileName]               nvarchar(256)  NULL,
        [ContentType]            nvarchar(128)  NULL,
        [FileSizeBytes]          bigint         NULL,
        [BlobRef]                nvarchar(1024) NULL,
        CONSTRAINT [PK_ArchitectInstructions] PRIMARY KEY ([ArchitectInstructionId])
    );
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ArchitectInstructionVariations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArchitectInstructionVariations] (
        [ArchitectInstructionVariationId] nvarchar(64)   NOT NULL,
        [ArchitectInstructionId]          nvarchar(64)   NOT NULL,
        [VariationOrderId]                nvarchar(64)   NOT NULL,
        [LinkedAt]                        datetimeoffset NOT NULL,
        [LinkedByEmail]                   nvarchar(256)  NOT NULL,
        CONSTRAINT [PK_ArchitectInstructionVariations] PRIMARY KEY ([ArchitectInstructionVariationId])
    );
END
");

            // The register is always read per project, and the links are always read by instruction
            // or by variation — JPMS declares no FK relationships, so EF's index convention never
            // fires and these have to be asked for explicitly.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArchitectInstructions_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[ArchitectInstructions]'))
    CREATE INDEX [IX_ArchitectInstructions_ProjectId] ON [dbo].[ArchitectInstructions] ([ProjectId]);
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArchitectInstructionVariations_ArchitectInstructionId'
               AND object_id = OBJECT_ID(N'[dbo].[ArchitectInstructionVariations]'))
    CREATE INDEX [IX_ArchitectInstructionVariations_ArchitectInstructionId]
        ON [dbo].[ArchitectInstructionVariations] ([ArchitectInstructionId]);
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArchitectInstructionVariations_VariationOrderId'
               AND object_id = OBJECT_ID(N'[dbo].[ArchitectInstructionVariations]'))
    CREATE INDEX [IX_ArchitectInstructionVariations_VariationOrderId]
        ON [dbo].[ArchitectInstructionVariations] ([VariationOrderId]);
");

            // Attachments on a request: linked drawing revisions (no bytes of their own — the
            // register stays the source of truth) and uploaded site photos.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[RequestAttachments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RequestAttachments] (
        [RequestAttachmentId] nvarchar(64)   NOT NULL,
        [RequestId]           nvarchar(64)   NOT NULL,
        [ProjectId]           nvarchar(64)   NOT NULL,
        [Kind]                int            NOT NULL,
        [DrawingId]           nvarchar(64)   NULL,
        [DrawingRevisionId]   nvarchar(64)   NULL,
        [DrawingCode]         nvarchar(64)   NULL,
        [RevisionLabel]       nvarchar(16)   NULL,
        [FileName]            nvarchar(256)  NULL,
        [ContentType]         nvarchar(128)  NULL,
        [FileSizeBytes]       bigint         NULL,
        [BlobRef]             nvarchar(1024) NULL,
        [Caption]             nvarchar(512)  NULL,
        [AddedAt]             datetimeoffset NOT NULL,
        [AddedByEmail]        nvarchar(256)  NOT NULL,
        CONSTRAINT [PK_RequestAttachments] PRIMARY KEY ([RequestAttachmentId])
    );
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RequestAttachments_RequestId'
               AND object_id = OBJECT_ID(N'[dbo].[RequestAttachments]'))
    CREATE INDEX [IX_RequestAttachments_RequestId] ON [dbo].[RequestAttachments] ([RequestId]);
");

            // The audit register is now read per record (a request's own History panel), which the
            // original AddAuditEvents migration only indexed by OccurredAt and ProjectId.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEvents_RecordId'
               AND object_id = OBJECT_ID(N'[dbo].[AuditEvents]'))
    CREATE INDEX [IX_AuditEvents_RecordId] ON [dbo].[AuditEvents] ([RecordId]) INCLUDE ([OccurredAt]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEvents_RecordId'
           AND object_id = OBJECT_ID(N'[dbo].[AuditEvents]'))
    DROP INDEX [IX_AuditEvents_RecordId] ON [dbo].[AuditEvents];
");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[RequestAttachments];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[ArchitectInstructionVariations];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[ArchitectInstructions];");
        }
    }
}
