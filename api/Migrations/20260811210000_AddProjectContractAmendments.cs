using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Contract amendments — deeds of variation, side letters, supplemental agreements — each its
    // own row with its own stored document, alongside (not versioning) the executed contract on
    // ProjectContracts. AttachProjectContractDocumentHandler has said since it was written that
    // amendments needing their own history are "a separate record type, not a version chain here";
    // this is that record type. Bytes live in the existing project-contracts blob container under
    // {projectId}/amendments/{amendmentId}/; this table is the register.
    //
    // Written as guarded raw SQL, matching AddWorkOrderAttachments: safe to re-run, and safe
    // against a database where the table was created by hand.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260811210000_AddProjectContractAmendments")]
    public partial class AddProjectContractAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ProjectContractAmendments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectContractAmendments] (
        [ProjectContractAmendmentId] nvarchar(64)   NOT NULL,
        [ProjectId]                  nvarchar(64)   NOT NULL,

        [Title]                      nvarchar(256)  NOT NULL,
        [AmendmentDate]              datetimeoffset NULL,
        [Notes]                      nvarchar(4000) NULL,

        [DocumentBlobRef]            nvarchar(1024) NOT NULL,
        [DocumentFileName]           nvarchar(256)  NOT NULL,
        [DocumentContentType]        nvarchar(128)  NOT NULL,
        [DocumentFileSizeBytes]      bigint         NOT NULL,
        [DocumentUploadedAt]         datetimeoffset NOT NULL,
        [DocumentUploadedByEmail]    nvarchar(256)  NOT NULL,

        [UpdatedByEmail]             nvarchar(256)  NULL,
        [UpdatedAt]                  datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProjectContractAmendments] PRIMARY KEY ([ProjectContractAmendmentId])
    );
END
");

            // Always read per project. NOT unique — amendments accumulate, unlike the one-per-
            // project row in ProjectContracts.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectContractAmendments_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[ProjectContractAmendments]'))
    CREATE INDEX [IX_ProjectContractAmendments_ProjectId] ON [dbo].[ProjectContractAmendments] ([ProjectId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[ProjectContractAmendments];");
        }
    }
}
