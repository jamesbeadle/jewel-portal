using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Step 1 of 2 of consolidating the valuation report and the tagged valuation snapshot into
    /// ONE object (2026-09-18): the claim. Widens ClaimLines with the frozen bill-line copy the
    /// snapshot line used to hold, stamps ValuationClaims.LockedAt, creates the alias register
    /// ValuationClaimLegacyStatements (old snapshot id / VRS number → claim) and backfills every
    /// locked claim's statement lines from its live snapshot (else the live bill). ADDITIVE —
    /// the old tables and ValuationInvoices.ValuationReportSnapshotId are dropped by step 2
    /// (DropValuationReportSnapshots) once the backfill has been checked. Scoped script with the
    /// same SQL and the printed reconciliation: api/Migrations/consolidate-valuation-statements.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260918120000_ConsolidateValuationStatements")]
    public partial class ConsolidateValuationStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "ElementType", table: "ClaimLines", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "SectionCode", table: "ClaimLines", type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "SectionName", table: "ClaimLines", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "VariationRef", table: "ClaimLines", type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "VariationTitle", table: "ClaimLines", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "LineType", table: "ClaimLines", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "CostCode", table: "ClaimLines", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Description", table: "ClaimLines", type: "nvarchar(512)", maxLength: 512, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Unit", table: "ClaimLines", type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<decimal>(name: "Quantity", table: "ClaimLines", type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "Rate", table: "ClaimLines", type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "LineAmount", table: "ClaimLines", type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "Comments", table: "ClaimLines", type: "nvarchar(512)", maxLength: 512, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "DisplayOrder", table: "ClaimLines", type: "int", nullable: false, defaultValue: -1);
            migrationBuilder.AddColumn<string>(name: "ClientReference", table: "ClaimLines", type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(name: "LockedAt", table: "ValuationClaims", type: "datetimeoffset", nullable: true);

            migrationBuilder.CreateTable(
                name: "ValuationClaimLegacyStatements",
                columns: table => new
                {
                    ValuationReportSnapshotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false),
                    ValuationInvoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_ValuationClaimLegacyStatements", x => x.ValuationReportSnapshotId));

            migrationBuilder.CreateIndex(
                name: "IX_ValuationClaimLegacyStatements_ValuationClaimId",
                table: "ValuationClaimLegacyStatements",
                column: "ValuationClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuationClaimLegacyStatements_ProjectId_Number",
                table: "ValuationClaimLegacyStatements",
                columns: new[] { "ProjectId", "Number" });

            // The backfill — the same batches the scoped script runs (which also PRINTs the
            migrationBuilder.Sql(@"-- ---------------------------------------------------------------------------
-- 2. The alias register: every snapshot that named a claim. (A snapshot with
--    no claim — none exist today — cannot alias anything; it is listed below
--    and left in the old table for step 2 to decide.)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'[ValuationReportSnapshots]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [ValuationClaimLegacyStatements]
        ([ValuationReportSnapshotId], [ProjectId], [ValuationClaimId], [Number], [Label], [TakenAt], [IsSuperseded], [ValuationInvoiceId])
    SELECT s.[ValuationReportSnapshotId], s.[ProjectId], s.[ValuationClaimId], s.[Number], s.[Label], s.[TakenAt], s.[IsSuperseded], s.[ValuationInvoiceId]
    FROM [ValuationReportSnapshots] s
    WHERE s.[ValuationClaimId] IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [ValuationClaimLegacyStatements] l WHERE l.[ValuationReportSnapshotId] = s.[ValuationReportSnapshotId]);
    PRINT CONCAT('Legacy statement aliases registered: ', @@ROWCOUNT);

    SELECT 'CLAIM-LESS SNAPSHOT (not aliased — review)' AS [Note], s.[ValuationReportSnapshotId], s.[ProjectId], s.[Number], s.[Label], s.[TakenAt]
    FROM [ValuationReportSnapshots] s WHERE s.[ValuationClaimId] IS NULL;
END;");

            migrationBuilder.Sql(@"-- ---------------------------------------------------------------------------
-- 3a. LockedAt on every locked claim (Status 1 Preapproved / 2 Confirmed):
--     the moment its live statement was frozen, else its lock stamp.
-- ---------------------------------------------------------------------------
UPDATE c SET [LockedAt] = COALESCE(
        (SELECT MAX(l.[TakenAt]) FROM [ValuationClaimLegacyStatements] l
          WHERE l.[ValuationClaimId] = c.[ValuationClaimId] AND l.[IsSuperseded] = 0),
        c.[PreapprovedAt], c.[ConfirmedAt], SYSDATETIMEOFFSET())
FROM [ValuationClaims] c
WHERE c.[Status] <> 0 AND c.[LockedAt] IS NULL;
PRINT CONCAT('Locked claims stamped with LockedAt: ', @@ROWCOUNT);");

            migrationBuilder.Sql(@"-- ---------------------------------------------------------------------------
-- 3b. Statement lines FROM THE LIVE SNAPSHOT: for each locked claim, its
--     newest non-superseded snapshot (the statement the client was sent).
--     Existing entries take the snapshot's descriptors and order and KEEP
--     their own % / money (the ledger the next claim seeds from); snapshot
--     lines with no entry (0% lines) become 0% rows so the statement is whole.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'[ValuationReportSnapshotLines]', N'U') IS NOT NULL
BEGIN
    ;WITH source AS (
        SELECT l.[ValuationClaimId], l.[ValuationReportSnapshotId],
               ROW_NUMBER() OVER (PARTITION BY l.[ValuationClaimId] ORDER BY l.[TakenAt] DESC) AS rn
        FROM [ValuationClaimLegacyStatements] l
        JOIN [ValuationClaims] c ON c.[ValuationClaimId] = l.[ValuationClaimId]
        WHERE l.[IsSuperseded] = 0 AND c.[Status] <> 0
    )
    SELECT s.[ValuationClaimId], s.[ValuationReportSnapshotId]
    INTO #statement_source
    FROM source s WHERE s.rn = 1;

    -- Where the entry's money disagrees with the frozen statement, say so (the entry wins:
    -- it is the ledger; the difference is what a later respread/rebase re-dealt).
    SELECT 'ENTRY DIFFERS FROM STATEMENT (entry kept)' AS [Note], c.[ProjectId], c.[ClaimNumber],
           e.[ValuationLineItemId], e.[PercentComplete] AS [EntryPercent], sl.[PercentComplete] AS [StatementPercent],
           e.[CumulativeClaimed] AS [EntryClaimed], sl.[CumulativeClaimed] AS [StatementClaimed]
    FROM #statement_source ss
    JOIN [ValuationClaims] c ON c.[ValuationClaimId] = ss.[ValuationClaimId]
    JOIN [ValuationReportSnapshotLines] sl ON sl.[ValuationReportSnapshotId] = ss.[ValuationReportSnapshotId]
    JOIN [ClaimLines] e ON e.[ValuationClaimId] = ss.[ValuationClaimId] AND e.[ValuationLineItemId] = sl.[SourceValuationLineItemId]
    WHERE e.[PercentComplete] <> sl.[PercentComplete] OR e.[CumulativeClaimed] <> sl.[CumulativeClaimed];

    UPDATE e SET
        e.[ElementType] = sl.[ElementType], e.[SectionCode] = sl.[SectionCode], e.[SectionName] = sl.[SectionName],
        e.[VariationRef] = sl.[VariationRef], e.[VariationTitle] = sl.[VariationTitle], e.[LineType] = sl.[LineType],
        e.[CostCode] = sl.[CostCode], e.[Description] = sl.[Description], e.[Unit] = sl.[Unit],
        e.[Quantity] = sl.[Quantity], e.[Rate] = sl.[Rate], e.[LineAmount] = sl.[LineAmount],
        e.[Comments] = sl.[Comments], e.[DisplayOrder] = sl.[DisplayOrder], e.[ClientReference] = sl.[ClientReference]
    FROM [ClaimLines] e
    JOIN #statement_source ss ON ss.[ValuationClaimId] = e.[ValuationClaimId]
    JOIN [ValuationReportSnapshotLines] sl ON sl.[ValuationReportSnapshotId] = ss.[ValuationReportSnapshotId]
                                          AND sl.[SourceValuationLineItemId] = e.[ValuationLineItemId]
    WHERE e.[DisplayOrder] = -1;
    PRINT CONCAT('Entries frozen from the live statement: ', @@ROWCOUNT);

    INSERT INTO [ClaimLines]
        ([ClaimLineId], [ValuationClaimId], [ValuationLineItemId], [PercentComplete], [CumulativeClaimed], [PeriodIncrement],
         [ElementType], [SectionCode], [SectionName], [VariationRef], [VariationTitle], [LineType], [CostCode], [Description],
         [Unit], [Quantity], [Rate], [LineAmount], [Comments], [DisplayOrder], [ClientReference])
    SELECT REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), ss.[ValuationClaimId], sl.[SourceValuationLineItemId],
           sl.[PercentComplete], sl.[CumulativeClaimed], sl.[PeriodIncrement],
           sl.[ElementType], sl.[SectionCode], sl.[SectionName], sl.[VariationRef], sl.[VariationTitle], sl.[LineType], sl.[CostCode], sl.[Description],
           sl.[Unit], sl.[Quantity], sl.[Rate], sl.[LineAmount], sl.[Comments], sl.[DisplayOrder], sl.[ClientReference]
    FROM #statement_source ss
    JOIN [ValuationReportSnapshotLines] sl ON sl.[ValuationReportSnapshotId] = ss.[ValuationReportSnapshotId]
    WHERE NOT EXISTS (SELECT 1 FROM [ClaimLines] e
                      WHERE e.[ValuationClaimId] = ss.[ValuationClaimId] AND e.[ValuationLineItemId] = sl.[SourceValuationLineItemId]);
    PRINT CONCAT('Statement-only (0%) lines added to claims: ', @@ROWCOUNT);

    DROP TABLE #statement_source;
END;");

            migrationBuilder.Sql(@"-- ---------------------------------------------------------------------------
-- 3c. Statement lines FROM THE LIVE BILL: every locked claim's row still
--     unfrozen — claims that never had a snapshot (locked before snapshots
--     existed, or never invoiced) and entries added under a locked claim
--     after its statement went out (a variation re-breakdown). The live bill
--     is the best record there is; the claims are listed so they can be
--     checked. Order continues after any statement order already frozen.
-- ---------------------------------------------------------------------------
SELECT 'FROZEN FROM LIVE BILL (no statement existed)' AS [Note], c.[ProjectId], c.[ClaimNumber], c.[Name], c.[Status],
       COUNT(*) AS [UnfrozenEntries]
FROM [ValuationClaims] c
JOIN [ClaimLines] e ON e.[ValuationClaimId] = c.[ValuationClaimId]
WHERE c.[Status] <> 0 AND e.[DisplayOrder] = -1
GROUP BY c.[ProjectId], c.[ClaimNumber], c.[Name], c.[Status];

-- Rows for live bill lines the locked claim has no entry for (0% at lock).
INSERT INTO [ClaimLines]
    ([ClaimLineId], [ValuationClaimId], [ValuationLineItemId], [PercentComplete], [CumulativeClaimed], [PeriodIncrement],
     [ElementType], [SectionCode], [SectionName], [VariationRef], [VariationTitle], [LineType], [CostCode], [Description],
     [Unit], [Quantity], [Rate], [LineAmount], [Comments], [DisplayOrder], [ClientReference])
SELECT REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), c.[ValuationClaimId], li.[ValuationLineItemId], 0, 0, 0,
       li.[ElementType], li.[SectionCode], li.[SectionName], li.[VariationRef], li.[VariationTitle], li.[LineType], li.[CostCode], li.[Description],
       li.[Unit], li.[Quantity], li.[Rate], li.[LineAmount], li.[Comments], -1, li.[ClientReference]
FROM [ValuationClaims] c
JOIN [ValuationLineItems] li ON li.[ProjectId] = c.[ProjectId]
WHERE c.[Status] <> 0
  AND NOT EXISTS (SELECT 1 FROM [ValuationClaimLegacyStatements] l WHERE l.[ValuationClaimId] = c.[ValuationClaimId] AND l.[IsSuperseded] = 0)
  AND NOT EXISTS (SELECT 1 FROM [ClaimLines] e WHERE e.[ValuationClaimId] = c.[ValuationClaimId] AND e.[ValuationLineItemId] = li.[ValuationLineItemId]);
PRINT CONCAT('Bill-only (0%) lines added to never-snapshotted locked claims: ', @@ROWCOUNT);

-- Descriptors from the live bill for every still-unfrozen locked row, ordered as the
-- statement orders them (element, variation number, bill order), after any frozen order.
;WITH unfrozen AS (
    SELECT e.[ClaimLineId], e.[ValuationClaimId], li.*,
           ROW_NUMBER() OVER (PARTITION BY e.[ValuationClaimId]
                              ORDER BY li.[ElementType],
                                       CASE WHEN li.[ElementType] = 3
                                            THEN TRY_CAST(SUBSTRING(li.[VariationRef], PATINDEX('%[0-9]%', li.[VariationRef] + '0'), 10) AS int)
                                            ELSE 0 END,
                                       li.[DisplayOrder]) AS rn,
           (SELECT ISNULL(MAX(x.[DisplayOrder]), -1) FROM [ClaimLines] x WHERE x.[ValuationClaimId] = e.[ValuationClaimId]) AS frozenMax
    FROM [ClaimLines] e
    JOIN [ValuationClaims] c ON c.[ValuationClaimId] = e.[ValuationClaimId]
    JOIN [ValuationLineItems] li ON li.[ValuationLineItemId] = e.[ValuationLineItemId]
    WHERE c.[Status] <> 0 AND e.[DisplayOrder] = -1
)
UPDATE e SET
    e.[ElementType] = u.[ElementType], e.[SectionCode] = u.[SectionCode], e.[SectionName] = u.[SectionName],
    e.[VariationRef] = u.[VariationRef], e.[VariationTitle] = u.[VariationTitle], e.[LineType] = u.[LineType],
    e.[CostCode] = u.[CostCode], e.[Description] = u.[Description], e.[Unit] = u.[Unit],
    e.[Quantity] = u.[Quantity], e.[Rate] = u.[Rate], e.[LineAmount] = u.[LineAmount],
    e.[Comments] = u.[Comments], e.[DisplayOrder] = u.frozenMax + u.rn, e.[ClientReference] = u.[ClientReference]
FROM [ClaimLines] e
JOIN unfrozen u ON u.[ClaimLineId] = e.[ClaimLineId];
PRINT CONCAT('Entries frozen from the live bill: ', @@ROWCOUNT);

-- Any locked row STILL unfrozen names a bill line that no longer exists and had no statement:
-- money with no description. Listed, kept (the money is real), printed as ""(line removed)"".
UPDATE e SET e.[Description] = N'(line removed from the bill before the statement was consolidated)', e.[DisplayOrder] = 99999
FROM [ClaimLines] e JOIN [ValuationClaims] c ON c.[ValuationClaimId] = e.[ValuationClaimId]
WHERE c.[Status] <> 0 AND e.[DisplayOrder] = -1;
PRINT CONCAT('Orphaned locked entries kept with a placeholder description: ', @@ROWCOUNT);");

            migrationBuilder.Sql(@"-- ---------------------------------------------------------------------------
-- 3d. ""This period"" on every locked row re-stated by the one rule
--     (ClaimPeriodBaseline, 2026-09-07): cumulative less the same line's
--     cumulative on the claim numbered immediately before, whatever that
--     claim's status. The stored increment was a convenience written when the
--     % was entered and could lag; a frozen statement prints the stored
--     figure, so it is made right here once. (scripts/2026-09-07-restate-
--     period-increments.sql did the same for the old snapshot rows.)
-- ---------------------------------------------------------------------------
;WITH prev AS (
    SELECT c.[ValuationClaimId], p.[ValuationClaimId] AS [PrevClaimId]
    FROM [ValuationClaims] c
    OUTER APPLY (SELECT TOP 1 x.[ValuationClaimId] FROM [ValuationClaims] x
                 WHERE x.[ProjectId] = c.[ProjectId] AND x.[ClaimNumber] < c.[ClaimNumber]
                 ORDER BY x.[ClaimNumber] DESC) p
    WHERE c.[Status] <> 0
)
UPDATE e SET e.[PeriodIncrement] = e.[CumulativeClaimed] - ISNULL(pe.[CumulativeClaimed], 0)
FROM [ClaimLines] e
JOIN prev ON prev.[ValuationClaimId] = e.[ValuationClaimId]
LEFT JOIN [ClaimLines] pe ON pe.[ValuationClaimId] = prev.[PrevClaimId] AND pe.[ValuationLineItemId] = e.[ValuationLineItemId]
WHERE e.[PeriodIncrement] <> e.[CumulativeClaimed] - ISNULL(pe.[CumulativeClaimed], 0);
PRINT CONCAT('Locked rows whose this-period figure was re-stated by the rule: ', @@ROWCOUNT);");

            migrationBuilder.Sql(@"-- ---------------------------------------------------------------------------
-- 4. Reconciliation: every locked claim's frozen lines must sum to its frozen
--    TotalWorksComplete (Declined/TBC lines excluded, as everywhere).
-- ---------------------------------------------------------------------------
SELECT 'RECONCILE' AS [Note], c.[ProjectId], c.[ClaimNumber], c.[Name], c.[TotalWorksComplete] AS [ClaimTotal],
       SUM(CASE WHEN e.[LineType] IN (3, 4) THEN 0 ELSE e.[CumulativeClaimed] END) AS [LinesTotal],
       c.[TotalWorksComplete] - SUM(CASE WHEN e.[LineType] IN (3, 4) THEN 0 ELSE e.[CumulativeClaimed] END) AS [Difference]
FROM [ValuationClaims] c
JOIN [ClaimLines] e ON e.[ValuationClaimId] = c.[ValuationClaimId]
WHERE c.[Status] <> 0
GROUP BY c.[ProjectId], c.[ClaimNumber], c.[Name], c.[TotalWorksComplete]
ORDER BY c.[ProjectId], c.[ClaimNumber];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversing loses the frozen statement lines of claims that never had a snapshot and
            // the alias register; the old snapshot tables are untouched by this step.
            migrationBuilder.DropTable(name: "ValuationClaimLegacyStatements");
            migrationBuilder.DropColumn(name: "LockedAt", table: "ValuationClaims");
            migrationBuilder.DropColumn(name: "ElementType", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "SectionCode", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "SectionName", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "VariationRef", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "VariationTitle", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "LineType", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "CostCode", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "Description", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "Unit", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "Quantity", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "Rate", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "LineAmount", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "Comments", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "DisplayOrder", table: "ClaimLines");
            migrationBuilder.DropColumn(name: "ClientReference", table: "ClaimLines");
        }
    }
}
