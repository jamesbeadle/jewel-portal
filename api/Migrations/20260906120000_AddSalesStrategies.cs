using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The Sales section (2026-09-06): sales strategies — methodologies for finding leads, with
    /// their justification and a Claude-drafted approach plan — and the rebuilt lead register.
    /// Adds the SalesStrategies and LeadActivities tables; extends Leads with the rebuild's
    /// columns (LD-#### number, prospect kind, postcode, summary, notes, strategy, stage date,
    /// the client/project Won creates, the Lost reason); and remaps the May 2026 prototype's
    /// Stage and Source ints onto the new, shorter enums. Additive only — the prototype's six
    /// satellite CRM tables are left in place, unread. No FKs, as everywhere else in the schema.
    /// The data-moving SQL is wrapped in sp_executesql so it compiles only when it runs (the
    /// standing rule after SeparateArchitectsFromClients poisoned the full script). Id timestamped
    /// AFTER every migration already on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260906120000_AddSalesStrategies")]
    public partial class AddSalesStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesStrategies",
                columns: table => new
                {
                    StrategyId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    TargetArea = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Hypothesis = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Proposition = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ApproachPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanGeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SalesStrategies", x => x.StrategyId));

            migrationBuilder.CreateIndex(name: "IX_SalesStrategies_Status", table: "SalesStrategies", column: "Status");

            migrationBuilder.CreateTable(
                name: "LeadActivities",
                columns: table => new
                {
                    LeadActivityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LeadActivities", x => x.LeadActivityId));

            migrationBuilder.CreateIndex(name: "IX_LeadActivities_LeadId", table: "LeadActivities", column: "LeadId");

            // ---- Leads: the rebuild's columns ----
            migrationBuilder.AddColumn<int>(name: "Number", table: "Leads", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "ProspectKind", table: "Leads", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "Postcode", table: "Leads", type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Summary", table: "Leads", type: "nvarchar(512)", maxLength: 512, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Notes", table: "Leads", type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "StrategyId", table: "Leads", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "StageChangedAt", table: "Leads", type: "datetimeoffset", nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
            migrationBuilder.AddColumn<string>(name: "ClientId", table: "Leads", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ProjectId", table: "Leads", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "LostReason", table: "Leads", type: "nvarchar(1024)", maxLength: 1024, nullable: true);

            migrationBuilder.CreateIndex(name: "IX_Leads_StrategyId", table: "Leads", column: "StrategyId");
            migrationBuilder.CreateIndex(name: "IX_Leads_Number", table: "Leads", column: "Number");

            // ---- Carry the prototype's rows across (there may be none — the prototype had no UI):
            // stage ints onto the 8-stage ladder, source ints onto the new source list, a number in
            // capture order, the stage date from capture, and the project/reason from LeadOutcomes.
            // Old Stage: 0 NewLead, 1 Qualified, 2 SurveyBooked, 3 SurveyComplete, 4 AwaitingInformation,
            //   5 DrawingsReceived, 6 FeasibilityReview, 7 Tendering, 8 ProposalIssued, 9 Negotiation,
            //   10 Won, 11 Lost, 12 Nurture.
            // New Stage: 0 New, 1 Contacted, 2 Engaged, 3 SiteVisit, 4 Proposal, 5 Won, 6 Lost, 7 Nurture.
            // Old Source: 0 Website, 1 Instagram, 2 LinkedIn, 3 Referral, 4 Architect, 5 RepeatClient, 6 Manual.
            // New Source: 0 Strategy, 1 Inbound, 2 Referral, 3 Architect, 4 RepeatClient, 5 Manual.
            migrationBuilder.Sql(@"
EXEC sp_executesql N'
UPDATE [Leads] SET [Stage] = CASE [Stage]
    WHEN 0 THEN 0
    WHEN 1 THEN 2
    WHEN 2 THEN 3
    WHEN 3 THEN 3
    WHEN 4 THEN 2
    WHEN 5 THEN 2
    WHEN 6 THEN 2
    WHEN 7 THEN 2
    WHEN 8 THEN 4
    WHEN 9 THEN 4
    WHEN 10 THEN 5
    WHEN 11 THEN 6
    WHEN 12 THEN 7
    ELSE 0 END
WHERE [Number] = 0;

UPDATE [Leads] SET [Source] = CASE [Source]
    WHEN 0 THEN 1
    WHEN 1 THEN 1
    WHEN 2 THEN 1
    WHEN 3 THEN 2
    WHEN 4 THEN 3
    WHEN 5 THEN 4
    WHEN 6 THEN 5
    ELSE 5 END
WHERE [Number] = 0;

UPDATE [Leads] SET [StageChangedAt] = [CapturedAt] WHERE [Number] = 0;

UPDATE L SET L.[ProjectId] = O.[CreatedProjectId]
FROM [Leads] L INNER JOIN [LeadOutcomes] O ON O.[LeadId] = L.[LeadId]
WHERE L.[Number] = 0 AND O.[IsWon] = 1 AND O.[CreatedProjectId] IS NOT NULL;

UPDATE L SET L.[LostReason] = O.[Reason]
FROM [Leads] L INNER JOIN [LeadOutcomes] O ON O.[LeadId] = L.[LeadId]
WHERE L.[Number] = 0 AND O.[IsWon] = 0;

WITH Numbered AS (
    SELECT [LeadId], ROW_NUMBER() OVER (ORDER BY [CapturedAt], [LeadId]) AS N
    FROM [Leads] WHERE [Number] = 0)
UPDATE L SET L.[Number] = Numbered.N
FROM [Leads] L INNER JOIN Numbered ON Numbered.[LeadId] = L.[LeadId];
';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LeadActivities");
            migrationBuilder.DropTable(name: "SalesStrategies");
            migrationBuilder.DropIndex(name: "IX_Leads_StrategyId", table: "Leads");
            migrationBuilder.DropIndex(name: "IX_Leads_Number", table: "Leads");
            migrationBuilder.DropColumn(name: "Number", table: "Leads");
            migrationBuilder.DropColumn(name: "ProspectKind", table: "Leads");
            migrationBuilder.DropColumn(name: "Postcode", table: "Leads");
            migrationBuilder.DropColumn(name: "Summary", table: "Leads");
            migrationBuilder.DropColumn(name: "Notes", table: "Leads");
            migrationBuilder.DropColumn(name: "StrategyId", table: "Leads");
            migrationBuilder.DropColumn(name: "StageChangedAt", table: "Leads");
            migrationBuilder.DropColumn(name: "ClientId", table: "Leads");
            migrationBuilder.DropColumn(name: "ProjectId", table: "Leads");
            migrationBuilder.DropColumn(name: "LostReason", table: "Leads");
        }
    }
}
