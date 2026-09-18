-- ============================================================================
-- ConsolidateValuationStatements  (2026-09-18)  — step 1 of 2 (ADDITIVE)
-- ============================================================================
-- A valuation report (ValuationClaim + ClaimLines) and a tagged valuation
-- snapshot (ValuationReportSnapshots + …Lines) were two objects for one thing:
-- the state of a project's valuation at a point in time. From this migration
-- the CLAIM is the one valuation object — created, tagged, reported on and
-- invoiced from — and locking it ("We're claiming this") freezes its own
-- statement lines. This step:
--
--   1. widens ClaimLines with the frozen copy of the bill line the snapshot
--      line used to hold (description, code, qty, rate, amount, client ref,
--      statement order) and stamps ValuationClaims.LockedAt;
--   2. creates ValuationClaimLegacyStatements — the alias register of every
--      snapshot that existed (old id + per-project VRS number → claim), so
--      old links, invoices and JPMS/VRS-… mailbox tags keep resolving;
--   3. BACKFILLS every locked claim's statement lines: from its live
--      (non-superseded, newest) snapshot when one exists — the statement the
--      client was actually sent — else from the live bill (claims locked
--      before snapshots existed, or never invoiced), and PRINTS what it did.
--
-- It drops NOTHING. ValuationReportSnapshots, ValuationReportSnapshotLines
-- and ValuationInvoices.ValuationReportSnapshotId stay in place, unread by
-- the new code, until step 2 (drop-valuation-report-snapshots.sql) is run
-- after the backfill has been checked. Nothing is lost between the two.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies
-- the migration directly and records its id in __EFMigrationsHistory so EF
-- never re-applies it. Mirrors
-- api/Migrations/20260918120000_ConsolidateValuationStatements.cs.
-- MUST be applied before the deployed api runs (it reads the new columns).
-- Idempotent: safe to re-run; the backfill only touches rows it has not
-- filled (DisplayOrder = -1) and claims with no LockedAt.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i consolidate-valuation-statements.sql -b -o consolidate-valuation-statements.log
-- ============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;
GO

-- ---------------------------------------------------------------------------
-- 1. Schema: the frozen bill-line copy on ClaimLines, LockedAt on the claim.
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'ClaimLines', N'ElementType') IS NULL
BEGIN
    ALTER TABLE [ClaimLines] ADD
        [ElementType]     int            NOT NULL CONSTRAINT [DF_ClaimLines_ElementType]     DEFAULT 0,
        [SectionCode]     nvarchar(16)   NOT NULL CONSTRAINT [DF_ClaimLines_SectionCode]     DEFAULT N'',
        [SectionName]     nvarchar(128)  NOT NULL CONSTRAINT [DF_ClaimLines_SectionName]     DEFAULT N'',
        [VariationRef]    nvarchar(16)   NOT NULL CONSTRAINT [DF_ClaimLines_VariationRef]    DEFAULT N'',
        [VariationTitle]  nvarchar(256)  NOT NULL CONSTRAINT [DF_ClaimLines_VariationTitle]  DEFAULT N'',
        [LineType]        int            NOT NULL CONSTRAINT [DF_ClaimLines_LineType]        DEFAULT 0,
        [CostCode]        nvarchar(32)   NOT NULL CONSTRAINT [DF_ClaimLines_CostCode]        DEFAULT N'',
        [Description]     nvarchar(512)  NOT NULL CONSTRAINT [DF_ClaimLines_Description]     DEFAULT N'',
        [Unit]            nvarchar(16)   NOT NULL CONSTRAINT [DF_ClaimLines_Unit]            DEFAULT N'',
        [Quantity]        decimal(18,4)  NOT NULL CONSTRAINT [DF_ClaimLines_Quantity]        DEFAULT 0,
        [Rate]            decimal(18,4)  NOT NULL CONSTRAINT [DF_ClaimLines_Rate]            DEFAULT 0,
        [LineAmount]      decimal(18,4)  NOT NULL CONSTRAINT [DF_ClaimLines_LineAmount]      DEFAULT 0,
        [Comments]        nvarchar(512)  NOT NULL CONSTRAINT [DF_ClaimLines_Comments]        DEFAULT N'',
        [DisplayOrder]    int            NOT NULL CONSTRAINT [DF_ClaimLines_DisplayOrder]    DEFAULT -1,
        [ClientReference] nvarchar(64)   NOT NULL CONSTRAINT [DF_ClaimLines_ClientReference] DEFAULT N'';
