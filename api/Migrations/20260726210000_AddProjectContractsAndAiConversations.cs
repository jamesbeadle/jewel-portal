using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Two unrelated additions that land together because they ship together.
    //
    // ProjectContracts — the contract a project is let under. Before this, "the contract sum" was
    // answerable only from ValuationClaims.ContractSum (frozen per claim, so two claims could
    // disagree) and the LAD rate only from LadClaims.RatePerWeek (same problem); the completion date
    // was not stored anywhere. Those columns stay — they are deliberate snapshots — but this row is
    // the fact they should be taken from. The OH&P and notice-period columns are here rather than in
    // configuration because they are contract terms, argued from the Contract Particulars, and they
    // differ per project.
    //
    // AiConversations / AiConversationMessages — the assistant's transcript. The server rebuilds
    // every turn from these rows and never trusts the client's copy, which is what makes the stored
    // conversation a record of what the model actually saw.
    //
    // Written as guarded raw SQL, matching AddArchitectInstructionsAndRequestAttachments: safe to
    // re-run, and safe against a database where the tables were created by hand.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260726210000_AddProjectContractsAndAiConversations")]
    public partial class AddProjectContractsAndAiConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // decimal(18,4) throughout — JpmsContext.ConfigureConventions pins that globally.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ProjectContracts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectContracts] (
        [ProjectContractId]                nvarchar(64)   NOT NULL,
        [ProjectId]                        nvarchar(64)   NOT NULL,

        [Form]                             int            NOT NULL,
        [FormEdition]                      nvarchar(16)   NULL,
        [BespokeDeviations]                nvarchar(4000) NULL,

        [EmployerName]                     nvarchar(256)  NULL,
        [ContractAdministratorName]        nvarchar(256)  NULL,
        [ContractAdministratorEmail]       nvarchar(256)  NULL,
        [ArchitectName]                    nvarchar(256)  NULL,
        [ArchitectEmail]                   nvarchar(256)  NULL,
        [ContractorName]                   nvarchar(256)  NULL,

        [ContractSum]                      decimal(18,4)  NOT NULL,
        [LiquidatedDamagesPerWeek]         decimal(18,4)  NOT NULL,

        [ContractDate]                     datetimeoffset NULL,
        [PossessionDate]                   datetimeoffset NULL,
        [CompletionDate]                   datetimeoffset NULL,

        [RetentionPercent]                 decimal(18,4)  NOT NULL,
        [RetentionPercentAfterCompletion]  decimal(18,4)  NOT NULL,
        [DefectsLiabilityPeriodMonths]     int            NOT NULL,

        [ApplicationCutOffDayOfMonth]      int            NULL,
        [PaymentNoticeDays]                int            NOT NULL,
        [PayLessNoticeDays]                int            NOT NULL,
        [FinalDateForPaymentDays]          int            NOT NULL,

        [OhpDirectWorksPercent]            decimal(18,4)  NOT NULL,
        [OhpSubcontractorPercent]          decimal(18,4)  NOT NULL,
        [AttendanceOnClientDirectPercent]  decimal(18,4)  NOT NULL,
        [DayworkLabourPercent]             decimal(18,4)  NOT NULL,
        [DayworkMaterialsPercent]          decimal(18,4)  NOT NULL,
        [DayworkPlantPercent]              decimal(18,4)  NOT NULL,

        [DocumentBlobRef]                  nvarchar(1024) NULL,
        [DocumentFileName]                 nvarchar(256)  NULL,
        [DocumentContentType]              nvarchar(128)  NULL,
        [DocumentFileSizeBytes]            bigint         NULL,
        [DocumentUploadedAt]               datetimeoffset NULL,
        [DocumentUploadedByEmail]          nvarchar(256)  NULL,

        [UpdatedByEmail]                   nvarchar(256)  NULL,
        [UpdatedAt]                        datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProjectContracts] PRIMARY KEY ([ProjectContractId])
    );
END
");

            // One contract per project. The handlers treat the row as an upsert, and this is what
            // stops two concurrent first-saves from both inserting.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectContracts_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[ProjectContracts]'))
    CREATE UNIQUE INDEX [IX_ProjectContracts_ProjectId] ON [dbo].[ProjectContracts] ([ProjectId]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AiConversations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AiConversations] (
        [ConversationId]  nvarchar(64)   NOT NULL,
        [ProjectId]       nvarchar(64)   NULL,
        [Route]           nvarchar(512)  NULL,
        [CapabilityKey]   nvarchar(64)   NOT NULL,
        [StartedByEmail]  nvarchar(256)  NOT NULL,
        [Title]           nvarchar(256)  NULL,
        [StartedAt]       datetimeoffset NOT NULL,
        [LastMessageAt]   datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiConversations] PRIMARY KEY ([ConversationId])
    );
END
");

            // Body is nvarchar(max) on purpose: a tool result carrying a variation register would be
            // silently truncated by a length cap, and a silently truncated tool result is a model
            // reasoning from partial data without knowing it.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AiConversationMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AiConversationMessages] (
        [MessageId]       nvarchar(64)   NOT NULL,
        [ConversationId]  nvarchar(64)   NOT NULL,
        [Role]            int            NOT NULL,
        [Body]            nvarchar(max)  NOT NULL,
        [ToolName]        nvarchar(128)  NULL,
        [ToolUseId]       nvarchar(128)  NULL,
        [Sequence]        int            NOT NULL,
        [PostedAt]        datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiConversationMessages] PRIMARY KEY ([MessageId])
    );
END
");

            // Every turn replays the whole conversation in sequence order, so this is the hot path.
            // Ordering by PostedAt would not be safe — several rows are written inside one turn and
            // can share a millisecond.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AiConversationMessages_ConversationId_Sequence'
               AND object_id = OBJECT_ID(N'[dbo].[AiConversationMessages]'))
    CREATE INDEX [IX_AiConversationMessages_ConversationId_Sequence]
        ON [dbo].[AiConversationMessages] ([ConversationId], [Sequence]);
");

            // The panel lists a user's own recent conversations.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AiConversations_StartedByEmail_LastMessageAt'
               AND object_id = OBJECT_ID(N'[dbo].[AiConversations]'))
    CREATE INDEX [IX_AiConversations_StartedByEmail_LastMessageAt]
        ON [dbo].[AiConversations] ([StartedByEmail], [LastMessageAt]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[AiConversationMessages];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[AiConversations];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[ProjectContracts];");
        }
    }
}
