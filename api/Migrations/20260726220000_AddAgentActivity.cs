using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The agent activity log: one row per agent run, append-only.
    //
    // Distinct from AuditEvents, which records what PEOPLE did to client correspondence. This
    // answers a different question — when did the machine act, on whose behalf, what did it touch,
    // and what did it cost. It exists before the scheduled agents deliberately: an autonomous agent
    // you cannot see the history of is one you cannot answer for.
    //
    // Guarded raw SQL, matching the house pattern: safe to re-run.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260726220000_AddAgentActivity")]
    public partial class AddAgentActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AgentActivity]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AgentActivity] (
        [ActivityId]       nvarchar(64)   NOT NULL,
        [AgentKey]         nvarchar(64)   NOT NULL,
        [Trigger]          int            NOT NULL,

        [ActorEmail]       nvarchar(256)  NOT NULL,
        [IsAutonomous]     bit            NOT NULL,

        [Action]           nvarchar(128)  NOT NULL,
        [Outcome]          int            NOT NULL,
        [Summary]          nvarchar(1024) NOT NULL,

        [ConversationId]   nvarchar(64)   NULL,
        [ProjectId]        nvarchar(64)   NULL,
        [RecordReference]  nvarchar(64)   NULL,
        [Route]            nvarchar(512)  NULL,

        [ToolsUsed]        nvarchar(512)  NULL,

        [DurationMs]       int            NOT NULL,
        [InputTokens]      int            NOT NULL,
        [OutputTokens]     int            NOT NULL,
        [CostPence]        decimal(18,4)  NOT NULL,

        [OccurredAt]       datetimeoffset NOT NULL,
        CONSTRAINT [PK_AgentActivity] PRIMARY KEY ([ActivityId])
    );
END
");

            // The log is read newest-first, and the one filter that matters most is "show me only
            // what ran with nobody watching".
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentActivity_OccurredAt'
               AND object_id = OBJECT_ID(N'[dbo].[AgentActivity]'))
    CREATE INDEX [IX_AgentActivity_OccurredAt] ON [dbo].[AgentActivity] ([OccurredAt] DESC);
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentActivity_IsAutonomous_OccurredAt'
               AND object_id = OBJECT_ID(N'[dbo].[AgentActivity]'))
    CREATE INDEX [IX_AgentActivity_IsAutonomous_OccurredAt]
        ON [dbo].[AgentActivity] ([IsAutonomous], [OccurredAt] DESC);
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentActivity_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[AgentActivity]'))
    CREATE INDEX [IX_AgentActivity_ProjectId] ON [dbo].[AgentActivity] ([ProjectId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[AgentActivity];");
        }
    }
}