END;
GO

IF COL_LENGTH(N'ValuationClaims', N'LockedAt') IS NULL
    ALTER TABLE [ValuationClaims] ADD [LockedAt] datetimeoffset NULL;
GO

IF OBJECT_ID(N'[ValuationClaimLegacyStatements]', N'U') IS NULL
BEGIN
    CREATE TABLE [ValuationClaimLegacyStatements] (
        [ValuationReportSnapshotId] nvarchar(64)  NOT NULL,
        [ProjectId]                 nvarchar(64)  NOT NULL,
        [ValuationClaimId]          nvarchar(64)  NOT NULL,
        [Number]                    int           NOT NULL,
        [Label]                     nvarchar(256) NOT NULL,
        [TakenAt]                   datetimeoffset NOT NULL,
        [IsSuperseded]              bit           NOT NULL,
        [ValuationInvoiceId]        nvarchar(64)  NULL,
        CONSTRAINT [PK_ValuationClaimLegacyStatements] PRIMARY KEY ([ValuationReportSnapshotId])
    );
    CREATE INDEX [IX_ValuationClaimLegacyStatements_ValuationClaimId] ON [ValuationClaimLegacyStatements] ([ValuationClaimId]);
    CREATE INDEX [IX_ValuationClaimLegacyStatements_ProjectId_Number] ON [ValuationClaimLegacyStatements] ([ProjectId], [Number]);
END;
GO

-- ---------------------------------------------------------------------------
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
END;
GO

-- ---------------------------------------------------------------------------
-- 3a. LockedAt on every locked claim (Status 1 Preapproved / 2 Confirmed):
--     the moment its live statement was frozen, else its lock stamp.
-- ---------------------------------------------------------------------------
UPDATE c SET [LockedAt] = COALESCE(
        (SELECT MAX(l.[TakenAt]) FROM [ValuationClaimLegacyStatements] l
          WHERE l.[ValuationClaimId] = c.[ValuationClaimId] AND l.[IsSuperseded] = 0),
        c.[PreapprovedAt], c.[ConfirmedAt], SYSDATETIMEOFFSET())
FROM [ValuationClaims] c
WHERE c.[Status] <> 0 AND c.[LockedAt] IS NULL;
PRINT CONCAT('Locked claims stamped with LockedAt: ', @@ROWCOUNT);
GO

-- ---------------------------------------------------------------------------
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
END;
GO

-- ---------------------------------------------------------------------------
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
-- money with no description. Listed, kept (the money is real), printed as "(line removed)".
UPDATE e SET e.[Description] = N'(line removed from the bill before the statement was consolidated)', e.[DisplayOrder] = 99999
FROM [ClaimLines] e JOIN [ValuationClaims] c ON c.[ValuationClaimId] = e.[ValuationClaimId]
WHERE c.[Status] <> 0 AND e.[DisplayOrder] = -1;
PRINT CONCAT('Orphaned locked entries kept with a placeholder description: ', @@ROWCOUNT);
GO

-- ---------------------------------------------------------------------------
-- 3d. "This period" on every locked row re-stated by the one rule
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
PRINT CONCAT('Locked rows whose this-period figure was re-stated by the rule: ', @@ROWCOUNT);
GO

-- ---------------------------------------------------------------------------
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
ORDER BY c.[ProjectId], c.[ClaimNumber];
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260918120000_ConsolidateValuationStatements')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260918120000_ConsolidateValuationStatements', N'8.0.10');
END;
GO

COMMIT;
GO
