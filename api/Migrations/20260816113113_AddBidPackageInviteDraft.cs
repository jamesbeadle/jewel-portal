using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBidPackageInviteDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntakeEmails");

            migrationBuilder.DropTable(
                name: "MailboxSyncStates");

            migrationBuilder.DropIndex(
                name: "IX_XeroLedgerLines_AllocationStatus",
                table: "XeroLedgerLines");

            migrationBuilder.DropIndex(
                name: "IX_XeroLedgerLines_ProjectId_CostCenterCode",
                table: "XeroLedgerLines");

            migrationBuilder.DropIndex(
                name: "IX_XeroLedgerLines_XeroInvoiceId",
                table: "XeroLedgerLines");

            migrationBuilder.DropIndex(
                name: "IX_XeroCostSplits_ProjectId_CostCenterCode",
                table: "XeroCostSplits");

            migrationBuilder.DropIndex(
                name: "IX_XeroCostSplits_XeroLedgerLineId",
                table: "XeroCostSplits");

            migrationBuilder.DropIndex(
                name: "IX_ValuationReportSnapshots_ProjectId",
                table: "ValuationReportSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_ValuationReportSnapshots_ValuationInvoiceId",
                table: "ValuationReportSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_ValuationReportSnapshotLines_ValuationReportSnapshotId",
                table: "ValuationReportSnapshotLines");

            migrationBuilder.DropIndex(
                name: "IX_ValuationLineItems_ProjectId",
                table: "ValuationLineItems");

            migrationBuilder.DropIndex(
                name: "IX_ValuationInvoiceEvents_ValuationInvoiceId",
                table: "ValuationInvoiceEvents");

            migrationBuilder.DropIndex(
                name: "IX_ValuationClaims_ProjectId",
                table: "ValuationClaims");

            migrationBuilder.DropIndex(
                name: "IX_Trades_Name",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_SubcontractorTrades_SubcontractorId_TradeId",
                table: "SubcontractorTrades");

            migrationBuilder.DropIndex(
                name: "IX_RequestItems_RequestId",
                table: "RequestItems");

            migrationBuilder.DropIndex(
                name: "IX_RequestAgents_RequestId",
                table: "RequestAgents");

            migrationBuilder.DropIndex(
                name: "IX_ProjectContacts_ProjectId",
                table: "ProjectContacts");

            migrationBuilder.DropIndex(
                name: "IX_ClaimLines_ValuationClaimId",
                table: "ClaimLines");

            migrationBuilder.DropIndex(
                name: "IX_AgentProposals_RequestId",
                table: "AgentProposals");

            migrationBuilder.DropIndex(
                name: "IX_AgentChatMessages_RequestId_AgentKey",
                table: "AgentChatMessages");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "Quotes",
                newName: "ReceivedAt");

            migrationBuilder.RenameColumn(
                name: "RaisedAt",
                table: "InfoChaseItems",
                newName: "RequestedAt");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "DrawingRevisions",
                newName: "ReceivedAt");

            migrationBuilder.RenameColumn(
                name: "RaisedAt",
                table: "AccessRequests",
                newName: "RequestedAt");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountDue",
                table: "XeroLedgerLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoiceTotal",
                table: "XeroLedgerLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByEmail",
                table: "WorkOrders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByName",
                table: "WorkOrders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent",
                table: "WorkOrders",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DepositRequired",
                table: "WorkOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammeNotes",
                table: "WorkOrders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProgrammeStart",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialBasis",
                table: "VariationOrderQuotes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Exclusions",
                table: "VariationOrderQuotes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammeImpact",
                table: "VariationOrderQuotes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent",
                table: "ValuationReportSnapshots",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleased",
                table: "ValuationReportSnapshots",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "ValuationReportSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositCredited",
                table: "ValuationInvoices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent",
                table: "ValuationClaims",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleased",
                table: "ValuationClaims",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositReleasedOpening",
                table: "ValuationClaims",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Subcontractors",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                table: "Subcontractors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Subcontractors",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "QuoteLineItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "QuoteLineItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "QuoteLineItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedMonthlyValuation",
                table: "Projects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartyContactId",
                table: "ProjectContacts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Routing",
                table: "ProjectContacts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InviteDraftBcc",
                table: "BidPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteDraftBody",
                table: "BidPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteDraftCc",
                table: "BidPackages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InviteDraftSavedAt",
                table: "BidPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteDraftSubject",
                table: "BidPackages",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteDraftTo",
                table: "BidPackages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MaterialsApplicable",
                table: "BidPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "BidPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "BidPackageLineItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "BoqLineItemId",
                table: "BidPackageLineItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCode",
                table: "BidPackageLineItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Coverage",
                table: "BidPackageLineItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VariationOrderQuoteId",
                table: "BidPackageLineItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentActivity",
                columns: table => new
                {
                    ActivityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsAutonomous = table.Column<bool>(type: "bit", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ToolsUsed = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    CostPence = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentActivity", x => x.ActivityId);
                });

            migrationBuilder.CreateTable(
                name: "AiConversationMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ToolUseId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ToolCallsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "AiConversations",
                columns: table => new
                {
                    ConversationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CapabilityKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopeRecordType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ScopeRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversations", x => x.ConversationId);
                });

            migrationBuilder.CreateTable(
                name: "ArchitectInstructions",
                columns: table => new
                {
                    ArchitectInstructionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InstructionRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    InstructedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IssuedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FiledByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchitectInstructions", x => x.ArchitectInstructionId);
                });

            migrationBuilder.CreateTable(
                name: "ArchitectInstructionVariations",
                columns: table => new
                {
                    ArchitectInstructionVariationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ArchitectInstructionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VariationOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LinkedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchitectInstructionVariations", x => x.ArchitectInstructionVariationId);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    AuditEventId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Pathway = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordType = table.Column<int>(type: "int", nullable: true),
                    RecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InternetMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    WebLink = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.AuditEventId);
                });

            migrationBuilder.CreateTable(
                name: "CompanyContacts",
                columns: table => new
                {
                    CompanyContactId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyContacts", x => x.CompanyContactId);
                });

            migrationBuilder.CreateTable(
                name: "CostCentreCostProgress",
                columns: table => new
                {
                    CostCentreCostProgressId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CostCompletionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsFinalised = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentreCostProgress", x => x.CostCentreCostProgressId);
                });

            migrationBuilder.CreateTable(
                name: "CostCentreGroupMembers",
                columns: table => new
                {
                    CostCentreGroupMemberId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCentreGroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentreGroupMembers", x => x.CostCentreGroupMemberId);
                });

            migrationBuilder.CreateTable(
                name: "CostCentreGroups",
                columns: table => new
                {
                    CostCentreGroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentreGroups", x => x.CostCentreGroupId);
                });

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                columns: table => new
                {
                    PartyContactId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PartyKind = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DefaultRouting = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.PartyContactId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                columns: table => new
                {
                    ProgressPhotoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UploadedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressPhotos", x => x.ProgressPhotoId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressReports",
                columns: table => new
                {
                    ProgressReportId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    WorkCompleted = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    UpcomingWorks = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressReports", x => x.ProgressReportId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressReportSelections",
                columns: table => new
                {
                    ProgressReportSelectionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgressReportId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressReportSelections", x => x.ProgressReportSelectionId);
                });

            migrationBuilder.CreateTable(
                name: "ProgressUpdates",
                columns: table => new
                {
                    ProgressUpdateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    WorkDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WeatherSummary = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WeatherObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WeatherTempHighC = table.Column<int>(type: "int", nullable: true),
                    WeatherTempLowC = table.Column<int>(type: "int", nullable: true),
                    WeatherWindMph = table.Column<int>(type: "int", nullable: true),
                    WeatherHumidityPercent = table.Column<int>(type: "int", nullable: true),
                    WeatherPrecipInches = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressUpdates", x => x.ProgressUpdateId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectContracts",
                columns: table => new
                {
                    ProjectContractId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Form = table.Column<int>(type: "int", nullable: false),
                    FormEdition = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    BespokeDeviations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EmployerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContractAdministratorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContractAdministratorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ArchitectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ArchitectEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContractorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContractSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LiquidatedDamagesPerWeek = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ContractDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PossessionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RetentionPercentAfterCompletion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefectsLiabilityPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    ApplicationCutOffDayOfMonth = table.Column<int>(type: "int", nullable: true),
                    PaymentNoticeDays = table.Column<int>(type: "int", nullable: false),
                    PayLessNoticeDays = table.Column<int>(type: "int", nullable: false),
                    FinalDateForPaymentDays = table.Column<int>(type: "int", nullable: false),
                    OhpDirectWorksPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OhpSubcontractorPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AttendanceOnClientDirectPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DayworkLabourPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DayworkMaterialsPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DayworkPlantPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DocumentBlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DocumentFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DocumentContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DocumentFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    DocumentUploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DocumentUploadedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectContracts", x => x.ProjectContractId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRetentions",
                columns: table => new
                {
                    ProjectRetentionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CompletionReleasePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DepositPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DepositReleasedOpening = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefectsPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    PracticalCompletionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionReleaseConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionReleaseAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FinalReleaseConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalReleaseAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRetentions", x => x.ProjectRetentionId);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationPackageCostLines",
                columns: table => new
                {
                    ReconciliationPackageCostLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackageCostLines", x => x.ReconciliationPackageCostLineId);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationPackageOrders",
                columns: table => new
                {
                    ReconciliationPackageOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackageOrders", x => x.ReconciliationPackageOrderId);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationPackages",
                columns: table => new
                {
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedSalesValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LockedClaimedToDate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LockedTargetCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LockedWoCommitted = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LockedInvoicedCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LockedProfitLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackages", x => x.ReconciliationPackageId);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationPackageSalesLines",
                columns: table => new
                {
                    ReconciliationPackageSalesLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReconciliationPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPackageSalesLines", x => x.ReconciliationPackageSalesLineId);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachments",
                columns: table => new
                {
                    RequestAttachmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DrawingCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RevisionLabel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    BlobRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AddedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachments", x => x.RequestAttachmentId);
                });

            migrationBuilder.CreateTable(
                name: "SkillReferences",
                columns: table => new
                {
                    SkillReferenceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RefKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillReferences", x => x.SkillReferenceId);
                });

            migrationBuilder.CreateTable(
                name: "SkillRevisions",
                columns: table => new
                {
                    SkillRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SavedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillRevisions", x => x.SkillRevisionId);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    SkillKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pinned = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.SkillKey);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorXeroLinks",
                columns: table => new
                {
                    SubcontractorXeroLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroContactId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorXeroLinks", x => x.SubcontractorXeroLinkId);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemLinks",
                columns: table => new
                {
                    TodoItemLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TodoItemAId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TodoItemBId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LinkedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemLinks", x => x.TodoItemLinkId);
                });

            migrationBuilder.CreateTable(
                name: "UsefulInformationNotes",
                columns: table => new
                {
                    UsefulInformationNoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsefulInformationNotes", x => x.UsefulInformationNoteId);
                });

            migrationBuilder.CreateTable(
                name: "XeroDisputeMessages",
                columns: table => new
                {
                    XeroDisputeMessageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroDisputeMessages", x => x.XeroDisputeMessageId);
                });

            migrationBuilder.CreateTable(
                name: "XeroLineWorkOrderLinks",
                columns: table => new
                {
                    XeroLineWorkOrderLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroLedgerLineId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XeroLineWorkOrderLinks", x => x.XeroLineWorkOrderLinkId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentActivity_IsAutonomous_OccurredAt",
                table: "AgentActivity",
                columns: new[] { "IsAutonomous", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentActivity_OccurredAt",
                table: "AgentActivity",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentActivity_ProjectId",
                table: "AgentActivity",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationMessages_ConversationId_Sequence",
                table: "AiConversationMessages",
                columns: new[] { "ConversationId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_ScopeRecordId",
                table: "AiConversations",
                columns: new[] { "ScopeRecordId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_StartedByEmail_LastMessageAt",
                table: "AiConversations",
                columns: new[] { "StartedByEmail", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectInstructions_ProjectId",
                table: "ArchitectInstructions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectInstructionVariations_ArchitectInstructionId",
                table: "ArchitectInstructionVariations",
                column: "ArchitectInstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectInstructionVariations_VariationOrderId",
                table: "ArchitectInstructionVariations",
                column: "VariationOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_RecordId",
                table: "AuditEvents",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContacts_SubcontractorId",
                table: "CompanyContacts",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContracts_ProjectId",
                table: "ProjectContracts",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachments_RequestId",
                table: "RequestAttachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillReferences_SkillKey_RefKey",
                table: "SkillReferences",
                columns: new[] { "SkillKey", "RefKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillRevisions_SkillKey_Version",
                table: "SkillRevisions",
                columns: new[] { "SkillKey", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_AgentKey_IsActive",
                table: "Skills",
                columns: new[] { "AgentKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorXeroLinks_SubcontractorId",
                table: "SubcontractorXeroLinks",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorXeroLinks_XeroContactId",
                table: "SubcontractorXeroLinks",
                column: "XeroContactId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemLinks_TodoItemAId_TodoItemBId",
                table: "TodoItemLinks",
                columns: new[] { "TodoItemAId", "TodoItemBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemLinks_TodoItemBId",
                table: "TodoItemLinks",
                column: "TodoItemBId");

            migrationBuilder.CreateIndex(
                name: "IX_UsefulInformationNotes_ProjectId",
                table: "UsefulInformationNotes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_XeroDisputeMessages_XeroLedgerLineId",
                table: "XeroDisputeMessages",
                column: "XeroLedgerLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentActivity");

            migrationBuilder.DropTable(
                name: "AiConversationMessages");

            migrationBuilder.DropTable(
                name: "AiConversations");

            migrationBuilder.DropTable(
                name: "ArchitectInstructions");

            migrationBuilder.DropTable(
                name: "ArchitectInstructionVariations");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "CompanyContacts");

            migrationBuilder.DropTable(
                name: "CostCentreCostProgress");

            migrationBuilder.DropTable(
                name: "CostCentreGroupMembers");

            migrationBuilder.DropTable(
                name: "CostCentreGroups");

            migrationBuilder.DropTable(
                name: "PartyContacts");

            migrationBuilder.DropTable(
                name: "ProgressPhotos");

            migrationBuilder.DropTable(
                name: "ProgressReports");

            migrationBuilder.DropTable(
                name: "ProgressReportSelections");

            migrationBuilder.DropTable(
                name: "ProgressUpdates");

            migrationBuilder.DropTable(
                name: "ProjectContracts");

            migrationBuilder.DropTable(
                name: "ProjectRetentions");

            migrationBuilder.DropTable(
                name: "ReconciliationPackageCostLines");

            migrationBuilder.DropTable(
                name: "ReconciliationPackageOrders");

            migrationBuilder.DropTable(
                name: "ReconciliationPackages");

            migrationBuilder.DropTable(
                name: "ReconciliationPackageSalesLines");

            migrationBuilder.DropTable(
                name: "RequestAttachments");

            migrationBuilder.DropTable(
                name: "SkillReferences");

            migrationBuilder.DropTable(
                name: "SkillRevisions");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "SubcontractorXeroLinks");

            migrationBuilder.DropTable(
                name: "TodoItemLinks");

            migrationBuilder.DropTable(
                name: "UsefulInformationNotes");

            migrationBuilder.DropTable(
                name: "XeroDisputeMessages");

            migrationBuilder.DropTable(
                name: "XeroLineWorkOrderLinks");

            migrationBuilder.DropColumn(
                name: "AmountDue",
                table: "XeroLedgerLines");

            migrationBuilder.DropColumn(
                name: "InvoiceTotal",
                table: "XeroLedgerLines");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "AcceptedByEmail",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "AcceptedByName",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "DepositPercent",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "DepositRequired",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProgrammeNotes",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProgrammeStart",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CommercialBasis",
                table: "VariationOrderQuotes");

            migrationBuilder.DropColumn(
                name: "Exclusions",
                table: "VariationOrderQuotes");

            migrationBuilder.DropColumn(
                name: "ProgrammeImpact",
                table: "VariationOrderQuotes");

            migrationBuilder.DropColumn(
                name: "DepositPercent",
                table: "ValuationReportSnapshots");

            migrationBuilder.DropColumn(
                name: "DepositReleased",
                table: "ValuationReportSnapshots");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "ValuationReportSnapshots");

            migrationBuilder.DropColumn(
                name: "DepositCredited",
                table: "ValuationInvoices");

            migrationBuilder.DropColumn(
                name: "DepositPercent",
                table: "ValuationClaims");

            migrationBuilder.DropColumn(
                name: "DepositReleased",
                table: "ValuationClaims");

            migrationBuilder.DropColumn(
                name: "DepositReleasedOpening",
                table: "ValuationClaims");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "Subcontractors");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                table: "Subcontractors");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Subcontractors");

            migrationBuilder.DropColumn(
                name: "PartyContactId",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "Routing",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "InviteDraftBcc",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "InviteDraftBody",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "InviteDraftCc",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "InviteDraftSavedAt",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "InviteDraftSubject",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "InviteDraftTo",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "MaterialsApplicable",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "BoqLineItemId",
                table: "BidPackageLineItems");

            migrationBuilder.DropColumn(
                name: "CostCode",
                table: "BidPackageLineItems");

            migrationBuilder.DropColumn(
                name: "Coverage",
                table: "BidPackageLineItems");

            migrationBuilder.DropColumn(
                name: "VariationOrderQuoteId",
                table: "BidPackageLineItems");

            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                table: "Quotes",
                newName: "PaidAt");

            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "InfoChaseItems",
                newName: "RaisedAt");

            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                table: "DrawingRevisions",
                newName: "PaidAt");

            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "AccessRequests",
                newName: "RaisedAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "QuoteLineItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "QuoteLineItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "QuoteLineItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedMonthlyValuation",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "BidPackageLineItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.CreateTable(
                name: "IntakeEmails",
                columns: table => new
                {
                    IntakeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BodyPreview = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClaimedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConversationId = table.Column<string>(type: "nvarchar(998)", maxLength: 998, nullable: true),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GraphMessageId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HasAttachments = table.Column<bool>(type: "bit", nullable: false),
                    InReplyTo = table.Column<string>(type: "nvarchar(998)", maxLength: 998, nullable: true),
                    InternetMessageId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LinkedRequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReferencesHeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeEmails", x => x.IntakeId);
                });

            migrationBuilder.CreateTable(
                name: "MailboxSyncStates",
                columns: table => new
                {
                    Mailbox = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BacklogImported = table.Column<bool>(type: "bit", nullable: false),
                    DeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubscriptionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxSyncStates", x => x.Mailbox);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XeroLedgerLines_AllocationStatus",
                table: "XeroLedgerLines",
                column: "AllocationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_XeroLedgerLines_ProjectId_CostCenterCode",
                table: "XeroLedgerLines",
                columns: new[] { "ProjectId", "CostCenterCode" });

            migrationBuilder.CreateIndex(
                name: "IX_XeroLedgerLines_XeroInvoiceId",
                table: "XeroLedgerLines",
                column: "XeroInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_XeroCostSplits_ProjectId_CostCenterCode",
                table: "XeroCostSplits",
                columns: new[] { "ProjectId", "CostCenterCode" });

            migrationBuilder.CreateIndex(
                name: "IX_XeroCostSplits_XeroLedgerLineId",
                table: "XeroCostSplits",
                column: "XeroLedgerLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationReportSnapshots_ProjectId",
                table: "ValuationReportSnapshots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationReportSnapshots_ValuationInvoiceId",
                table: "ValuationReportSnapshots",
                column: "ValuationInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationReportSnapshotLines_ValuationReportSnapshotId",
                table: "ValuationReportSnapshotLines",
                column: "ValuationReportSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationLineItems_ProjectId",
                table: "ValuationLineItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationInvoiceEvents_ValuationInvoiceId",
                table: "ValuationInvoiceEvents",
                column: "ValuationInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationClaims_ProjectId",
                table: "ValuationClaims",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Name",
                table: "Trades",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorTrades_SubcontractorId_TradeId",
                table: "SubcontractorTrades",
                columns: new[] { "SubcontractorId", "TradeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_RequestId",
                table: "RequestItems",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAgents_RequestId",
                table: "RequestAgents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContacts_ProjectId",
                table: "ProjectContacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimLines_ValuationClaimId",
                table: "ClaimLines",
                column: "ValuationClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProposals_RequestId",
                table: "AgentProposals",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentChatMessages_RequestId_AgentKey",
                table: "AgentChatMessages",
                columns: new[] { "RequestId", "AgentKey" });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeEmails_InternetMessageId",
                table: "IntakeEmails",
                column: "InternetMessageId",
                unique: true);
        }
    }
}
