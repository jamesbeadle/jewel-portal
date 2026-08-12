using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The assistant's skill store (docs/ai/05-agents-and-skills.md §2). Skills are the DOMAIN half
    // of an agent — versioned markdown doctrine edited in the portal by the discipline owner —
    // while the agent scaffolding stays in code (contracts/Ai/AgentCatalogue.cs). Three tables:
    // the skill, its on-demand reference documents, and the append-only revision trail a save
    // writes the outgoing body to, so a doctrine edit is never destructive.
    //
    // Written as guarded raw SQL, matching AddProjectContractsAndAiConversations: safe to re-run,
    // and safe against a database where the tables were created by hand.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260812090000_AddAiSkills")]
    public partial class AddAiSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Skills]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Skills] (
        [SkillKey]        nvarchar(128)  NOT NULL,
        [AgentKey]        nvarchar(64)   NOT NULL,
        [DisplayName]     nvarchar(256)  NOT NULL,
        [Description]     nvarchar(4000) NOT NULL,
        [Body]            nvarchar(max)  NOT NULL,
        [Pinned]          bit            NOT NULL,
        [IsActive]        bit            NOT NULL,
        [Version]         int            NOT NULL,
        [UpdatedByEmail]  nvarchar(256)  NOT NULL,
        [UpdatedAt]       datetimeoffset NOT NULL,
        CONSTRAINT [PK_Skills] PRIMARY KEY ([SkillKey])
    );

    CREATE INDEX [IX_Skills_AgentKey_IsActive]
        ON [dbo].[Skills] ([AgentKey], [IsActive]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[SkillReferences]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SkillReferences] (
        [SkillReferenceId] nvarchar(64)   NOT NULL,
        [SkillKey]         nvarchar(128)  NOT NULL,
        [RefKey]           nvarchar(128)  NOT NULL,
        [DisplayName]      nvarchar(256)  NOT NULL,
        [Description]      nvarchar(2000) NOT NULL,
        [Body]             nvarchar(max)  NOT NULL,
        [UpdatedByEmail]   nvarchar(256)  NOT NULL,
        [UpdatedAt]        datetimeoffset NOT NULL,
        CONSTRAINT [PK_SkillReferences] PRIMARY KEY ([SkillReferenceId])
    );

    CREATE UNIQUE INDEX [IX_SkillReferences_SkillKey_RefKey]
        ON [dbo].[SkillReferences] ([SkillKey], [RefKey]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[SkillRevisions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SkillRevisions] (
        [SkillRevisionId] nvarchar(64)   NOT NULL,
        [SkillKey]        nvarchar(128)  NOT NULL,
        [Version]         int            NOT NULL,
        [Body]            nvarchar(max)  NOT NULL,
        [Description]     nvarchar(4000) NOT NULL,
        [SavedByEmail]    nvarchar(256)  NOT NULL,
        [SavedAt]         datetimeoffset NOT NULL,
        CONSTRAINT [PK_SkillRevisions] PRIMARY KEY ([SkillRevisionId])
    );

    CREATE INDEX [IX_SkillRevisions_SkillKey_Version]
        ON [dbo].[SkillRevisions] ([SkillKey], [Version]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[SkillRevisions]', N'U') IS NOT NULL DROP TABLE [dbo].[SkillRevisions];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[SkillReferences]', N'U') IS NOT NULL DROP TABLE [dbo].[SkillReferences];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[Skills]', N'U') IS NOT NULL DROP TABLE [dbo].[Skills];");
        }
    }
}
